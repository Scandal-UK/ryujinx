using ARMeilleure.Memory;
using Ryujinx.Memory;
using Ryujinx.Memory.Range;
using Ryujinx.Memory.Tracking;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ryujinx.Cpu.Nce
{
    /// <summary>
    /// Represents a CPU memory manager which maps guest virtual memory directly onto a host virtual region.
    /// </summary>
    public sealed class MemoryManagerNative : VirtualMemoryManagerRefCountedBase, ICpuMemoryManager, IVirtualMemoryManagerTracked, IWritableBlock
    {
        private readonly InvalidAccessHandler _invalidAccessHandler;

        private readonly MemoryBlock _addressSpace;
        private readonly MemoryBlock _addressSpaceMirror;

        private readonly MemoryBlock _backingMemory;
        private readonly PageTable<ulong> _pageTable;

        private readonly MemoryEhMeilleure _memoryEh;

        private readonly ManagedPageFlags _pages;

        /// <inheritdoc/>
        public bool UsesPrivateAllocations => false;

        public int AddressSpaceBits { get; }

        public IntPtr PageTablePointer => IntPtr.Zero;

        public ulong ReservedSize => (ulong)_addressSpace.Pointer.ToInt64();

        public MemoryManagerType Type => MemoryManagerType.HostMappedUnsafe;

        public MemoryTracking Tracking { get; }

        public event Action<ulong, ulong> UnmapEvent;

        protected override ulong AddressSpaceSize { get; }

        /// <summary>
        /// Creates a new instance of the host mapped memory manager.
        /// </summary>
        /// <param name="addressSpace">Address space instance to use</param>
        /// <param name="backingMemory">Physical backing memory where virtual memory will be mapped to</param>
        /// <param name="addressSpaceSize">Size of the address space</param>
        /// <param name="invalidAccessHandler">Optional function to handle invalid memory accesses</param>
        public MemoryManagerNative(
            AddressSpace addressSpace,
            MemoryBlock backingMemory,
            ulong addressSpaceSize,
            InvalidAccessHandler invalidAccessHandler = null)
        {
            _backingMemory = backingMemory;
            _pageTable = new PageTable<ulong>();
            _invalidAccessHandler = invalidAccessHandler;

            ulong asSize = PageSize;
            int asBits = PageBits;

            while (asSize < addressSpaceSize)
            {
                asSize <<= 1;
                asBits++;
            }

            AddressSpaceBits = asBits;
            AddressSpaceSize = addressSpace.AddressSpaceSize;

            _pages = new ManagedPageFlags(AddressSpaceBits);

            _addressSpace = addressSpace.Base;
            _addressSpaceMirror = addressSpace.Mirror;

            Tracking = new MemoryTracking(this, PageSize, invalidAccessHandler);
            _memoryEh = new MemoryEhMeilleure(asSize, Tracking);
        }

        /// <summary>
        /// Ensures the combination of virtual address and size is part of the addressable space and fully mapped.
        /// </summary>
        /// <param name="va">Virtual address of the range</param>
        /// <param name="size">Size of the range in bytes</param>
        private void AssertMapped(ulong va, ulong size)
        {
            if (!ValidateAddressAndSize(va, size) || !_pages.IsRangeMapped(va, size))
            {
                throw new InvalidMemoryRegionException($"Not mapped: va=0x{va:X16}, size=0x{size:X16}");
            }
        }

        /// <inheritdoc/>
        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            AssertValidAddressAndSize(va, size);

            _addressSpace.MapView(_backingMemory, pa, AddressToOffset(va), size);
            _addressSpaceMirror.MapView(_backingMemory, pa, AddressToOffset(va), size);
            _pages.AddMapping(va, size);
            PtMap(va, pa, size);

            Tracking.Map(va, size);
        }

        /// <inheritdoc/>
        public void Unmap(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            UnmapEvent?.Invoke(va, size);
            Tracking.Unmap(va, size);

            _pages.RemoveMapping(va, size);
            PtUnmap(va, size);
            _addressSpace.UnmapView(_backingMemory, AddressToOffset(va), size);
            _addressSpaceMirror.UnmapView(_backingMemory, AddressToOffset(va), size);
        }

        private void PtMap(ulong va, ulong pa, ulong size)
        {
            while (size != 0)
            {
                _pageTable.Map(va, pa);

                va += PageSize;
                pa += PageSize;
                size -= PageSize;
            }
        }

        private void PtUnmap(ulong va, ulong size)
        {
            while (size != 0)
            {
                _pageTable.Unmap(va);

                va += PageSize;
                size -= PageSize;
            }
        }

        /// <inheritdoc/>
        public void Reprotect(ulong va, ulong size, MemoryPermission permission)
        {
            _addressSpace.Reprotect(AddressToOffset(va), size, permission);
        }

        public override T Read<T>(ulong va)
        {
            try
            {
                AssertMapped(va, (ulong)Unsafe.SizeOf<T>());

                return _addressSpaceMirror.Read<T>(AddressToOffset(va));
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }

                return default;
            }
        }

        public override T ReadTracked<T>(ulong va)
        {
            try
            {
                SignalMemoryTracking(va, (ulong)Unsafe.SizeOf<T>(), false);

                return Read<T>(va);
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }

                return default;
            }
        }

        public override void Read(ulong va, Span<byte> data)
        {
            try
            {
                AssertMapped(va, (ulong)data.Length);

                _addressSpaceMirror.Read(AddressToOffset(va), data);
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }
            }
        }


        public override void Write<T>(ulong va, T value)
        {
            try
            {
                SignalMemoryTracking(va, (ulong)Unsafe.SizeOf<T>(), write: true);

                _addressSpaceMirror.Write(AddressToOffset(va), value);
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }
            }
        }

        public override void Write(ulong va, ReadOnlySpan<byte> data)
        {
            try
            {
                SignalMemoryTracking(va, (ulong)data.Length, write: true);

                _addressSpaceMirror.Write(AddressToOffset(va), data);
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }
            }
        }

        public override void WriteUntracked(ulong va, ReadOnlySpan<byte> data)
        {
            try
            {
                AssertMapped(va, (ulong)data.Length);

                _addressSpaceMirror.Write(AddressToOffset(va), data);
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }
            }
        }

        public override bool WriteWithRedundancyCheck(ulong va, ReadOnlySpan<byte> data)
        {
            try
            {
                SignalMemoryTracking(va, (ulong)data.Length, false);

                Span<byte> target = _addressSpaceMirror.GetSpan(AddressToOffset(va), data.Length);
                bool changed = !data.SequenceEqual(target);

                if (changed)
                {
                    data.CopyTo(target);
                }

                return changed;
            }
            catch (InvalidMemoryRegionException)
            {
                if (_invalidAccessHandler == null || !_invalidAccessHandler(va))
                {
                    throw;
                }

                return true;
            }
        }

        public override ReadOnlySequence<byte> GetReadOnlySequence(ulong va, int size, bool tracked = false)
        {
            if (tracked)
            {
                SignalMemoryTracking(va, (ulong)size, write: false);
            }
            else
            {
                AssertMapped(va, (ulong)size);
            }

            return new ReadOnlySequence<byte>(_addressSpaceMirror.GetMemory(va, size));
        }

        public override ReadOnlySpan<byte> GetSpan(ulong va, int size, bool tracked = false)
        {
            if (tracked)
            {
                SignalMemoryTracking(va, (ulong)size, write: false);
            }
            else
            {
                AssertMapped(va, (ulong)size);
            }

            if (size == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            return _addressSpaceMirror.GetSpan(AddressToOffset(va), size);
        }

        public override WritableRegion GetWritableRegion(ulong va, int size, bool tracked = false)
        {
            if (tracked)
            {
                SignalMemoryTracking(va, (ulong)size, true);
            }
            else
            {
                AssertMapped(va, (ulong)size);
            }

            return _addressSpaceMirror.GetWritableRegion(AddressToOffset(va), size);
        }

        /// <inheritdoc/>
        public ref T GetRef<T>(ulong va) where T : unmanaged
        {
            SignalMemoryTracking(va, (ulong)Unsafe.SizeOf<T>(), true);

            return ref _addressSpaceMirror.GetRef<T>(AddressToOffset(va));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsMapped(ulong va)
        {
            return ValidateAddress(va) && _pages.IsMapped(va);
        }

        /// <inheritdoc/>
        public bool IsRangeMapped(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            return _pages.IsRangeMapped(va, size);
        }

        /// <inheritdoc/>
        public IEnumerable<HostMemoryRange> GetHostRegions(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            return Enumerable.Repeat(new HostMemoryRange((nuint)(ulong)_addressSpaceMirror.GetPointer(AddressToOffset(va), size), size), 1);
        }

        /// <inheritdoc/>
        public IEnumerable<MemoryRange> GetPhysicalRegions(ulong va, ulong size)
        {
            int pages = GetPagesCount(va, (uint)size, out va);

            var regions = new List<MemoryRange>();

            ulong regionStart = GetPhysicalAddressChecked(va);
            ulong regionSize = PageSize;

            for (int page = 0; page < pages - 1; page++)
            {
                if (!ValidateAddress(va + PageSize))
                {
                    return null;
                }

                ulong newPa = GetPhysicalAddressChecked(va + PageSize);

                if (GetPhysicalAddressChecked(va) + PageSize != newPa)
                {
                    regions.Add(new MemoryRange(regionStart, regionSize));
                    regionStart = newPa;
                    regionSize = 0;
                }

                va += PageSize;
                regionSize += PageSize;
            }

            regions.Add(new MemoryRange(regionStart, regionSize));

            return regions;
        }

        private ulong GetPhysicalAddressChecked(ulong va)
        {
            if (!IsMapped(va))
            {
                ThrowInvalidMemoryRegionException($"Not mapped: va=0x{va:X16}");
            }

            return GetPhysicalAddressInternal(va);
        }

        private ulong GetPhysicalAddressInternal(ulong va)
        {
            return _pageTable.Read(va) + (va & PageMask);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This function also validates that the given range is both valid and mapped, and will throw if it is not.
        /// </remarks>
        public override void SignalMemoryTracking(ulong va, ulong size, bool write, bool precise = false, int? exemptId = null)
        {
            AssertValidAddressAndSize(va, size);

            if (precise)
            {
                Tracking.VirtualMemoryEvent(va, size, write, precise: true, exemptId);
                return;
            }

            _pages.SignalMemoryTracking(Tracking, va, size, write, exemptId);
        }

        /// <inheritdoc/>
        public void TrackingReprotect(ulong va, ulong size, MemoryPermission protection, bool guest)
        {
            if (guest)
            {
                _addressSpace.Reprotect(AddressToOffset(va), size, protection, false);
            }
            else
            {
                _pages.TrackingReprotect(va, size, protection);
            }
        }

        /// <inheritdoc/>
        public RegionHandle BeginTracking(ulong address, ulong size, int id, RegionFlags flags)
        {
            return Tracking.BeginTracking(address, size, id, flags);
        }

        /// <inheritdoc/>
        public MultiRegionHandle BeginGranularTracking(ulong address, ulong size, IEnumerable<IRegionHandle> handles, ulong granularity, int id, RegionFlags flags)
        {
            return Tracking.BeginGranularTracking(address, size, handles, granularity, id, flags);
        }

        /// <inheritdoc/>
        public SmartMultiRegionHandle BeginSmartGranularTracking(ulong address, ulong size, ulong granularity, int id)
        {
            return Tracking.BeginSmartGranularTracking(address, size, granularity, id);
        }

        private ulong AddressToOffset(ulong address)
        {
            if (address < ReservedSize)
            {
                throw new ArgumentException($"Invalid address 0x{address:x16}");
            }

            return address - ReservedSize;
        }

        /// <summary>
        /// Disposes of resources used by the memory manager.
        /// </summary>
        protected override void Destroy()
        {
            _addressSpace.Dispose();
            _addressSpaceMirror.Dispose();
            _memoryEh.Dispose();
        }

        protected override Memory<byte> GetPhysicalAddressMemory(nuint pa, int size)
            => _addressSpaceMirror.GetMemory(pa, size);

        protected override Span<byte> GetPhysicalAddressSpan(nuint pa, int size)
            => _addressSpaceMirror.GetSpan(pa, size);

        protected override nuint TranslateVirtualAddressChecked(ulong va)
            => (nuint)GetPhysicalAddressChecked(va);

        protected override nuint TranslateVirtualAddressUnchecked(ulong va)
            => (nuint)GetPhysicalAddressInternal(va);
    }
}
