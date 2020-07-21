using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    struct ProxyConnectResponse
    {
        public ProxyInfo Info;
    }
}