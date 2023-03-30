using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxyServer
    {
        private const ushort AuthWaitSeconds = 1;

        private ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

        private ProxyConfig _config;
        private List<P2pProxySession> _players = new();

        private List<ExternalProxyToken> _waitingTokens = new();
        private AutoResetEvent _tokenEvent = new(false);

        private uint _broadcastAddress;

        internal LdnMasterProxyClient Master;

        public P2pProxyServer(LdnMasterProxyClient master)
        {
            Master = master;

            Master.Protocol.ExternalProxyState += HandleStateChange;
            Master.Protocol.ExternalProxyToken += HandleToken;
            Master.Protocol.ExternalProxy      += TryRegisterUser;
        }

        public bool ReceivePlayerPacket(IPEndPoint endpoint, byte[] buffer, int size, int offset)
        {
            var player = _players.SingleOrDefault(player => Equals(player.Endpoint, endpoint), null);

            if (player is null)
            {
                return false;
            }

            Thread thread = new(() => player.OnReceived(buffer, offset, size))
            {
                Name = "LdnPlayerReceivedThread"
            };
            thread.Start();

            return true;

        }

        private void HandleToken(LdnHeader header, ExternalProxyToken token)
        {
            _lock.EnterWriteLock();

            _waitingTokens.Add(token);

            _lock.ExitWriteLock();

            _tokenEvent.Set();
        }

        private void HandleStateChange(LdnHeader header, ExternalProxyConnectionState state)
        {
            if (!state.Connected)
            {
                _lock.EnterWriteLock();

                _waitingTokens.RemoveAll(token => token.VirtualIp == state.IpAddress);

                _players.RemoveAll(player =>
                {
                    if (player.VirtualIpAddress == state.IpAddress)
                    {
                        return true;
                    }

                    return false;
                });

                _lock.ExitWriteLock();
            }
        }

        public void Configure(ProxyConfig config)
        {
            _config           = config;
            _broadcastAddress = config.ProxyIp | (~config.ProxySubnetMask);
        }

        // public async Task<ushort> NatPunch()
        // {
        //     NatDiscoverer           discoverer = new NatDiscoverer();
        //     CancellationTokenSource cts        = new CancellationTokenSource(1000);
        //
        //     NatDevice device;
        //
        //     try
        //     {
        //         device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
        //     }
        //     catch (NatDeviceNotFoundException)
        //     {
        //         return 0;
        //     }
        //
        //     _publicPort = PublicPortBase;
        //
        //     for (int i = 0; i < PublicPortRange; i++)
        //     {
        //         try
        //         {
        //             _portMapping = new Mapping(Protocol.Tcp, PrivatePort, _publicPort, PortLeaseLength, "Ryujinx Local Multiplayer");
        //
        //             await device.CreatePortMapAsync(_portMapping);
        //
        //             break;
        //         }
        //         catch (MappingException)
        //         {
        //             _publicPort++;
        //         }
        //         catch (Exception)
        //         {
        //             return 0;
        //         }
        //
        //         if (i == PublicPortRange - 1)
        //         {
        //             _publicPort = 0;
        //         }
        //     }
        //
        //     if (_publicPort != 0)
        //     {
        //         _ = Task.Delay(PortLeaseRenew * 1000, _disposedCancellation.Token).ContinueWith((task) => Task.Run(RefreshLease));
        //     }
        //
        //     _natDevice = device;
        //
        //     return _publicPort;
        // }

        // Proxy handlers

        private void RouteMessage(P2pProxySession sender, ref ProxyInfo info, Action<P2pProxySession> action)
        {
            if (info.SourceIpV4 == 0)
            {
                // If they sent from a connection bound on 0.0.0.0, make others see it as them.
                info.SourceIpV4 = sender.VirtualIpAddress;
            }
            else if (info.SourceIpV4 != sender.VirtualIpAddress)
            {
                // Can't pretend to be somebody else.
                return;
            }

            uint destIp = info.DestIpV4;

            if (destIp == 0xc0a800ff)
            {
                destIp = _broadcastAddress;
            }

            bool isBroadcast = destIp == _broadcastAddress;

            _lock.EnterReadLock();

            if (isBroadcast)
            {
                _players.ForEach(action);
            }
            else
            {
                P2pProxySession target = _players.FirstOrDefault(player => player.VirtualIpAddress == destIp);

                if (target != null)
                {
                    action(target);
                }
            }

            _lock.ExitReadLock();
        }

        public void HandleProxyDisconnect(P2pProxySession sender, LdnHeader header, ProxyDisconnectMessage message)
        {
            RouteMessage(sender, ref message.Info, (target) =>
            {
                target.SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyDisconnect, message));
            });
        }

        public void HandleProxyData(P2pProxySession sender, LdnHeader header, ProxyDataHeader message, byte[] data)
        {
            RouteMessage(sender, ref message.Info, (target) =>
            {
                target.SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyData, message, data));
            });
        }

        public void HandleProxyConnectReply(P2pProxySession sender, LdnHeader header, ProxyConnectResponse message)
        {
            RouteMessage(sender, ref message.Info, (target) =>
            {
                target.SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyConnectReply, message));
            });
        }

        public void HandleProxyConnect(P2pProxySession sender, LdnHeader header, ProxyConnectRequest message)
        {
            RouteMessage(sender, ref message.Info, (target) =>
            {
                target.SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyConnect, message));
            });
        }

        // End proxy handlers

        // private async Task RefreshLease()
        // {
        //     if (_disposed || _natDevice == null)
        //     {
        //         return;
        //     }
        //
        //     try
        //     {
        //         await _natDevice.CreatePortMapAsync(_portMapping);
        //     }
        //     catch (Exception)
        //     {
        //
        //     }
        //
        //     _ = Task.Delay(PortLeaseRenew, _disposedCancellation.Token).ContinueWith((task) => Task.Run(RefreshLease));
        // }

        private void TryRegisterUser(IPEndPoint endpoint, LdnHeader header, ExternalProxyConfig config)
        {
            _lock.EnterWriteLock();

            // Attempt to find matching configuration. If we don't find one, wait for a bit and try again.
            // Woken by new tokens coming in from the master server.

            IPAddress address      = endpoint.Address;
            byte[]    addressBytes = ProxyHelpers.AddressTo16Byte(address);

            P2pProxySession session = new(this, endpoint);

            long time;
            long endTime = Stopwatch.GetTimestamp() + Stopwatch.Frequency * AuthWaitSeconds;

            do
            {
                for (int i = 0; i < _waitingTokens.Count; i++)
                {
                    ExternalProxyToken waitToken = _waitingTokens[i];

                    // Allow any client that has a private IP to connect. (indicated by the server as all 0 in the token)

                    bool isPrivate = waitToken.PhysicalIp.AsSpan().SequenceEqual(new byte[16]);
                    bool ipEqual   = isPrivate || waitToken.AddressFamily == address.AddressFamily && waitToken.PhysicalIp.AsSpan().SequenceEqual(addressBytes);

                    if (ipEqual && waitToken.Token.AsSpan().SequenceEqual(config.Token.AsSpan()))
                    {
                        // This is a match.

                        _waitingTokens.RemoveAt(i);

                        session.SetIpv4(waitToken.VirtualIp);

                        ProxyConfig pconfig = new ProxyConfig
                        {
                            ProxyIp = session.VirtualIpAddress,
                            ProxySubnetMask = 0xFFFF0000 // TODO: Use from server.
                        };

                        if (_players.Count == 0)
                        {
                            Configure(pconfig);
                        }

                        _players.Add(session);

                        session.SendAsync(RyuLdnProtocol.Encode(PacketId.ProxyConfig, pconfig));

                        _lock.ExitWriteLock();
                    }
                }

                // Couldn't find the token.
                // It may not have arrived yet, so wait for one to arrive.

                _lock.ExitWriteLock();

                time = Stopwatch.GetTimestamp();
                int remainingMs = (int)((endTime - time) / (Stopwatch.Frequency / 1000));

                if (remainingMs < 0)
                {
                    remainingMs = 0;
                }

                _tokenEvent.WaitOne(remainingMs);

                _lock.EnterWriteLock();

            } while (time < endTime);

            _lock.ExitWriteLock();
        }

        public void DisconnectProxyClient(P2pProxySession session)
        {
            _lock.EnterWriteLock();

            bool removed = _players.Remove(session);

            if (removed)
            {
                Master.SendAsync(RyuLdnProtocol.Encode(PacketId.ExternalProxyState, new ExternalProxyConnectionState
                {
                    IpAddress = session.VirtualIpAddress,
                    Connected = false
                }));
            }

            _lock.ExitWriteLock();
        }
    }
}