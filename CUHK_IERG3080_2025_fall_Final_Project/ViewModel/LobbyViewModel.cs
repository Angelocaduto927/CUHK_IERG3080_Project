using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        // ✅ 对外暴露 ICommand（最稳）
        public ICommand StartHostCommand { get; }
        public ICommand JoinHostCommand { get; }
        public ICommand CopyAddressCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CloseCommand { get; }

        // Window 会订阅这个事件来 Close + DialogResult
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

            StartHostCommand = new RelayCommand(
                 _ => { _ = StartHostAsync(); },
                 _ => !IsBusy && !IsConnected && !Session.IsHost);

            JoinHostCommand = new RelayCommand(
                _ => { _ = JoinHostAsync(); },
                _ => !IsBusy && !IsConnected && !Session.IsHost);

            CopyAddressCommand = new RelayCommand(_ => CopySelectedAddress());

            OkCommand = new RelayCommand(_ =>
            {
                if (!IsConnected)
                {
                    Logs.Add("[Lobby] Not connected yet.");
                    return;
                }
                if (RequestClose != null) RequestClose(true);
            });

            CloseCommand = new RelayCommand(_ =>
            {
                if (RequestClose != null) RequestClose(false);
            });
        }

        private async Task StartHostAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                int port;
                if (!TryParsePort(out port)) return;

                Logs.Add("[Lobby] Starting host...");
                await Session.StartHostAsync(port, Name, RoomId);
                RefreshShareAddresses();
            }
            catch (Exception ex)
            {
                Logs.Add("[Error] " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task JoinHostAsync()
        {
            if (IsBusy) return;
            if (Session.IsHost)
            {
                Logs.Add("[Lobby] You are hosting already. Please open a 2nd instance to join.");
                return;
            }

            IsBusy = true;
            try
            {
                int defaultPort;
                if (!TryParsePort(out defaultPort)) return;

                string host;
                int port;

                if (!TryParseEndpoint(HostIp, defaultPort, out host, out port))
                {
                    Logs.Add("[Error] Invalid address. Use: 192.168.x.x or 192.168.x.x:5050");
                    return;
                }

                HostIp = host;
                PortText = port.ToString();

                Logs.Add("[Lobby] Joining " + host + ":" + port + " ...");
                await Session.JoinHostAsync(host, port, Name);
            }
            catch (Exception ex)
            {
                Logs.Add("[Error] " + ex.Message);
            }
            finally
            {
                IsBusy = false;
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

        private bool TryParseEndpoint(string input, int defaultPort, out string host, out int port)
        {
            host = "";
            port = defaultPort;

            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            // 兼容中文冒号：192.168.1.5：5050
            input = input.Replace('：', ':');

            // 支持用户粘贴 http://192.168.1.5:5050/xxx
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Uri uri;
                if (Uri.TryCreate(input, UriKind.Absolute, out uri))
                {
                    host = uri.Host;
                    port = uri.Port;
                    return true;
                }
                return false;
            }

            // 去掉可能的路径部分 192.168.1.5:5050/abc
            int slash = input.IndexOf('/');
            if (slash >= 0) input = input.Substring(0, slash);

            // 支持 host:port（只处理一个冒号的情况，避免 IPv6 麻烦）
            int firstColon = input.IndexOf(':');
            int lastColon = input.LastIndexOf(':');
            if (firstColon > 0 && firstColon == lastColon)
            {
                string h = input.Substring(0, firstColon).Trim();
                string pStr = input.Substring(firstColon + 1).Trim();

                int p;
                if (!string.IsNullOrEmpty(h) && int.TryParse(pStr, out p))
                {
                    host = h;
                    port = p;
                    return true;
                }
            }

            // 否则就是纯 host/ip
            host = input;
            port = defaultPort;
            return true;
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                CommandManager.InvalidateRequerySuggested();
            }
        }

    }

}
