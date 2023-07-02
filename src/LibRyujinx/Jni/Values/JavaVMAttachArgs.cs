using LibRyujinx.Jni.Pointers;
using LibRyujinx.Jni.References;
using System;

namespace LibRyujinx.Jni.Values
{
    public readonly struct JavaVMAttachArgs
    {
        internal Int32 Version { get; init; }
        internal CCharSequence Name { get; init; }
        internal JObjectLocalRef Group { get; init; }
    }
}
