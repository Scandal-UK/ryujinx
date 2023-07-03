using System;

namespace Ryujinx.Cpu.Jit
{
    class DiskCacheLoadState : IDiskCacheLoadState
    {
        /// <inheritdoc/>
        public event Action<LoadState, int, int> StateChanged;

        /// <inheritdoc/>
        public void Cancel()
        {
        }
    }
}