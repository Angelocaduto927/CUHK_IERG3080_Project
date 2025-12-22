using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SettingVM : ViewModelBase, IDisposable
    {
        private readonly Action _navigationBack;
        private readonly OnlineSession _session;

        private bool IsOnlineMultiplayer =>
            GameModeManager.CurrentMode is OnlineMultiPlayerMode
            && _session != null
            && _session.IsConnected;

        private int LocalPlayerIndex => (_session != null) ? (_session.LocalSlot - 1) : -1;

        private bool CanEditSpeedForPlayerIndex(int playerIndex)
        {
            if (!IsOnlineMultiplayer) return true;
            return playerIndex == LocalPlayerIndex;
        }

        private void RefreshSpeedFromSettings()
        {
            _player1Speed = PlayerSettingsManager.GetSettings(0).Speed;
            _player2Speed = PlayerSettingsManager.GetSettings(1).Speed;
            OnPropertyChanged(nameof(Player1Speed));
            OnPropertyChanged(nameof(Player2Speed));
        }

        private void OnPlayerSettingFromNet(CUHK_IERG3080_2025_fall_Final_Project.Shared.UpdatePlayerSettingMsg msg)
        {
            // ✅ 只在 Online 模式下需要刷新 Setting 页面的显示
            if (!IsOnlineMultiplayer) return;

            // OnlineSession.ApplyPlayerSetting 已经写入 PlayerSettingsManager，这里只需要刷新 VM->UI
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                RefreshSpeedFromSettings();
            }));
        }

        private double _player1Speed;
        public double Player1Speed
        {
            get => _player1Speed;
            set
            {
                // ✅ Online：P1 只能由 Host(=slot1) 改；Joiner 在 UI 上强制回滚显示
                if (!CanEditSpeedForPlayerIndex(0))
                {
                    RefreshSpeedFromSettings();
                    return;
                }

                _player1Speed = value;

                var settings = PlayerSettingsManager.GetSettings(0);
                settings.Speed = value;
                PlayerSettingsManager.UpdateSettings(0, settings);
                OnPropertyChanged(nameof(Player1Speed));

                UpdateRunningGameSpeed(0, value);
                SyncOnlineSpeedIfLocal(0, value);
            }
        }

        private double _player2Speed;
        public double Player2Speed
        {
            get => _player2Speed;
            set
            {
                // ✅ Online：P2 只能由 Joiner(=slot2) 改；Host 在 UI 上强制回滚显示
                if (!CanEditSpeedForPlayerIndex(1))
                {
                    RefreshSpeedFromSettings();
                    return;
                }

                _player2Speed = value;

                var settings = PlayerSettingsManager.GetSettings(1);
                settings.Speed = value;
                PlayerSettingsManager.UpdateSettings(1, settings);
                OnPropertyChanged(nameof(Player2Speed));

                UpdateRunningGameSpeed(1, value);
                SyncOnlineSpeedIfLocal(1, value);
            }
        }

        private void UpdateRunningGameSpeed(int playerIndex, double newSpeed)
        {
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

        private void SyncOnlineSpeedIfLocal(int playerIndex, double speed)
        {
            // ✅ 只在 Online Multiplayer 模式同步速度
            if (!IsOnlineMultiplayer) return;

            int localIdx = LocalPlayerIndex;
            if (localIdx != playerIndex) return;

            _ = _session.SendUpdatePlayerSettingAsync(_session.LocalSlot, speed);
        }

        // Player 1 Key Bindings (display)
        public string P1Blue1 => _listeningFor == "P1Blue1" ? "Enter a key..." : GetBindingDisplay(0, "Blue1", "J");
        public string P1Blue2 => _listeningFor == "P1Blue2" ? "Enter a key..." : GetBindingDisplay(0, "Blue2", "K");
        public string P1Red1 => _listeningFor == "P1Red1" ? "Enter a key..." : GetBindingDisplay(0, "Red1", "D");
        public string P1Red2 => _listeningFor == "P1Red2" ? "Enter a key..." : GetBindingDisplay(0, "Red2", "F");

        public string P2Blue1 => _listeningFor == "P2Blue1" ? "Enter a key..." : GetBindingDisplay(1, "Blue1", "O");
        public string P2Blue2 => _listeningFor == "P2Blue2" ? "Enter a key..." : GetBindingDisplay(1, "Blue2", "P");
        public string P2Red1 => _listeningFor == "P2Red1" ? "Enter a key..." : GetBindingDisplay(1, "Red1", "Q");
        public string P2Red2 => _listeningFor == "P2Red2" ? "Enter a key..." : GetBindingDisplay(1, "Red2", "W");

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

        private static readonly string[] BindingNames = new[] { "Blue1", "Blue2", "Red1", "Red2" };

        public SettingVM() : this(null) { }

        public SettingVM(Action navigationBack)
        {
            _navigationBack = navigationBack;
            _session = GameModeManager.OnlineSession;

            _player1Speed = PlayerSettingsManager.GetSettings(0).Speed;
            _player2Speed = PlayerSettingsManager.GetSettings(1).Speed;

            // ✅ 只在 Online Multiplayer 才订阅网络设置更新
            if (IsOnlineMultiplayer)
            {
                _session.OnPlayerSetting += OnPlayerSettingFromNet;
            }

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

        public void Dispose()
        {
            // ✅ NavigationVM 切页会 Dispose 旧 VM
            if (_session != null)
            {
                try { _session.OnPlayerSetting -= OnPlayerSettingFromNet; } catch { }
            }
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

            if (key == Key.Escape)
            {
                _listeningFor = null;
                RefreshBindings();
                e.Handled = true;
                return;
            }

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
            RefreshBindings();
        }

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

        private Key GetDefaultKey(int playerIdx, string bindingName)
        {
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

        private void SetBinding(Key newKey)
        {
            if (_listeningFor == null) return;

            int targetPlayer = _listeningFor.StartsWith("P1") ? 0 : 1;
            string targetBinding = _listeningFor.Contains("Blue1") ? "Blue1" :
                                   _listeningFor.Contains("Blue2") ? "Blue2" :
                                   _listeningFor.Contains("Red1") ? "Red1" : "Red2";

            var targetSettings = PlayerSettingsManager.GetSettings(targetPlayer);
            Key? oldKey = targetSettings.KeyBindings.ContainsKey(targetBinding)
                ? (Key?)targetSettings.KeyBindings[targetBinding]
                : null;

            if (oldKey.HasValue && oldKey.Value == newKey)
            {
                _listeningFor = null;
                RefreshBindings();
                return;
            }

            var snapshot = new List<Tuple<int, string, Key, bool>>();
            for (int p = 0; p <= 1; p++)
            {
                var s = PlayerSettingsManager.GetSettings(p);
                foreach (var bn in BindingNames)
                {
                    if (s.KeyBindings.ContainsKey(bn))
                        snapshot.Add(Tuple.Create(p, bn, s.KeyBindings[bn], true));
                    else
                    {
                        snapshot.Add(Tuple.Create(p, bn, GetDefaultKey(p, bn), false));
                }
            }

            var conflicts = snapshot
                .Where(t => t.Item3 == newKey && !(t.Item1 == targetPlayer && t.Item2 == targetBinding))
                .Select(t => Tuple.Create(t.Item1, t.Item2, t.Item4))
                .ToList();

            if (conflicts.Count == 0)
            {
                targetSettings.KeyBindings[targetBinding] = newKey;
                PlayerSettingsManager.UpdateSettings(targetPlayer, targetSettings);
            }
            else
            {
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
                    foreach (var c in conflicts)
                    {
                        int pIdx = c.Item1;
                        string bName = c.Item2;

                        var s = PlayerSettingsManager.GetSettings(pIdx);

                        if (!s.KeyBindings.ContainsKey(bName))
                        {
                            s.KeyBindings.Add(bName, Key.None);
                        else
                        {
                            s.KeyBindings[bName] = Key.None;

                        PlayerSettingsManager.UpdateSettings(pIdx, s);
                    }

                    targetSettings = PlayerSettingsManager.GetSettings(targetPlayer);
                    targetSettings.KeyBindings[targetBinding] = newKey;
                    PlayerSettingsManager.UpdateSettings(targetPlayer, targetSettings);
                }
                else
                {
                }
            }

            _listeningFor = null;
            RefreshBindings();
        }

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

        private bool IsAssignableKey(Key key)
        {
            if (key == Key.None) return false;

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
                return false;

            return true;
        }
    }
}