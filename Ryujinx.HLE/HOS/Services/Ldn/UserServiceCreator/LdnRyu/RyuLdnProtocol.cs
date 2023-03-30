using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu
{
    class RyuLdnProtocol
    {
        private const byte CurrentProtocolVersion = 2;
        private const uint Magic                  = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24);
        private const int  MaxPacketSize          = 131072;

        private readonly int _headerSize = Marshal.SizeOf<LdnHeader>();

        private readonly byte[] _buffer    = new byte[MaxPacketSize];
        private int    _bufferEnd;

        // Client Packets.
        public event Action<IPEndPoint, LdnHeader, InitializeMessage> Initialize;
        public event Action<LdnHeader, PassphraseMessage> Passphrase;
        public event Action<LdnHeader, NetworkInfo> Connected;
        public event Action<LdnHeader, NetworkInfo> SyncNetwork;
        public event Action<LdnHeader, NetworkInfo> ScanReply;
        public event Action<LdnHeader> ScanReplyEnd;
        public event Action<LdnHeader, DisconnectMessage> Disconnected;

        // External Proxy Packets.
        public event Action<IPEndPoint, LdnHeader, ExternalProxyConfig> ExternalProxy;
        public event Action<LdnHeader, ExternalProxyConnectionState> ExternalProxyState;
        public event Action<LdnHeader, ExternalProxyToken> ExternalProxyToken;

        // Server Packets.
        public event Action<LdnHeader, CreateAccessPointRequest, byte[]> CreateAccessPoint;
        public event Action<LdnHeader, CreateAccessPointPrivateRequest, byte[]> CreateAccessPointPrivate;
        public event Action<LdnHeader, RejectRequest> Reject;
        public event Action<LdnHeader> RejectReply;
        public event Action<LdnHeader, SetAcceptPolicyRequest> SetAcceptPolicy;
        public event Action<LdnHeader, byte[]> SetAdvertiseData;
        public event Action<LdnHeader, ConnectRequest> Connect;
        public event Action<LdnHeader, ConnectPrivateRequest> ConnectPrivate;
        public event Action<LdnHeader, ScanFilter> Scan;

        // Proxy Packets.
        public event Action<IPEndPoint, LdnHeader, ProxyConfig> ProxyConfig;
        public event Action<IPEndPoint, LdnHeader, ProxyConnectRequest> ProxyConnect;
        public event Action<LdnHeader, ProxyConnectResponse> ProxyConnectReply;
        public event Action<LdnHeader, ProxyDataHeader, byte[]> ProxyData;
        public event Action<LdnHeader, ProxyDisconnectMessage> ProxyDisconnect;

        // Lifecycle Packets.
        public event Action<LdnHeader, NetworkErrorMessage> NetworkError;
        public event Action<LdnHeader, PingMessage> Ping;
        public event Action<IPEndPoint, LdnHeader, PingMessage> TestPing;

        public event Action<IPEndPoint, LdnHeader> Any;

        private void Reset()
        {
            _bufferEnd = 0;
        }

        public void Read(IPEndPoint endpoint, byte[] data, int offset, int size)
        {
            int index = 0;

            while (index < size)
            {
                if (_bufferEnd < _headerSize)
                {
                    // Assemble the header first.

                    int copyable = Math.Min(size - index, Math.Min(size, _headerSize - _bufferEnd));

                    Array.Copy(data, index + offset, _buffer, _bufferEnd, copyable);

                    index      += copyable;
                    _bufferEnd += copyable;
                }

                if (_bufferEnd >= _headerSize)
                {
                    // The header is available. Make sure we received all the data (size specified in the header)

                    LdnHeader ldnHeader = MemoryMarshal.Cast<byte, LdnHeader>(_buffer)[0];

                    if (ldnHeader.Magic != Magic)
                    {
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Invalid magic number in received packet: {ldnHeader.Magic}");
                        Reset();

                        return;
                    }

                    if (ldnHeader.Version != CurrentProtocolVersion)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Protocol version mismatch. Expected ${CurrentProtocolVersion}, was ${ldnHeader.Version}.");
                        Reset();

                        return;
                    }

                    int finalSize = _headerSize + ldnHeader.DataSize;

                    if (finalSize >= MaxPacketSize)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Max packet size { MaxPacketSize } exceeded.");
                        Reset();

                        return;
                    }

                    int copyable = Math.Min(size - index, Math.Min(size, finalSize - _bufferEnd));

                    Array.Copy(data, index + offset, _buffer, _bufferEnd, copyable);

                    index      += copyable;
                    _bufferEnd += copyable;

                    if (finalSize == _bufferEnd)
                    {
                        // The full packet has been retrieved. Send it to be decoded.

                        byte[] ldnData = new byte[ldnHeader.DataSize];

                        Array.Copy(_buffer, _headerSize, ldnData, 0, ldnData.Length);

                        DecodeAndHandle(endpoint, ldnHeader, ldnData);

                        Reset();
                    }
                }
            }
        }

        private static (T, byte[]) ParseWithData<T>(byte[] data) where T : struct
        {
            T str = default;
            int size = Marshal.SizeOf(str);

            byte[] remainder = new byte[data.Length - size];

            if (remainder.Length > 0)
            {
                Array.Copy(data, size, remainder, 0, remainder.Length);
            }

            return (MemoryMarshal.Read<T>(data), remainder);
        }

        private void DecodeAndHandle(IPEndPoint endpoint, LdnHeader header, byte[] data)
        {
            Any?.Invoke(endpoint, header);

            switch ((PacketId)header.Type)
            {
                // Client Packets.
                case PacketId.Initialize:
                    {
                        Initialize?.Invoke(endpoint, header, MemoryMarshal.Read<InitializeMessage>(data));

                        break;
                    }
                case PacketId.Passphrase:
                    {
                        Passphrase?.Invoke(header, MemoryMarshal.Read<PassphraseMessage>(data));

                        break;
                    }
                case PacketId.Connected:
                    {
                        Connected?.Invoke(header, MemoryMarshal.Read<NetworkInfo>(data));

                        break;
                    }
                case PacketId.SyncNetwork:
                    {
                        SyncNetwork?.Invoke(header, MemoryMarshal.Read<NetworkInfo>(data));

                        break;
                    }
                case PacketId.ScanReply:
                    {
                        ScanReply?.Invoke(header, MemoryMarshal.Read<NetworkInfo>(data));

                        break;
                    }

                case PacketId.ScanReplyEnd:
                    {
                        ScanReplyEnd?.Invoke(header);

                        break;
                    }
                case PacketId.Disconnect:
                    {
                        Disconnected?.Invoke(header, MemoryMarshal.Read<DisconnectMessage>(data));

                        break;
                    }

                // External Proxy Packets.
                case PacketId.ExternalProxy:
                    {
                        ExternalProxy?.Invoke(endpoint, header, MemoryMarshal.Read<ExternalProxyConfig>(data));

                        break;
                    }
                case PacketId.ExternalProxyState:
                    {
                        ExternalProxyState?.Invoke(header, MemoryMarshal.Read<ExternalProxyConnectionState>(data));

                        break;
                    }
                case PacketId.ExternalProxyToken:
                    {
                        ExternalProxyToken?.Invoke(header, MemoryMarshal.Read<ExternalProxyToken>(data));

                        break;
                    }

                // Server Packets.
                case PacketId.CreateAccessPoint:
                    {
                        (CreateAccessPointRequest packet, byte[] extraData) = ParseWithData<CreateAccessPointRequest>(data);
                        CreateAccessPoint?.Invoke(header, packet, extraData);
                        break;
                    }
                case PacketId.CreateAccessPointPrivate:
                    {
                        (CreateAccessPointPrivateRequest packet, byte[] extraData) = ParseWithData<CreateAccessPointPrivateRequest>(data);
                        CreateAccessPointPrivate?.Invoke(header, packet, extraData);
                        break;
                    }
                case PacketId.Reject:
                    {
                        Reject?.Invoke(header, MemoryMarshal.Read<RejectRequest>(data));

                        break;
                    }
                case PacketId.RejectReply:
                    {
                        RejectReply?.Invoke(header);

                        break;
                    }
                case PacketId.SetAcceptPolicy:
                    {
                        SetAcceptPolicy?.Invoke(header, MemoryMarshal.Read<SetAcceptPolicyRequest>(data));

                        break;
                    }
                case PacketId.SetAdvertiseData:
                    {
                        SetAdvertiseData?.Invoke(header, data);

                        break;
                    }
                case PacketId.Connect:
                    {
                        Connect?.Invoke(header, MemoryMarshal.Read<ConnectRequest>(data));

                        break;
                    }
                case PacketId.ConnectPrivate:
                    {
                        ConnectPrivate?.Invoke(header, MemoryMarshal.Read<ConnectPrivateRequest>(data));

                        break;
                    }
                case PacketId.Scan:
                    {
                        Scan?.Invoke(header, MemoryMarshal.Read<ScanFilter>(data));

                        break;
                    }

                // Proxy Packets
                case PacketId.ProxyConfig:
                    {
                        ProxyConfig?.Invoke(endpoint, header, MemoryMarshal.Read<ProxyConfig>(data));

                        break;
                    }
                case PacketId.ProxyConnect:
                    {
                        ProxyConnect?.Invoke(endpoint, header, MemoryMarshal.Read<ProxyConnectRequest>(data));

                        break;
                    }
                case PacketId.ProxyConnectReply:
                    {
                        ProxyConnectReply?.Invoke(header, MemoryMarshal.Read<ProxyConnectResponse>(data));

                        break;
                    }
                case PacketId.ProxyData:
                    {
                        (ProxyDataHeader packet, byte[] extraData) = ParseWithData<ProxyDataHeader>(data);

                        ProxyData?.Invoke(header, packet, extraData);

                        break;
                    }
                case PacketId.ProxyDisconnect:
                    {
                        ProxyDisconnect?.Invoke(header, MemoryMarshal.Read<ProxyDisconnectMessage>(data));

                        break;
                    }

                // Lifecycle Packets.
                case PacketId.Ping:
                    {
                        Ping?.Invoke(header, MemoryMarshal.Read<PingMessage>(data));

                        break;
                    }
                case PacketId.TestPing:
                    {
                        TestPing?.Invoke(endpoint, header, MemoryMarshal.Read<PingMessage>(data));

                        break;
                    }
                case PacketId.NetworkError:
                    {
                        NetworkError?.Invoke(header, MemoryMarshal.Read<NetworkErrorMessage>(data));

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(header.Type));
            }
        }

        private static LdnHeader GetHeader(PacketId type, int dataSize)
        {
            return new LdnHeader()
            {
                Magic    = Magic,
                Version  = CurrentProtocolVersion,
                Type     = (byte)type,
                DataSize = dataSize
            };
        }

        public static byte[] Encode(PacketId type)
        {
            LdnHeader header = GetHeader(type, 0);

            return SpanHelpers.AsSpan<LdnHeader, byte>(ref header).ToArray();
        }

        public static byte[] Encode(PacketId type, byte[] data)
        {
            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Encoding packet of type: {type}");

            LdnHeader header = GetHeader(type, data.Length);

            byte[] result = SpanHelpers.AsSpan<LdnHeader, byte>(ref header).ToArray();

            Array.Resize(ref result, result.Length + data.Length);
            Array.Copy(data, 0, result, Marshal.SizeOf<LdnHeader>(), data.Length);

            return result;
        }

        public static byte[] Encode<T>(PacketId type, T packet) where T : struct
        {
            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Encoding packet of type: {type}");

            byte[] packetData = new byte[Marshal.SizeOf<T>()];
            MemoryMarshal.Write(packetData, ref packet);

            LdnHeader header = GetHeader(type, packetData.Length);

            byte[] result = SpanHelpers.AsSpan<LdnHeader, byte>(ref header).ToArray();

            Array.Resize(ref result, result.Length + packetData.Length);
            Array.Copy(packetData, 0, result, Marshal.SizeOf<LdnHeader>(), packetData.Length);

            return result;
        }

        public static byte[] Encode<T>(PacketId type, T packet, byte[] data) where T : struct
        {
            Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Encoding packet of type: {type}");

            byte[] packetData = new byte[Marshal.SizeOf<T>()];
            MemoryMarshal.Write(packetData, ref packet);

            LdnHeader header = GetHeader(type, packetData.Length + data.Length);

            byte[] result = SpanHelpers.AsSpan<LdnHeader, byte>(ref header).ToArray();

            Array.Resize(ref result, result.Length + packetData.Length + data.Length);
            Array.Copy(packetData, 0, result, Marshal.SizeOf<LdnHeader>(), packetData.Length);
            Array.Copy(data, 0, result, Marshal.SizeOf<LdnHeader>() + packetData.Length, data.Length);

            return result;
        }
    }
}