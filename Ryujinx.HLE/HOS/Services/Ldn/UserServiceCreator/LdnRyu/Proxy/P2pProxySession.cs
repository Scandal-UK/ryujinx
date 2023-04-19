using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxySession : IDisposable
    {
        public uint VirtualIpAddress { get; private set; }

        private readonly RyuLdnProtocol _protocol = new();

        private readonly P2pProxyServer _parent;

        private bool _active = true;
        private readonly Thread _packetThread;
        private readonly ConcurrentQueue<(byte[], int, int)> _packets = new();
        private readonly AutoResetEvent _packetReady = new(false);

        internal readonly IPEndPoint Endpoint;

        public P2pProxySession(P2pProxyServer parent, IPEndPoint endpoint)
        {
            _parent = parent;
            Endpoint = endpoint;

            _protocol.Connected         += _parent.HandleConnected;
            _protocol.ProxyConfig       += HandleProxyConfig;
            _protocol.ProxyDisconnect   += HandleProxyDisconnect;
            _protocol.ProxyData         += HandleProxyData;
            _protocol.ProxyConnectReply += HandleProxyConnectReply;
            _protocol.ProxyConnect      += HandleProxyConnect;
            _protocol.Any               += HandleAny;

            _packetThread = new Thread(ReadPackets)
            {
                Name = "P2pProxySessionReadPacketsThread"
            };
            _packetThread.Start();
        }

        private void HandleProxyConfig(IPEndPoint endpoint, LdnHeader header, ProxyConfig config)
        {
            _parent.HandleProxyConfig(this, endpoint, header, config);
        }

        private void HandleAny(IPEndPoint endpoint, LdnHeader header)
        {
            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Session received packet from {endpoint} of type: {(PacketId)header.Type}");
        }

        private void ReadPackets()
        {
            while (_active)
            {
                _packetReady.WaitOne();

                while (_packets.TryDequeue(out var result))
                {
                    try
                    {
                        _protocol.Read(Endpoint, result.Item1, result.Item2, result.Item3);
                    }
                    catch (Exception e)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"P2pProxySession caught an error while reading packet: {e}");
                        _parent.DisconnectProxyClient(this);
                    }
                }
            }
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
            _packets.Enqueue((buffer, offset, size));
            _packetReady.Set();
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

        public void Dispose()
        {
            _active = false;
            _packetReady.Set();

            _packetThread?.Join();
            _packetReady?.Dispose();
        }
    }
}
