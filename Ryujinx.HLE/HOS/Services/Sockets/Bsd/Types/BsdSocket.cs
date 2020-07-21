using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class BsdSocket
    {
        public int Family;
        public int Type;
        public int Protocol;

        public ISocket Handle;
    }
}