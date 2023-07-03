using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadTable
    {
        private const int MaxThreads = 4096;

        private struct Entry
        {
            public IntPtr ThreadId;
            public IntPtr NativeContextPtr;

            public Entry(IntPtr threadId, IntPtr nativeContextPtr)
            {
                ThreadId = threadId;
                NativeContextPtr = nativeContextPtr;
            }
        }

        private static MemoryBlock _block;

        public static IntPtr EntriesPointer => _block.Pointer + 8;

        static NceThreadTable()
        {
            _block = new MemoryBlock((ulong)Unsafe.SizeOf<Entry>() * MaxThreads + 8UL);
            _block.Write(0UL, 0UL);
        }

        public static int Register(IntPtr threadId, IntPtr nativeContextPtr)
        {
            Span<Entry> entries = GetStorage();

            lock (_block)
            {
                ref ulong currentThreadCount = ref GetThreadsCount();

                for (int i = 0; i < MaxThreads; i++)
                {
                    if (entries[i].ThreadId == IntPtr.Zero)
                    {
                        entries[i] = new Entry(threadId, nativeContextPtr);

                        if (currentThreadCount < (ulong)i + 1)
                        {
                            currentThreadCount = (ulong)i + 1;
                        }

                        return i;
                    }
                }
            }

            throw new Exception($"Number of active threads exceeds limit of {MaxThreads}.");
        }

        public static void Unregister(int tableIndex)
        {
            Span<Entry> entries = GetStorage();

            lock (_block)
            {
                if (entries[tableIndex].ThreadId != IntPtr.Zero)
                {
                    entries[tableIndex] = default;

                    ulong currentThreadCount = GetThreadsCount();

                    for (int i = (int)currentThreadCount - 1; i >= 0; i--)
                    {
                        if (entries[i].ThreadId != IntPtr.Zero)
                        {
                            break;
                        }

                        currentThreadCount = (ulong)i;
                    }

                    GetThreadsCount() = currentThreadCount;
                }
            }
        }

        private static ref ulong GetThreadsCount()
        {
            return ref _block.GetRef<ulong>(0UL);
        }

        private static unsafe Span<Entry> GetStorage()
        {
            return new Span<Entry>((void*)_block.GetPointer(8UL, (ulong)Unsafe.SizeOf<Entry>() * MaxThreads), MaxThreads);
        }
    }
}