using LibRyujinx.Jni.Pointers;
using System;

namespace LibRyujinx.Jni.Values
{
    public readonly struct JavaVMOption
    {
        internal CCharSequence Name { get; init; }
        internal IntPtr ExtraInfo { get; init; }
    }
}
