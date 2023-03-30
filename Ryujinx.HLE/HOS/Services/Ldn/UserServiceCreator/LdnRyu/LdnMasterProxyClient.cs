using NetCoreServer;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu
{
    class LdnMasterProxyClient : UdpServer, INetworkClient, IProxyClient
    {
        public bool NeedsRealId => true;

        private static InitializeMessage _initializeMemory;

        private const int InactiveTimeout = 6000;
        private const int FailureTimeout  = 4000;
        private const int ScanTimeout     = 1000;

        private bool         _useP2pProxy;
        private NetworkError _lastError;

        private ManualResetEvent _connected   = new(false);
        private ManualResetEvent _error       = new(false);
        private ManualResetEvent _scan        = new(false);
        private ManualResetEvent _reject      = new(false);
        private AutoResetEvent   _apConnected = new(false);

        internal readonly RyuLdnProtocol Protocol;
        private readonly NetworkTimeout _timeout;
        private readonly IPEndPoint _lanPlayHost;

        private readonly List<NetworkInfo> _availableGames = new();
        private DisconnectReason  _disconnectReason;

        private P2pProxyServer _hostedProxy;
        private P2pProxyClient _connectedProxy;

        private bool _networkConnected;

        private string _passphrase;
        private byte[] _gameVersion = new byte[0x10];

        private readonly HLEConfiguration _config;

        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        public ProxyConfig Config { get; private set; }

        public LdnMasterProxyClient(IPAddress address, int port, HLEConfiguration config) : base(NetworkHelpers.GetLocalInterface().Item2.Address, 0)
        {
            _lanPlayHost = new IPEndPoint(address, port);
            Protocol = new RyuLdnProtocol();
            _timeout  = new NetworkTimeout(InactiveTimeout, TimeoutConnection);

            Protocol.Initialize   += HandleInitialize;
            Protocol.Connected    += HandleConnected;
            Protocol.Reject       += HandleReject;
            Protocol.RejectReply  += HandleRejectReply;
            Protocol.SyncNetwork  += HandleSyncNetwork;
            Protocol.ProxyConfig  += HandleProxyConfig;
            Protocol.Disconnected += HandleDisconnected;

            Protocol.ScanReply     += HandleScanReply;
            Protocol.ScanReplyEnd  += HandleScanReplyEnd;
            Protocol.ExternalProxy += HandleExternalProxy;

            Protocol.Ping         += HandlePing;
            Protocol.NetworkError += HandleNetworkError;

            Protocol.Any          += HandleAny;

            _config = config;
            _useP2pProxy = !config.MultiplayerDisableP2p;
        }

        private void HandleAny(IPEndPoint endpoint, LdnHeader header)
        {
            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Received '{(PacketId)header.Type}' packet from: {endpoint}");
        }

        private void TimeoutConnection()
        {
            Stop();
        }

        private bool EnsureConnected()
        {
            if (IsStarted)
            {
                return true;
            }

            Start();

            int index = WaitHandle.WaitAny(new WaitHandle[] { _connected, _error }, FailureTimeout);

            if (IsStarted && index == 0)
            {
                UpdatePassphraseIfNeeded();

                return true;
            }

            return false;
        }

        private void UpdatePassphraseIfNeeded()
        {
            string passphrase = _config.MultiplayerLdnPassphrase ?? "";
            if (passphrase != _passphrase)
            {
                _passphrase = passphrase;

                SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Passphrase, StringUtils.GetFixedLengthBytes(passphrase, 0x80, Encoding.UTF8)));
            }
        }

        protected override void OnStarted()
        {
            ReceiveAsync();

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN UDP server created a new session with Id {Id}");

            SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Initialize, _initializeMemory));
        }

        protected override void OnStopped()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN UDP server closed a session with Id {Id}");

            _passphrase = null;

            if (_networkConnected)
            {
                DisconnectInternal();
            }
        }

        public void DisconnectAndStop()
        {
            _timeout.Dispose();

            Stop();

            Dispose();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            if (_hostedProxy is not null && _hostedProxy.ReceivePlayerPacket((IPEndPoint)endpoint, buffer, (int)offset, (int)size))
            {
                ReceiveAsync();

                return;
            }

            Thread thread = new(() => Protocol.Read((IPEndPoint)endpoint, buffer, (int)offset, (int)size))
            {
                Name = "LdnReceivedThread"
            };
            thread.Start();

            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN UDP server caught an error with code {error}");
        }

        private void HandleInitialize(IPEndPoint ipEndPoint, LdnHeader ldnHeader, InitializeMessage message)
        {
            _initializeMemory = message;
            _connected.Set();
        }

        private void HandleExternalProxy(IPEndPoint endpoint, LdnHeader ldnHeader, ExternalProxyConfig config)
        {
            int length = config.AddressFamily switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => 0
            };

            if (length == 0)
            {
                return; // Invalid external proxy.
            }

            IPAddress      address = new(config.ProxyIp.AsSpan()[..length].ToArray());
            P2pProxyClient proxy   = new(this, new IPEndPoint(address, config.ProxyPort));

            _connectedProxy = proxy;

            bool success = proxy.PerformAuth(config);

            if (!success)
            {
                DisconnectInternal();
            }
        }

        private void HandlePing(LdnHeader header, PingMessage ping)
        {
            if (ping.Requester == 0) // Server requested.
            {
                // Send the ping message back.

                SendAsync(RyuLdnProtocol.Encode(PacketId.Ping, ping));
            }
        }

        private void HandleNetworkError(LdnHeader header, NetworkErrorMessage error)
        {
            if (error.Error == NetworkError.PortUnreachable)
            {
                _useP2pProxy = false;
            }
            else
            {
                _lastError = error.Error;
            }
        }

        private NetworkError ConsumeNetworkError()
        {
            NetworkError result = _lastError;

            _lastError = NetworkError.None;

            return result;
        }

        private void HandleSyncNetwork(LdnHeader header, NetworkInfo info)
        {
            SendAsync(RyuLdnProtocol.Encode(PacketId.SyncNetwork, info));
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, true));
        }

        private void HandleConnected(LdnHeader header, NetworkInfo info)
        {
            _networkConnected = true;
            _disconnectReason = DisconnectReason.None;

            foreach (var node in info.Ldn.Nodes.AsSpan())
            {
                Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Node: {NetworkHelpers.ConvertUint(node.Ipv4Address)}");
            }

            _apConnected.Set();

            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, true));
        }

        private void HandleDisconnected(LdnHeader header, DisconnectMessage message)
        {
            DisconnectInternal();
        }

        private void HandleReject(LdnHeader header, RejectRequest reject)
        {
            // When the client receives a Reject request, we have been rejected and will be disconnected shortly.
            _disconnectReason = reject.DisconnectReason;
        }

        private void HandleRejectReply(LdnHeader header)
        {
            _reject.Set();
        }

        private void HandleScanReply(LdnHeader header, NetworkInfo info)
        {
            _availableGames.Add(info);
        }

        private void HandleScanReplyEnd(LdnHeader obj)
        {
            _scan.Set();
        }

        private void DisconnectInternal()
        {
            if (_networkConnected)
            {
                _networkConnected = false;

                // _hostedProxy?.Dispose();
                _hostedProxy = null;

                // _connectedProxy?.Dispose();
                _connectedProxy = null;

                _apConnected.Reset();

                NetworkChange?.Invoke(this, new NetworkChangeEventArgs(new NetworkInfo(), false, _disconnectReason));
            }
        }

        public void DisconnectNetwork()
        {
            if (_networkConnected)
            {
                SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Disconnect, new DisconnectMessage()));

                DisconnectInternal();
            }
        }

        public ResultCode Reject(DisconnectReason disconnectReason, uint nodeId)
        {
            if (_networkConnected)
            {
                _reject.Reset();

                SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Reject, new RejectRequest(disconnectReason, nodeId)));

                int index = WaitHandle.WaitAny(new WaitHandle[] { _reject, _error }, InactiveTimeout);

                if (index == 0)
                {
                    return (ConsumeNetworkError() != NetworkError.None) ? ResultCode.InvalidState : ResultCode.Success;
                }
            }

            return ResultCode.InvalidState;
        }

        public void SetAdvertiseData(byte[] data)
        {
            // TODO: validate we're the owner (the server will do this anyways tho)
            if (_networkConnected)
            {
                SendAsync(_lanPlayHost,RyuLdnProtocol.Encode(PacketId.SetAdvertiseData, data));
            }
        }

        public void SetGameVersion(byte[] versionString) {
            _gameVersion = versionString;

            if (_gameVersion.Length < 0x10)
            {
                Array.Resize(ref _gameVersion, 0x10);
            }
        }

        public void SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            // TODO: validate we're the owner (the server will do this anyways tho)
            if (_networkConnected)
            {
                SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.SetAcceptPolicy, new SetAcceptPolicyRequest
                {
                    StationAcceptPolicy = acceptPolicy
                }));
            }
        }

        private void DisposeProxy()
        {
            // _hostedProxy?.Dispose();
            _hostedProxy = null;
        }

        private void ConfigureAccessPoint()
        {
            _hostedProxy = new P2pProxyServer(this);
        }

        // private void ConfigureAccessPoint(ref RyuNetworkConfig request)
        // {
        //     _gameVersion.AsSpan().CopyTo(request.GameVersion.AsSpan());
        //
        //     if (_useP2pProxy)
        //     {
        //         // Before sending the request, attempt to set up a proxy server.
        //         // This can be on a range of private ports, which can be exposed on a range of public
        //         // ports via UPnP. If any of this fails, we just fall back to using the master server.
        //
        //         int i = 0;
        //         for (; i < P2pProxyServer.PrivatePortRange; i++)
        //         {
        //             _hostedProxy = new P2pProxyServer(this, (ushort)(P2pProxyServer.PrivatePortBase + i), _protocol);
        //
        //             try
        //             {
        //                 _hostedProxy.Start();
        //
        //                 break;
        //             }
        //             catch (SocketException e)
        //             {
        //                 _hostedProxy.Dispose();
        //                 _hostedProxy = null;
        //
        //                 if (e.SocketErrorCode != SocketError.AddressAlreadyInUse)
        //                 {
        //                     i = P2pProxyServer.PrivatePortRange; // Immediately fail.
        //                 }
        //             }
        //         }
        //
        //         bool openSuccess = i < P2pProxyServer.PrivatePortRange;
        //
        //         if (openSuccess)
        //         {
        //             Task<ushort> natPunchResult = _hostedProxy.NatPunch();
        //
        //             try
        //             {
        //                 if (natPunchResult.Result != 0)
        //                 {
        //                     // Tell the server that we are hosting the proxy.
        //                     request.ExternalProxyPort = natPunchResult.Result;
        //                 }
        //             }
        //             catch (Exception) { }
        //
        //             if (request.ExternalProxyPort == 0)
        //             {
        //                 Logger.Warning?.Print(LogClass.ServiceLdn, "Failed to open a port with UPnP for P2P connection. Proxying through the master server instead. Expect higher latency.");
        //                 _hostedProxy.Dispose();
        //             }
        //             else
        //             {
        //                 Logger.Info?.Print(LogClass.ServiceLdn, $"Created a wireless P2P network on port {request.ExternalProxyPort}.");
        //                 _hostedProxy.Start();
        //
        //                 (_, UnicastIPAddressInformation unicastAddress) = NetworkHelpers.GetLocalInterface();
        //
        //                 unicastAddress.Address.GetAddressBytes().AsSpan().CopyTo(request.PrivateIp.AsSpan());
        //                 request.InternalProxyPort = _hostedProxy.PrivatePort;
        //                 request.AddressFamily = unicastAddress.Address.AddressFamily;
        //             }
        //         }
        //         else
        //         {
        //             Logger.Warning?.Print(LogClass.ServiceLdn, "Cannot create a P2P server. Proxying through the master server instead. Expect higher latency.");
        //         }
        //     }
        // }

        private bool CreateNetworkCommon()
        {
            bool signalled = _apConnected.WaitOne(FailureTimeout);

            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"CreateNetworkCommon> signalled: {signalled}");

            if (!_useP2pProxy && _hostedProxy != null)
            {
                Logger.Warning?.Print(LogClass.ServiceLdn, "Locally hosted proxy server was not externally reachable. Proxying through the master server instead. Expect higher latency.");

                DisposeProxy();
            }

            if (signalled && _connectedProxy != null)
            {
                _connectedProxy.EnsureProxyReady();

                Config = _connectedProxy.ProxyConfig;
            }
            else
            {
                DisposeProxy();
            }

            return signalled;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            _timeout.DisableTimeout();

            ConfigureAccessPoint();

            if (!EnsureConnected())
            {
                DisposeProxy();

                return false;
            }

            SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.CreateAccessPoint, request, advertiseData));

            UpdatePassphraseIfNeeded();

            return CreateNetworkCommon();
        }

        public bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData)
        {
            _timeout.DisableTimeout();

            ConfigureAccessPoint();

            if (!EnsureConnected())
            {
                DisposeProxy();

                return false;
            }

            UpdatePassphraseIfNeeded();

            SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.CreateAccessPointPrivate, request, advertiseData));

            return CreateNetworkCommon();
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter)
        {
            if (!_networkConnected)
            {
                _timeout.RefreshTimeout();
            }

            _availableGames.Clear();

            int index = -1;

            if (EnsureConnected())
            {
                UpdatePassphraseIfNeeded();

                _scan.Reset();

                SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Scan, scanFilter));

                index = WaitHandle.WaitAny(new WaitHandle[] { _scan, _error }, ScanTimeout);
            }

            if (index != 0)
            {
                // An error occurred or timeout. Write 0 games.
                return Array.Empty<NetworkInfo>();
            }

            return _availableGames.ToArray();
        }

        private NetworkError ConnectCommon()
        {
            bool signalled = _apConnected.WaitOne(FailureTimeout);

            NetworkError error = ConsumeNetworkError();

            if (error != NetworkError.None)
            {
                return error;
            }

            if (signalled && _connectedProxy != null)
            {
                _connectedProxy.EnsureProxyReady();

                Config = _connectedProxy.ProxyConfig;
            }

            return signalled ? NetworkError.None : NetworkError.ConnectTimeout;
        }

        public NetworkError Connect(ConnectRequest request)
        {
            _timeout.DisableTimeout();

            if (!EnsureConnected())
            {
                return NetworkError.Unknown;
            }

            SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.Connect, request));

            return ConnectCommon();
        }

        public NetworkError ConnectPrivate(ConnectPrivateRequest request)
        {
            _timeout.DisableTimeout();

            if (!EnsureConnected())
            {
                return NetworkError.Unknown;
            }

            SendAsync(_lanPlayHost, RyuLdnProtocol.Encode(PacketId.ConnectPrivate, request));

            return ConnectCommon();
        }

        private void HandleProxyConfig(IPEndPoint endpoint, LdnHeader header, ProxyConfig config)
        {
            // TODO: Ensure this works
            if (_connectedProxy is not null)
            {
                Logger.Warning?.PrintMsg(LogClass.ServiceLdn, "here");
                _connectedProxy.HandleProxyConfig(header, config);
                return;
            }

            Config = config;

            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"HandleProxyConfig: {NetworkHelpers.ConvertUint(config.ProxyIp)}");

            SocketHelpers.RegisterProxy(new LdnProxy(config, this, Protocol));

            SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyConfig, config));
        }

        public bool SendAsync(byte[] buffer)
        {
            return SendAsync(_lanPlayHost, buffer);
        }
    }
}