using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    public partial class SongSelection : UserControl
    {
        private OnlineSession _session;

        public SongSelection()
        {
            InitializeComponent();
            AudioManager.PlayBackgroundMusic();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 这里假设你已经把 Lobby 里建立好的 session 放到 GameModeManager.OnlineSession 里
            _session = GameModeManager.OnlineSession;
            if (_session != null)
            {
                _session.OnSelectSong += Session_OnSelectSong;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_session != null)
            {
                _session.OnSelectSong -= Session_OnSelectSong;
                _session = null;
            }
        }

        // joiner 收到 host 的选歌消息后，同步导航到 DifficultySelection
        private void Session_OnSelectSong(SelectSongMsg msg)
        {
            try
            {
                if (_session == null) return;

                // 只让 joiner 响应；host 自己会点
                if (_session.IsHost) return;

                if (msg == null || string.IsNullOrWhiteSpace(msg.SongId)) return;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var navVM = Application.Current.MainWindow?.DataContext as NavigationVM;
                    if (navVM != null)
                    {
                        navVM.DifficultySelectionCommand.Execute(msg.SongId);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SongSelection] Session_OnSelectSong error: " + ex);
            }
        }

        private async void OnSong1Click(object sender, MouseButtonEventArgs e)
        {
            await HostPickSongAndGoAsync("One_Last_Kiss");
        }

        private async void OnSong2Click(object sender, MouseButtonEventArgs e)
        {
            await HostPickSongAndGoAsync("IRIS_OUT");
        }

        private async Task HostPickSongAndGoAsync(string songId)
        {
            var navVM = Application.Current.MainWindow?.DataContext as NavigationVM;
            if (navVM == null) return;

            // 在线模式：只允许 host 点选来驱动全局
            var s = GameModeManager.OnlineSession;
            if (s != null && s.IsConnected)
            {
                if (!s.IsHost)
                {
                    // joiner 点击就忽略（避免两边抢状态）
                    return;
                }

                try
                {
                    // difficulty 暂时空着，等下一页再补
                    await s.SendSelectSongAsync(songId, "");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[SongSelection] SendSelectSong failed: " + ex.Message);
                }
            }

            // 本机导航
            navVM.DifficultySelectionCommand.Execute(songId);
        }
    }
}
