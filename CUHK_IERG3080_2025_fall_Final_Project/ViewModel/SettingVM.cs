using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Reflection;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.Model;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SettingVM : ViewModelBase
    {
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
                    UpdatePlayerSpeed(0, value);
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
                    UpdatePlayerSpeed(1, value);
                }
            }
        }

        public SettingVM()
        {
            _bindings = new Dictionary<string, Tuple<Key, ModifierKeys>>();
            foreach (var kv in _defaults)
                _bindings[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);

            LoadSpeedSettings();
        }

        private void LoadSpeedSettings()
        {
            var players = GetCurrentModePlayers();
            if (players != null && players.Count > 0)
            {
                dynamic player0 = players[0];
                _player1Speed = (int)player0?.Speed?.CurrentSpeed;
                OnPropertyChanged(nameof(Player1Speed));

                if (players.Count > 1)
                {
                    dynamic player1 = players[1];
                    _player2Speed = (int)player1?.Speed?.CurrentSpeed;
                    OnPropertyChanged(nameof(Player2Speed));
                }
            }
        }

        private void UpdatePlayerSpeed(int playerIndex, int speed)
        {
            var players = GetCurrentModePlayers();
            if (players != null && playerIndex < players.Count)
            {
                dynamic player = players[playerIndex];
                if (player?.Speed != null)
                {
                    player.Speed.CurrentSpeed = speed;
                }
            }
        }

        private IList GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            if (mode == null)
                return null;

            var property = mode.GetType().GetProperty("Players", BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(mode) as IList;
        }

        public Dictionary<string, Tuple<Key, ModifierKeys>> GetBindings()
        {
            var copy = new Dictionary<string, Tuple<Key, ModifierKeys>>();
            foreach (var kv in _bindings)
                copy[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);
            return copy;
        }

        public string SaveBinding(string actionName, Key key, ModifierKeys mods, bool force = false)
        {
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException(nameof(actionName));

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
                return existing;
            }

            if (existing != null && force)
            {
                _bindings.Remove(existing);
            }

            _bindings[actionName] = Tuple.Create(key, mods);
            OnPropertyChanged(nameof(GetBindings));
            return null;
        }

        public void ResetToDefaults()
        {
            _bindings.Clear();
            foreach (var kv in _defaults)
                _bindings[kv.Key] = Tuple.Create(kv.Value.Item1, kv.Value.Item2);

            Player1Speed = 2;
            Player2Speed = 2;

            OnPropertyChanged(nameof(GetBindings));
        }

        public string FindExistingBinding(Key key, ModifierKeys mods)
        {
            foreach (var kv in _bindings)
            {
                if (kv.Value.Item1 == key && kv.Value.Item2 == mods)
                    return kv.Key;
            }
            return null;
        }

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
