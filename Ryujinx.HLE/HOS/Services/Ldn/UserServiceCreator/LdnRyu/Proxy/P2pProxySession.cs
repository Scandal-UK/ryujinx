using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using System;
using System.Net;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxySession
    {
        public uint VirtualIpAddress { get; private set; }

        private readonly RyuLdnProtocol _protocol = new();

        private readonly P2pProxyServer _parent;

        internal readonly IPEndPoint Endpoint;

        public P2pProxySession(P2pProxyServer parent, IPEndPoint endpoint)
        {
            _parent = parent;
            Endpoint = endpoint;

            _protocol.ProxyDisconnect   += HandleProxyDisconnect;
            _protocol.ProxyData         += HandleProxyData;
            _protocol.ProxyConnectReply += HandleProxyConnectReply;
            _protocol.ProxyConnect      += HandleProxyConnect;
        }

        public bool SendAsync(byte[] data)
        {
            return _parent.Master.SendAsync(Endpoint, data);
        }

        public void SetIpv4(uint ip)
        {
            VirtualIpAddress = ip;
        }

        public void OnReceived(byte[] buffer, int offset, int size)
        {
            try
            {
                _protocol.Read(Endpoint, buffer, offset, size);
            }
            catch (Exception)
            {
                _parent.DisconnectProxyClient(this);
            }
        }

        private void HandleProxyDisconnect(LdnHeader header, ProxyDisconnectMessage message)
        {
            _parent.HandleProxyDisconnect(this, header, message);
        }

        private void HandleProxyData(LdnHeader header, ProxyDataHeader message, byte[] data)
        {
            _parent.HandleProxyData(this, header, message, data);
        }

        private void HandleProxyConnectReply(LdnHeader header, ProxyConnectResponse data)
        {
            _parent.HandleProxyConnectReply(this, header, data);
        }

        private void HandleProxyConnect(IPEndPoint endpoint, LdnHeader header, ProxyConnectRequest message)
        {
            _parent.HandleProxyConnect(this, header, message);
        }
    }
}