using System;
using System.Collections.Generic;
using System.Windows.Input;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.Model;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    // ViewModel for Settings: holds bindings, defaults and simple persistence hooks.
    internal class SettingVM : ViewModelBase
    {
        // backing store for bindings (actionName -> (Key, Modifiers))
        private readonly Dictionary<string, Tuple<Key, ModifierKeys>> _bindings;
        private readonly Dictionary<string, Tuple<Key, ModifierKeys>> _defaults = new Dictionary<string, Tuple<Key, ModifierKeys>>()
        {
            { "KeyBindActionA", Tuple.Create(Key.Space, ModifierKeys.None) },
            { "KeyBindActionB", Tuple.Create(Key.LeftCtrl, ModifierKeys.None) },
            { "KeyBindActionC", Tuple.Create(Key.LeftShift, ModifierKeys.None) },
            { "KeyBindActionD", Tuple.Create(Key.E, ModifierKeys.None) },
            { "KeyBindActionA2", Tuple.Create(Key.Enter, ModifierKeys.None) },
            { "KeyBindActionB2", Tuple.Create(Key.RightCtrl, ModifierKeys.None) },
            { "KeyBindActionC2", Tuple.Create(Key.RightShift, ModifierKeys.None) },
            { "KeyBindActionD2", Tuple.Create(Key.R, ModifierKeys.None) }
        };

        // Speed properties
        private int _player1Speed = 2;
        private int _player2Speed = 2;

        public int Player1Speed
        {
            get => _player1Speed;
            set
            {
                if (_player1Speed != value)
                {
                    _player1Speed = value;
                    OnPropertyChanged(nameof(Player1Speed));
                    UpdatePlayerSpeed(0, value); // 0-based index
                }
            }
        }

        public int Player2Speed
        {
            get => _player2Speed;
            set
            {
                if (_player2Speed != value)
                {
                    _player2Speed = value;
                    OnPropertyChanged(nameof(Player2Speed));
                    UpdatePlayerSpeed(1, value); // 0-based index
                }
            }
        }

        public SettingVM()
        {
            // initialize from defaults (replace with persisted load when available)
            _bindings = new Dictionary<string, Tuple<Key, ModifierKeys>>();
            foreach (var kv in _defaults)
                _bindings[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);

            // Load speed settings from current mode
            LoadSpeedSettings();
        }

        private void LoadSpeedSettings()
        {
            if (GameModeManager.CurrentMode != null && GameModeManager.CurrentMode.Players != null)
            {
                var players = GameModeManager.CurrentMode.Players;
                if (players.Count > 0)
                {
                    _player1Speed = players[0].Speed.CurrentSpeed;
                    OnPropertyChanged(nameof(Player1Speed));
                }
                if (players.Count > 1)
                {
                    _player2Speed = players[1].Speed.CurrentSpeed;
                    OnPropertyChanged(nameof(Player2Speed));
                }
            }
        }

        private void UpdatePlayerSpeed(int playerIndex, int speed)
        {
            if (GameModeManager.CurrentMode != null && GameModeManager.CurrentMode.Players != null)
            {
                var players = GameModeManager.CurrentMode.Players;
                if (playerIndex < players.Count)
                {
                    players[playerIndex].Speed.CurrentSpeed = speed;
                }
            }
        }

        // Return a copy of bindings for the view to display
        public Dictionary<string, Tuple<Key, ModifierKeys>> GetBindings()
        {
            var copy = new Dictionary<string, Tuple<Key, ModifierKeys>>();
            foreach (var kv in _bindings)
                copy[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);
            return copy;
        }

        // Save a binding. If force==false and another action already uses the same key+mods,
        // the method returns the actionName that currently holds that binding (collision).
        // If force==true the existing mapping will be removed and the new binding applied.
        // Returns null when save succeeded (no collision or overwrite applied), or the existing action name on collision when not forced.
        public string SaveBinding(string actionName, Key key, ModifierKeys mods, bool force = false)
        {
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException(nameof(actionName));

            // find existing binding that uses the same key+mods (excluding the same action)
            string existing = null;
            foreach (var kv in _bindings)
            {
                if (kv.Key != actionName && kv.Value.Item1 == key && kv.Value.Item2 == mods)
                {
                    existing = kv.Key;
                    break;
                }
            }

            if (existing != null && !force)
            {
                // collision detected, caller should decide (prompt) — do not overwrite here.
                return existing;
            }

            // If forced and a different existing mapping was found, remove it
            if (existing != null && force)
            {
                _bindings.Remove(existing);
            }

            // Apply binding (overwrite any previous binding for actionName)
            _bindings[actionName] = Tuple.Create(key, mods);

            // TODO: persist to file / settings here
            OnPropertyChanged(nameof(GetBindings));
            return null;
        }

        public void ResetToDefaults()
        {
            _bindings.Clear();
            foreach (var kv in _defaults)
                _bindings[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);

            // Reset speeds to default
            Player1Speed = 2;
            Player2Speed = 2;

            // TODO: persist defaults
            OnPropertyChanged(nameof(GetBindings));
        }

        // Find an action currently using this key+mods, or null if none
        public string FindExistingBinding(Key key, ModifierKeys mods)
        {
            foreach (var kv in _bindings)
            {
                if (kv.Value.Item1 == key && kv.Value.Item2 == mods)
                    return kv.Key;
            }
            return null;
        }

        // Helper: human-friendly text for a binding
        public static string FormatBindingText(Key key, ModifierKeys mods)
        {
            var parts = new List<string>();
            if ((mods & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
            if ((mods & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((mods & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");
            if ((mods & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");

            string keyName = KeyToFriendlyString(key);
            if (parts.Count > 0)
                return string.Join("+", parts) + "+" + keyName;
            return keyName;
        }

        private static string KeyToFriendlyString(Key key)
        {
            switch (key)
            {
                case Key.Space: return "Space";
                case Key.LeftCtrl: return "LeftCtrl";
                case Key.RightCtrl: return "RightCtrl";
                case Key.LeftShift: return "LeftShift";
                case Key.RightShift: return "RightShift";
                case Key.LeftAlt: return "LeftAlt";
                case Key.RightAlt: return "RightAlt";
                case Key.OemPlus: return "+";
                case Key.OemMinus: return "-";
                default: return key.ToString();
            }
        }
    }
}