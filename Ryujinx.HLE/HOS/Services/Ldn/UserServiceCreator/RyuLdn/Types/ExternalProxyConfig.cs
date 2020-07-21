using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types
{
    /// <summary>
    /// Sent by the server to point a client towards an external server being used as a proxy.
    /// The client then forwards this to the external proxy after connecting, to verify the connection worked.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x26, Pack = 1)]
    struct ExternalProxyConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] ProxyIp;
        public AddressFamily AddressFamily;

        public ushort ProxyPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] Token;
    }
}