using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0xF)]
    struct LdnHeader
    {
        public uint Magic;
        public uint Sequence;
        public bool NeedsAck;
        public byte Type;
        public byte Version;
        public int  DataSize;
    }
}
