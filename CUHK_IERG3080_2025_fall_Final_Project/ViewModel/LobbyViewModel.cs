using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    public sealed class LobbyViewModel : ViewModelBase
    {
        public OnlineSession Session { get; } = new OnlineSession();

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ShareAddresses { get; } = new ObservableCollection<string>();

        private string _selectedShareAddress;
        public string SelectedShareAddress
        {
            get { return _selectedShareAddress; }
            set { _selectedShareAddress = value; OnPropertyChanged(nameof(SelectedShareAddress)); }
        }

        private string _name = "Player";
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _hostIp = "127.0.0.1";
        public string HostIp
        {
            get { return _hostIp; }
            set { _hostIp = value; OnPropertyChanged(nameof(HostIp)); }
        }

        private string _portText = "5050";
        public string PortText
        {
            get { return _portText; }
            set { _portText = value; OnPropertyChanged(nameof(PortText)); }
        }

        private string _roomId = "room1";
        public string RoomId
        {
            get { return _roomId; }
            set { _roomId = value; OnPropertyChanged(nameof(RoomId)); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText
        {
            get { return IsConnected ? ("Connected as Player " + Session.LocalSlot) : "Not connected"; }
        }

        // 你的 RelayCommand<T> 只有 1 参数构造函数，所以我们只传 execute
        public RelayCommand<object> StartHostCommand { get; }
        public RelayCommand<object> JoinHostCommand { get; }
        public RelayCommand<object> CopyAddressCommand { get; }
        public RelayCommand<object> OkCommand { get; }
        public RelayCommand<object> CloseCommand { get; }

        public event Action<bool> RequestClose;

        public LobbyViewModel()
        {
            Session.OnLog += s => UI(() => Logs.Add(s));

            Session.OnConnected += slot =>
            {
                UI(() =>
                {
                    IsConnected = true;
                    Logs.Add("[Lobby] Connected (slot=" + slot + ")");
                    RefreshShareAddresses();
                });
            };

            Session.OnDisconnected += reason =>
            {
                UI(() =>
                {
                    IsConnected = false;
                    Logs.Add("[Lobby] Disconnected: " + reason);
                });
            };

            StartHostCommand = new RelayCommand<object>(async o => await StartHostAsync());
            JoinHostCommand = new RelayCommand<object>(async o => await JoinHostAsync());
            CopyAddressCommand = new RelayCommand<object>(o => CopySelectedAddress());

            OkCommand = new RelayCommand<object>(o =>
            {
                if (!IsConnected)
                {
                    Logs.Add("[Lobby] Not connected yet.");
                    return;
                }
                if (RequestClose != null) RequestClose(true);
            });

            CloseCommand = new RelayCommand<object>(o =>
            {
                if (RequestClose != null) RequestClose(false);
            });
        }

        private async Task StartHostAsync()
        {
            int port;
            if (!TryParsePort(out port)) return;

            Logs.Add("[Lobby] Starting host...");
            try
            {
                await Session.StartHostAsync(port, Name, RoomId);
                RefreshShareAddresses();
            }
            catch (Exception ex)
            {
                Logs.Add("[Error] " + ex.Message);
            }
        }

        private async Task JoinHostAsync()
        {
            int port;
            if (!TryParsePort(out port)) return;

            Logs.Add("[Lobby] Joining " + HostIp + ":" + port + " ...");
            try
            {
                await Session.JoinHostAsync(HostIp, port, Name);
            }
            catch (Exception ex)
            {
                Logs.Add("[Error] " + ex.Message);
            }
        }

        private void CopySelectedAddress()
        {
            if (string.IsNullOrWhiteSpace(SelectedShareAddress)) return;
            try
            {
                Clipboard.SetText(SelectedShareAddress);
                Logs.Add("[Lobby] Address copied");
            }
            catch { }
        }

        private void RefreshShareAddresses()
        {
            ShareAddresses.Clear();
            foreach (var a in Session.ShareAddresses)
                ShareAddresses.Add(a);

            if (ShareAddresses.Count > 0 && string.IsNullOrEmpty(SelectedShareAddress))
                SelectedShareAddress = ShareAddresses[0];
        }

        private bool TryParsePort(out int port)
        {
            if (!int.TryParse(PortText, out port))
            {
                Logs.Add("[Error] Invalid port");
                return false;
            }
            return true;
        }

        private static void UI(Action a)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess()) a();
                else Application.Current.Dispatcher.Invoke(a);
            }
            else a();
        }

        public void OnWindowClosed()
        {
            // 暂时不 Dispose Session，避免 OK 之后断线
        }
    }
}
