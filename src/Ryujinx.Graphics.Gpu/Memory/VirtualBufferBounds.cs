namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Virtual memory range used for buffers.
    /// </summary>
    readonly struct VirtualBufferBounds
    {
        /// <summary>
        /// Base virtual address.
        /// </summary>
        public ulong GpuVa { get; }

        /// <summary>
        /// Binding size.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Creates a new virtual buffer region.
        /// </summary>
        /// <param name="gpuVa">Base virtual address</param>
        /// <param name="size">Binding size</param>
        public VirtualBufferBounds(ulong gpuVa, ulong size)
        {
            GpuVa = gpuVa;
            Size = size;
        }

        public bool Equals(ulong gpuVa, ulong size)
        {
            return GpuVa == gpuVa && Size == size;
        }
    }
}
