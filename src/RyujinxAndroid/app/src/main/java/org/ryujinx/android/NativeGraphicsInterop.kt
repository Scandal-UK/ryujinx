package org.ryujinx.android

import android.view.Surface

class NativeGraphicsInterop {
    var VkCreateSurface: Long = 0
    var SurfaceHandle: Long = 0
    var VkRequiredExtensions: Array<String>? = null
}