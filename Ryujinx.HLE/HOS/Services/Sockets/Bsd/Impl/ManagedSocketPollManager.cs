using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Impl
{
    class ManagedSocketPollManager : IPollManager
    {
        private static ManagedSocketPollManager _instance;

        public static ManagedSocketPollManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ManagedSocketPollManager();
                }

                return _instance;
            }
        }

        public bool IsCompatible(PollEvent evnt)
        {
            return evnt.FileDescriptor is ManagedSocket;
        }

        public LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount)
        {
            List<Socket> readEvents = new List<Socket>();
            List<Socket> writeEvents = new List<Socket>();
            List<Socket> errorEvents = new List<Socket>();

            updatedCount = 0;

            foreach (PollEvent evnt in events)
            {
                if ((evnt.FileDescriptor is ManagedSocket ms) && (ms.Socket is DefaultSocket ds))
                {
                    bool isValidEvent = evnt.Data.InputEvents == 0;

                    errorEvents.Add(ds.BaseSocket);

                    if ((evnt.Data.InputEvents & PollEventTypeMask.Input) != 0)
                    {
                        readEvents.Add(ds.BaseSocket);

                        isValidEvent = true;
                    }

                    if ((evnt.Data.InputEvents & PollEventTypeMask.UrgentInput) != 0)
                    {
                        readEvents.Add(ds.BaseSocket);

                        isValidEvent = true;
                    }

                    if ((evnt.Data.InputEvents & PollEventTypeMask.Output) != 0)
                    {
                        writeEvents.Add(ds.BaseSocket);

                        isValidEvent = true;
                    }

                    if (!isValidEvent)
                    {
                        Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported Poll input event type: {evnt.Data.InputEvents}");
                        return LinuxError.EINVAL;
                    }
                }
            }

            try
            {
                int actualTimeoutMicroseconds = timeoutMilliseconds == -1 ? -1 : timeoutMilliseconds * 1000;

                Socket.Select(readEvents, writeEvents, errorEvents, actualTimeoutMicroseconds);
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            foreach (PollEvent evnt in events)
            {
                if ((evnt.FileDescriptor is ManagedSocket ms) && (ms.Socket is DefaultSocket ds))
                {
                    Socket socket = ds.BaseSocket;

                    PollEventTypeMask outputEvents = evnt.Data.OutputEvents & ~evnt.Data.InputEvents;

                    if (errorEvents.Contains(socket))
                    {
                        outputEvents |= PollEventTypeMask.Error;

                        if (!socket.Connected || !socket.IsBound)
                        {
                            outputEvents |= PollEventTypeMask.Disconnected;
                        }
                    }

                    if (readEvents.Contains(socket))
                    {
                        if ((evnt.Data.InputEvents & PollEventTypeMask.Input) != 0)
                        {
                            outputEvents |= PollEventTypeMask.Input;
                        }
                    }

                    if (writeEvents.Contains(socket))
                    {
                        outputEvents |= PollEventTypeMask.Output;
                    }

                    evnt.Data.OutputEvents = outputEvents;
                }
            }

            updatedCount = readEvents.Count + writeEvents.Count + errorEvents.Count;

            return LinuxError.SUCCESS;
        }

        public LinuxError Select(List<PollEvent> events, int timeout, out int updatedCount)
        {
            List<Socket> readEvents = new();
            List<Socket> writeEvents = new();
            List<Socket> errorEvents = new();

            updatedCount = 0;

            foreach (PollEvent pollEvent in events)
            {
                if ((pollEvent.FileDescriptor is ManagedSocket ms) && (ms.Socket is DefaultSocket ds))
                {
                    if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Input))
                    {
                        readEvents.Add(ds.BaseSocket);
                    }

                    if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                    {
                        writeEvents.Add(ds.BaseSocket);
                    }

                    if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Error))
                    {
                        errorEvents.Add(ds.BaseSocket);
                    }
                }
            }

            Socket.Select(readEvents, writeEvents, errorEvents, timeout);

            updatedCount = readEvents.Count + writeEvents.Count + errorEvents.Count;

            foreach (PollEvent pollEvent in events)
            {
                if ((pollEvent.FileDescriptor is ManagedSocket ms) && (ms.Socket is DefaultSocket ds))
                {
                    if (readEvents.Contains(ds.BaseSocket))
                    {
                        pollEvent.Data.OutputEvents |= PollEventTypeMask.Input;
                    }

                    if (writeEvents.Contains(ds.BaseSocket))
                    {
                        pollEvent.Data.OutputEvents |= PollEventTypeMask.Output;
                    }

                    if (errorEvents.Contains(ds.BaseSocket))
                    {
                        pollEvent.Data.OutputEvents |= PollEventTypeMask.Error;
                    }
                }
            }

            return LinuxError.SUCCESS;
        }
    }
}