package org.ryujinx.android

import org.ryujinx.android.viewmodels.GameInfo

@Suppress("KotlinJniMissingFunction")
class RyujinxNative {
    external fun initialize(appPath: Long): Boolean

    companion object {
        val instance: RyujinxNative = RyujinxNative()

        init {
            System.loadLibrary("ryujinx")
        }
    }

    external fun deviceInitialize(
        isHostMapped: Boolean, useNce: Boolean,
        systemLanguage: Int,
        regionCode: Int,
        enableVsync: Boolean,
        enableDockedMode: Boolean,
        enablePtc: Boolean,
        enableInternetAccess: Boolean,
        timeZone: Long,
        ignoreMissingServices: Boolean
    ): Boolean

    external fun graphicsInitialize(configuration: GraphicsConfiguration): Boolean
    external fun graphicsInitializeRenderer(
        extensions: Array<String>,
        driver: Long
    ): Boolean

    external fun deviceLoad(game: String): Boolean
    external fun deviceLaunchMiiEditor(): Boolean
    external fun deviceGetGameFrameRate(): Double
    external fun deviceGetGameFrameTime(): Double
    external fun deviceGetGameFifo(): Double
    external fun deviceGetGameInfo(fileDescriptor: Int, extension: Long): GameInfo
    external fun deviceGetGameInfoFromPath(path: String): GameInfo
    external fun deviceLoadDescriptor(fileDescriptor: Int, gameType: Int, updateDescriptor: Int): Boolean
    external fun graphicsRendererSetSize(width: Int, height: Int)
    external fun graphicsRendererSetVsync(enabled: Boolean)
    external fun graphicsRendererRunLoop()
    external fun deviceReloadFilesystem()
    external fun inputInitialize(width: Int, height: Int)
    external fun inputSetClientSize(width: Int, height: Int)
    external fun inputSetTouchPoint(x: Int, y: Int)
    external fun inputReleaseTouchPoint()
    external fun inputUpdate()
    external fun inputSetButtonPressed(button: Int, id: Int)
    external fun inputSetButtonReleased(button: Int, id: Int)
    external fun inputConnectGamepad(index: Int): Int
    external fun inputSetStickAxis(stick: Int, x: Float, y: Float, id: Int)
    external fun inputSetAccelerometerData(x: Float, y: Float, z: Float, id: Int)
    external fun inputSetGyroData(x: Float, y: Float, z: Float, id: Int)
    external fun graphicsSetSurface(surface: Long, window: Long)
    external fun deviceCloseEmulation()
    external fun deviceSignalEmulationClose()
    external fun deviceGetDlcTitleId(path: Long, ncaPath: Long): Long
    external fun deviceGetDlcContentList(path: Long, titleId: Long): Array<String>
    external fun userGetOpenedUser(): Long
    external fun userGetUserPicture(userId: Long): Long
    external fun userSetUserPicture(userId: String, picture: String)
    external fun userGetUserName(userId: Long): Long
    external fun userSetUserName(userId: String, userName: String)
    external fun userGetAllUsers(): Array<String>
    external fun userAddUser(username: String, picture: String)
    external fun userDeleteUser(userId: String)
    external fun userOpenUser(userId: Long)
    external fun userCloseUser(userId: String)
    external fun loggingSetEnabled(logLevel: Int, enabled: Boolean)
    external fun deviceVerifyFirmware(fileDescriptor: Int, isXci: Boolean): Long
    external fun deviceInstallFirmware(fileDescriptor: Int, isXci: Boolean)
    external fun deviceGetInstalledFirmwareVersion() : Long
    external fun uiHandlerSetup()
    external fun uiHandlerWait()
    external fun uiHandlerStopWait()
    external fun uiHandlerSetResponse(isOkPressed: Boolean, input: Long)
}
