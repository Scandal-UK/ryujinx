using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x2)]
    struct PingMessage
    {
        public byte Requester;
        public byte Id;
    }
}