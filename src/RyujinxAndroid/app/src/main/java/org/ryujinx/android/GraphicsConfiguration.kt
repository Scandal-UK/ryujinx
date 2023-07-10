package org.ryujinx.android

import android.R.bool

class GraphicsConfiguration {
    var ResScale = 1f
    var MaxAnisotropy = -1f
    var FastGpuTime: Boolean = true
    var Fast2DCopy: Boolean = true
    var EnableMacroJit: Boolean = false
    var EnableMacroHLE: Boolean = true
    var EnableShaderCache: Boolean = true
    var EnableTextureRecompression: Boolean = false
    var BackendThreading: Int = org.ryujinx.android.BackendThreading.Auto.ordinal
}

enum class BackendThreading
{
    Auto,
    Off,
    On
}