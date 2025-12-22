using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.Model;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SettingVM : ViewModelBase
    {
        // Player 1 Speed
        private readonly Action _navigationBack;

        private double _player1Speed;
        public double Player1Speed
        {
            get => _player1Speed;
            set
            {
                _player1Speed = value;
                var settings = PlayerSettingsManager.GetSettings(0);
                settings.Speed = value;
                PlayerSettingsManager.UpdateSettings(0, settings);
                OnPropertyChanged(nameof(Player1Speed));

                UpdateRunningGameSpeed(0, value);
            }
        }

        // Player 2 Speed
        private double _player2Speed;
        public double Player2Speed
        {
            get => _player2Speed;
            set
            {
                _player2Speed = value;
                var settings = PlayerSettingsManager.GetSettings(1);
                settings.Speed = value;
                PlayerSettingsManager.UpdateSettings(1, settings);
                OnPropertyChanged(nameof(Player2Speed));

                UpdateRunningGameSpeed(1, value);
            }
        }

        private void UpdateRunningGameSpeed(int playerIndex, double newSpeed)
        {
            // 通过 GameModeManager 获取当前模式的玩家
            if (GameModeManager.CurrentMode != null)
            {
                var playersField = GameModeManager.CurrentMode.GetType().GetField("_players");
                if (playersField != null)
                {
                    var players = playersField.GetValue(GameModeManager.CurrentMode)
                        as System.Collections.Generic.List<PlayerManager>;

                    if (players != null && playerIndex < players.Count)
                    {
                        players[playerIndex].UpdateSpeed(newSpeed);
                    }
                }
            }
        }

        // Player 1 Key Bindings (display)
        public string P1Blue1 => _listeningFor == "P1Blue1" ? "Enter a key..." : GetBindingDisplay(0, "Blue1", "J");
        public string P1Blue2 => _listeningFor == "P1Blue2" ? "Enter a key..." : GetBindingDisplay(0, "Blue2", "K");
        public string P1Red1 => _listeningFor == "P1Red1" ? "Enter a key..." : GetBindingDisplay(0, "Red1", "D");
        public string P1Red2 => _listeningFor == "P1Red2" ? "Enter a key..." : GetBindingDisplay(0, "Red2", "F");

        // Player 2 Key Bindings (display)
        public string P2Blue1 => _listeningFor == "P2Blue1" ? "Enter a key..." : GetBindingDisplay(1, "Blue1", "O");
        public string P2Blue2 => _listeningFor == "P2Blue2" ? "Enter a key..." : GetBindingDisplay(1, "Blue2", "P");
        public string P2Red1 => _listeningFor == "P2Red1" ? "Enter a key..." : GetBindingDisplay(1, "Red1", "Q");
        public string P2Red2 => _listeningFor == "P2Red2" ? "Enter a key..." : GetBindingDisplay(1, "Red2", "W");

        // Commands for rebinding
        public ICommand RebindP1Blue1Command { get; }
        public ICommand RebindP1Blue2Command { get; }
        public ICommand RebindP1Red1Command { get; }
        public ICommand RebindP1Red2Command { get; }
        public ICommand RebindP2Blue1Command { get; }
        public ICommand RebindP2Blue2Command { get; }
        public ICommand RebindP2Red1Command { get; }
        public ICommand RebindP2Red2Command { get; }
        public ICommand BackCommand { get; }

        private string _listeningFor = null;
        private Window _window;

        // All binding names we manage
        private static readonly string[] BindingNames = new[] { "Blue1", "Blue2", "Red1", "Red2" };


        public SettingVM() : this(null) { }
        public SettingVM(Action navigationBack)
        {
            _navigationBack = navigationBack;

            // Load current speeds
            _player1Speed = PlayerSettingsManager.GetSettings(0).Speed;
            _player2Speed = PlayerSettingsManager.GetSettings(1).Speed;

            // Setup commands
            RebindP1Blue1Command = new RelayCommand(o => StartListening("P1Blue1"));
            RebindP1Blue2Command = new RelayCommand(o => StartListening("P1Blue2"));
            RebindP1Red1Command = new RelayCommand(o => StartListening("P1Red1"));
            RebindP1Red2Command = new RelayCommand(o => StartListening("P1Red2"));
            RebindP2Blue1Command = new RelayCommand(o => StartListening("P2Blue1"));
            RebindP2Blue2Command = new RelayCommand(o => StartListening("P2Blue2"));
            RebindP2Red1Command = new RelayCommand(o => StartListening("P2Red1"));
            RebindP2Red2Command = new RelayCommand(o => StartListening("P2Red2"));

            BackCommand = new RelayCommand(o =>
                {
                    if (_navigationBack != null)
                    {
                        _navigationBack();
                    }
                    else
                    {
                        var navVM = Application.Current.MainWindow?.DataContext as NavigationVM;
                        navVM?.TitleScreenCommand.Execute(null);
                    }
                });
        }

        public void AttachWindow(Window window)
        {
            if (_window != null)
                _window.PreviewKeyDown -= OnKeyPress;

            _window = window;
            if (_window != null)
                _window.PreviewKeyDown += OnKeyPress;
        }

        public void DetachWindow()
        {
            if (_window != null)
                _window.PreviewKeyDown -= OnKeyPress;
            _window = null;
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            if (_listeningFor == null) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Allow Escape to cancel
            if (key == Key.Escape)
            {
                _listeningFor = null;
                RefreshBindings();
                e.Handled = true;
                return;
            }

            // Disallow modifier-only keys for assignment
            if (!IsAssignableKey(key))
            {
                MessageBox.Show($"Key '{key}' cannot be assigned.", "Invalid Key", MessageBoxButton.OK, MessageBoxImage.Information);
                _listeningFor = null;
                RefreshBindings();
                e.Handled = true;
                return;
            }

            SetBinding(key);
            e.Handled = true;
        }

        private void StartListening(string binding)
        {
            _listeningFor = binding;
            RefreshBindings(); // show "Enter a key..."
        }

        /// <summary>
        /// Returns the display string for a binding: if a stored binding exists and is not Key.None, show it;
        /// if stored and Key.None, show "None"; otherwise show the default fallback string.
        /// </summary>
        private string GetBindingDisplay(int playerIdx, string bindingName, string fallback)
        {
            var settings = PlayerSettingsManager.GetSettings(playerIdx);
            if (settings.KeyBindings.ContainsKey(bindingName))
            {
                var k = settings.KeyBindings[bindingName];
                return k == Key.None ? "None" : k.ToString();
            }
            return fallback;
        }

        /// <summary>
        /// Returns the default Key for a given player and binding name.
        /// These defaults are used for collision detection even when not stored.
        /// </summary>
        private Key GetDefaultKey(int playerIdx, string bindingName)
        {
            // Player 1 defaults: J K D F
            // Player 2 defaults: O P Q W
            if (playerIdx == 0)
            {
                switch (bindingName)
                {
                    case "Blue1": return PlayerSettingsManager.GetSettings(0).KeyBindings["Blue1"];
                    case "Blue2": return PlayerSettingsManager.GetSettings(0).KeyBindings["Blue2"];
                    case "Red1": return PlayerSettingsManager.GetSettings(0).KeyBindings["Red1"];
                    case "Red2": return PlayerSettingsManager.GetSettings(0).KeyBindings["Red2"];
                }
            }
            else
            {
                switch (bindingName)
                {
                    case "Blue1": return PlayerSettingsManager.GetSettings(1).KeyBindings["Blue1"];
                    case "Blue2": return PlayerSettingsManager.GetSettings(1).KeyBindings["Blue2"];
                    case "Red1": return PlayerSettingsManager.GetSettings(1).KeyBindings["Red1"];
                    case "Red2": return PlayerSettingsManager.GetSettings(1).KeyBindings["Red2"];
                }
            }
            return Key.None;
        }

        /// <summary>
        /// Set a binding with global collision detection that includes defaults.
        /// Resolution: assign to target and explicitly clear (persist Key.None) every conflicting binding (stored or default).
        /// </summary>
        private void SetBinding(Key newKey)
        {
            if (_listeningFor == null) return;

            int targetPlayer = _listeningFor.StartsWith("P1") ? 0 : 1;
            string targetBinding = _listeningFor.Contains("Blue1") ? "Blue1" :
                                   _listeningFor.Contains("Blue2") ? "Blue2" :
                                   _listeningFor.Contains("Red1") ? "Red1" : "Red2";

            // Load target settings and current stored key (if any)
            var targetSettings = PlayerSettingsManager.GetSettings(targetPlayer);
            Key? oldKey = targetSettings.KeyBindings.ContainsKey(targetBinding)
                ? (Key?)targetSettings.KeyBindings[targetBinding]
                : null;

            // No-op if pressing same key already stored on target
            if (oldKey.HasValue && oldKey.Value == newKey)
            {
                _listeningFor = null;
                RefreshBindings();
                return;
            }

            // Build a stable snapshot that includes defaults for bindings that are not stored.
            // Snapshot entries: (playerIdx, bindingName, keyValue, wasStored)
            var snapshot = new List<Tuple<int, string, Key, bool>>();
            for (int p = 0; p <= 1; p++)
            {
                var s = PlayerSettingsManager.GetSettings(p);
                foreach (var bn in BindingNames)
                {
                    if (s.KeyBindings.ContainsKey(bn))
                    {
                        snapshot.Add(Tuple.Create(p, bn, s.KeyBindings[bn], true));
                    }
                    else
                    {
                        // include default as a virtual binding (wasStored = false)
                        snapshot.Add(Tuple.Create(p, bn, GetDefaultKey(p, bn), false));
                    }
                }
            }

            // Find conflicts in snapshot (exclude the target binding itself)
            var conflicts = snapshot
                .Where(t => t.Item3 == newKey && !(t.Item1 == targetPlayer && t.Item2 == targetBinding))
                .Select(t => Tuple.Create(t.Item1, t.Item2, t.Item4)) // (player, binding, wasStored)
                .ToList();

            if (conflicts.Count == 0)
            {
                // No conflict: assign directly to target and persist
                targetSettings.KeyBindings[targetBinding] = newKey;
                PlayerSettingsManager.UpdateSettings(targetPlayer, targetSettings);
            }
            else
            {
                // Build readable conflict list
                var conflictList = string.Join("\n", conflicts.Select(c =>
                    $"- Player {c.Item1 + 1} - {c.Item2}"));

                var result = MessageBox.Show(
                    $"Key '{newKey}' is already bound to:\n{conflictList}\n\n" +
                    $"Do you want to assign it to Player {targetPlayer + 1} - {targetBinding} and clear it from the others?",
                    "Key Conflict",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // For each conflict (stored or default), persist an explicit cleared state by setting Key.None
                    foreach (var c in conflicts)
                    {
                        int pIdx = c.Item1;
                        string bName = c.Item2;

                        var s = PlayerSettingsManager.GetSettings(pIdx);

                        // Persist explicit cleared state (Key.None) so the default no longer applies
                        if (!s.KeyBindings.ContainsKey(bName))
                        {
                            // add explicit cleared entry
                            s.KeyBindings.Add(bName, Key.None);
                        }
                        else
                        {
                            // overwrite stored binding with Key.None
                            s.KeyBindings[bName] = Key.None;
                        }

                        PlayerSettingsManager.UpdateSettings(pIdx, s);
                    }

                    // Assign new key to target (reload target in case it was changed above)
                    targetSettings = PlayerSettingsManager.GetSettings(targetPlayer);
                    targetSettings.KeyBindings[targetBinding] = newKey;
                    PlayerSettingsManager.UpdateSettings(targetPlayer, targetSettings);
                }
                else
                {
                    // Cancel: do nothing
                }
            }

            _listeningFor = null;
            RefreshBindings();
        }

        /// <summary>
        /// Returns all current conflicts (live) for compatibility if needed elsewhere.
        /// </summary>
        private List<Tuple<int, string>> FindKeyConflicts(Key key, int excludePlayerIdx, string excludeBindingKey)
        {
            var conflicts = new List<Tuple<int, string>>();

            for (int playerIdx = 0; playerIdx <= 1; playerIdx++)
            {
                var settings = PlayerSettingsManager.GetSettings(playerIdx);
                foreach (var kvp in settings.KeyBindings)
                {
                    if (playerIdx == excludePlayerIdx && kvp.Key == excludeBindingKey)
                        continue;

                    if (kvp.Value == key)
                        conflicts.Add(Tuple.Create(playerIdx, kvp.Key));
                }

                // Also check defaults for bindings that are not stored
                foreach (var bn in BindingNames)
                {
                    if (!settings.KeyBindings.ContainsKey(bn))
                    {
                        var def = GetDefaultKey(playerIdx, bn);
                        if (def == key && !(playerIdx == excludePlayerIdx && bn == excludeBindingKey))
                            conflicts.Add(Tuple.Create(playerIdx, bn));
                    }
                }
            }

            return conflicts;
        }

        public bool IsListening => _listeningFor != null;
        public string ListeningFor => _listeningFor;

        private void RefreshBindings()
        {
            OnPropertyChanged(nameof(P1Blue1));
            OnPropertyChanged(nameof(P1Blue2));
            OnPropertyChanged(nameof(P1Red1));
            OnPropertyChanged(nameof(P1Red2));
            OnPropertyChanged(nameof(P2Blue1));
            OnPropertyChanged(nameof(P2Blue2));
            OnPropertyChanged(nameof(P2Red1));
            OnPropertyChanged(nameof(P2Red2));
        }

        /// <summary>
        /// Disallow modifier-only keys (Ctrl/Alt/Shift) and Key.None from being assigned interactively.
        /// Key.None is still used internally as a persisted "cleared" sentinel.
        /// </summary>
        private bool IsAssignableKey(Key key)
        {
            if (key == Key.None) return false;

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return false;
            }

            return true;
        }
    }
}
