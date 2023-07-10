package org.ryujinx.android

import android.view.Surface

class NativeHelpers {

    companion object {
        init {
            System.loadLibrary("ryujinxjni")
        }
    }
    external fun releaseNativeWindow(window:Long) : Unit
    external fun createSurface(vkInstance:Long, window:Long) : Long
    external fun getCreateSurfacePtr() : Long
    external fun getNativeWindow(surface:Surface) : Long
    external fun attachCurrentThread() : Unit
    external fun detachCurrentThread() : Unit
}