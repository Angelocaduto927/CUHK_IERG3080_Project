using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Networking
{
    public sealed class OnlineSession : IDisposable
    {
        private TcpHost _host;
        private TcpJoiner _joiner;

        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }

        public int LocalSlot { get; private set; } = 0;
        public string LocalName { get; private set; } = "Player";
        public string RoomId { get; private set; } = "room1";

        public int Port { get; private set; } = 5050;

        public string[] ShareAddresses
        {
            get
            {
                return GetLanIPv4Addresses()
                    .Select(ip => ip + ":" + Port)
                    .Prepend("localhost:" + Port)
                    .Distinct()
                    .ToArray();
            }
        }

        public event Action<string> OnLog;
        public event Action<int> OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<SelectSongMsg> OnSelectSong;
        public event Action<StartMsg> OnStart;
        public event Action<InputMsg> OnInput;

        public async Task StartHostAsync(int port, string name, string roomId = "room1")
        {
            Cleanup();

            Port = port;
            LocalName = name;
            RoomId = roomId;

            IsHost = true;
            LocalSlot = 1;

            _host = new TcpHost { RoomId = roomId };
            _host.OnLog += Log;
            _host.OnClientConnected += clientName =>
            {
                IsConnected = true;
                Log("[Session] Client '" + clientName + "' connected.");
                if (OnConnected != null) OnConnected(LocalSlot);
            };
            _host.OnClientDisconnected += () =>
            {
                IsConnected = false;
                if (OnDisconnected != null) OnDisconnected("Client disconnected");
            };
            _host.OnMessage += HandleIncoming;

            await _host.StartAsync(port);
            Log("[Session] Host started.");
            Log("[Session] Share addresses:");
            foreach (var a in ShareAddresses) Log("  " + a);
        }

        public async Task JoinHostAsync(string hostIp, int port, string name)
        {
            Cleanup();

            Port = port;
            LocalName = name;

            IsHost = false;

            _joiner = new TcpJoiner();
            _joiner.OnLog += Log;
            _joiner.OnJoinOk += ok =>
            {
                LocalSlot = ok.Slot;
                RoomId = ok.RoomId;
                IsConnected = true;
                if (OnConnected != null) OnConnected(LocalSlot);
            };
            _joiner.OnJoinRejected += reason =>
            {
                IsConnected = false;
                if (OnDisconnected != null) OnDisconnected("Join rejected: " + reason);
            };
            _joiner.OnDisconnected += () =>
            {
                IsConnected = false;
                if (OnDisconnected != null) OnDisconnected("Disconnected");
            };
            _joiner.OnMessage += HandleIncoming;

            await _joiner.ConnectAndJoinAsync(hostIp, port, name);
        }

        public Task SendSelectSongAsync(string songId, string difficulty)
        {
            return SendAsync(new SelectSongMsg { SongId = songId, Difficulty = difficulty });
        }

        public Task SendStartAsync(int startInMs = 2000)
        {
            return SendAsync(new StartMsg { StartInMs = startInMs });
        }

        public Task SendInputAsync(int slot, string noteType, double atMs)
        {
            return SendAsync(new InputMsg { Slot = slot, NoteType = noteType, AtMs = atMs });
        }

        public async Task LeaveAsync(string reason = "Leave")
        {
            try { await SendAsync(new AbortMsg { Reason = reason }); } catch { }
            Cleanup();
            if (OnDisconnected != null) OnDisconnected(reason);
        }

        private Task SendAsync(NetMsg msg)
        {
            if (IsHost)
                return _host != null ? _host.SendAsync(msg) : Task.CompletedTask;
            else
                return _joiner != null ? _joiner.SendAsync(msg) : Task.CompletedTask;
        }

        private void HandleIncoming(NetMsg msg)
        {
            var sel = msg as SelectSongMsg;
            if (sel != null) { if (OnSelectSong != null) OnSelectSong(sel); return; }

            var st = msg as StartMsg;
            if (st != null) { if (OnStart != null) OnStart(st); return; }

            var inp = msg as InputMsg;
            if (inp != null) { if (OnInput != null) OnInput(inp); return; }

            var ab = msg as AbortMsg;
            if (ab != null)
            {
                IsConnected = false;
                if (OnDisconnected != null) OnDisconnected(ab.Reason);
                Cleanup();
                return;
            }

            var sys = msg as SystemMsg;
            if (sys != null) { Log("[SYS] " + sys.Text); return; }
        }

        private void Log(string s)
        {
            if (OnLog != null) OnLog(s);
        }

        private void Cleanup()
        {
            try { if (_host != null) _host.Dispose(); } catch { }
            try { if (_joiner != null) _joiner.Dispose(); } catch { }
            _host = null;
            _joiner = null;

            IsConnected = false;
            IsHost = false;
            LocalSlot = 0;
        }

        public void Dispose()
        {
            Cleanup();
        }

        private static string[] GetLanIPv4Addresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ua => ua.Address.ToString())
                    .Where(ip => !ip.StartsWith("169.254."))
                    .Distinct()
                    .ToArray();
            }
            catch
            {
                return new string[0];
            }
        }
    }
}
