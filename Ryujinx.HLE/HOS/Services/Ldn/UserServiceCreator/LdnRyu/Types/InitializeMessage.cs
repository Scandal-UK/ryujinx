using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    /// <summary>
    /// This message is first sent by the client to identify themselves.
    /// If the server has a token+mac combo that matches the submission, then they are returned their new ID and mac address. (the mac is also reassigned to the new id)
    /// Otherwise, they are returned a random mac address.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x16)]
    struct InitializeMessage
    {
        // All 0 if we don't have an ID yet.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] Id;

        // All 0 if we don't have a mac yet.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x6)]
        public byte[] MacAddress;
    }
}
