using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;
using System;
using System.Net;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxyClient : IProxyClient, IDisposable
    {
        private const int FailureTimeout = 4000;

        public ProxyConfig ProxyConfig { get; private set; }

        private LdnMasterProxyClient _master;
        private IPEndPoint _endpoint;

        private ManualResetEvent _ready     = new(false);

        public P2pProxyClient(LdnMasterProxyClient master, IPEndPoint endpoint)
        {
            _master = master;
            _endpoint = endpoint;

            // ConnectAsync();
        }

        public void Dispose()
        {
            SocketHelpers.UnregisterProxy();
        }

        internal void HandleProxyConfig(LdnHeader header, ProxyConfig config)
        {
            ProxyConfig = config;

            SocketHelpers.RegisterProxy(new LdnProxy(config, this, _master.Protocol));

            SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyConfig, config));

            _ready.Set();
        }

        public bool EnsureProxyReady()
        {
            return _ready.WaitOne(FailureTimeout);
        }

        public bool PerformAuth(ExternalProxyConfig config)
        {
            SendAsync(RyuLdnProtocol.Encode(PacketId.ExternalProxy, config));

            return true;
        }

        public bool SendAsync(byte[] buffer)
        {
            return _master.SendAsync(_endpoint, buffer);
        }
    }
}