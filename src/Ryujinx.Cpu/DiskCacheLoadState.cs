using System;

namespace Ryujinx.Cpu
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