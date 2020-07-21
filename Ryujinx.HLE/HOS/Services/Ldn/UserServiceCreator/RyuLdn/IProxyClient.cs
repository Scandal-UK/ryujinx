namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn
{
    interface IProxyClient
    {
        bool SendAsync(byte[] buffer);
    }
}