package org.ryujinx.android

import android.view.Surface

class NativeHelpers {

    companion object {
        val instance = NativeHelpers()
        init {
            System.loadLibrary("ryujinxjni")
        }
    }

    external fun releaseNativeWindow(window: Long)
    external fun getCreateSurfacePtr(): Long
    external fun getNativeWindow(surface: Surface): Long
    external fun attachCurrentThread()
    external fun detachCurrentThread()

    external fun loadDriver(
        nativeLibPath: String,
        privateAppsPath: String,
        driverName: String
    ): Long

    external fun setTurboMode(enable: Boolean)
    external fun getMaxSwapInterval(nativeWindow: Long): Int
    external fun getMinSwapInterval(nativeWindow: Long): Int
    external fun setSwapInterval(nativeWindow: Long, swapInterval: Int): Int
    external fun getProgressInfo() : String
    external fun getProgressValue() : Float
    external fun storeStringJava(string: String) : Long
    external fun getStringJava(id: Long) : String
}
