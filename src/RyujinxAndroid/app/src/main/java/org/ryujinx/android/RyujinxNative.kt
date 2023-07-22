package org.ryujinx.android

import org.ryujinx.android.viewmodels.GameInfo

@Suppress("KotlinJniMissingFunction")
class RyujinxNative {

    external fun initialize(appPath: String, enableDebugLogs : Boolean): Boolean

    companion object {
        init {
            System.loadLibrary("ryujinx")
        }
    }

    external fun deviceInitialize(isHostMapped: Boolean, useNce: Boolean,
                                  systemLanguage : Int,
                                  regionCode : Int,
                                  enableVsync : Boolean,
                                  enableDockedMode : Boolean,
                                  enablePtc : Boolean,
                                  enableInternetAccess : Boolean,
                                  timeZone : String,
                                  ignoreMissingServices : Boolean): Boolean
    external fun graphicsInitialize(configuration: GraphicsConfiguration): Boolean
    external fun graphicsInitializeRenderer(
        extensions: Array<String>,
        surface: Long
    ): Boolean

    external fun deviceLoad(game: String): Boolean
    external fun deviceGetGameFrameRate(): Double
    external fun deviceGetGameFrameTime(): Double
    external fun deviceGetGameFifo(): Double
    external fun deviceGetGameInfo(fileDescriptor: Int,  isXci:Boolean): GameInfo
    external fun deviceGetGameInfoFromPath(path: String): GameInfo
    external fun deviceLoadDescriptor(fileDescriptor: Int,  isXci:Boolean): Boolean
    external fun graphicsRendererSetSize(width: Int, height: Int): Unit
    external fun graphicsRendererSetVsync(enabled: Boolean): Unit
    external fun graphicsRendererRunLoop(): Unit
    external fun inputInitialize(width: Int, height: Int): Unit
    external fun inputSetClientSize(width: Int, height: Int): Unit
    external fun inputSetTouchPoint(x: Int, y: Int): Unit
    external fun inputReleaseTouchPoint(): Unit
    external fun inputUpdate(): Unit
    external fun inputSetButtonPressed(button: Int, id: Int): Unit
    external fun inputSetButtonReleased(button: Int, id: Int): Unit
    external fun inputConnectGamepad(index: Int): Int
    external fun inputSetStickAxis(stick: Int, x: Float, y: Float, id: Int): Unit
    external fun graphicsSetSurface(surface: Long): String
}