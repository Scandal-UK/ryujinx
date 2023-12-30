package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.compose.runtime.MutableState
import androidx.documentfile.provider.DocumentFile
import androidx.navigation.NavHostController
import androidx.preference.PreferenceManager
import com.anggrayudi.storage.callback.FileCallback
import com.anggrayudi.storage.file.FileFullPath
import com.anggrayudi.storage.file.copyFileTo
import com.anggrayudi.storage.file.extension
import com.anggrayudi.storage.file.getAbsolutePath
import org.ryujinx.android.LogLevel
import org.ryujinx.android.MainActivity
import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.RyujinxNative
import java.io.File
import kotlin.concurrent.thread

class SettingsViewModel(var navController: NavHostController, val activity: MainActivity) {
    var selectedFirmwareVersion: String = ""
    private var previousFileCallback: ((requestCode: Int, files: List<DocumentFile>) -> Unit)?
    private var previousFolderCallback: ((requestCode: Int, folder: DocumentFile) -> Unit)?
    private var sharedPref: SharedPreferences
    var selectedFirmwareFile: DocumentFile? = null

    init {
        sharedPref = getPreferences()
        previousFolderCallback = activity.storageHelper!!.onFolderSelected
        previousFileCallback = activity.storageHelper!!.onFileSelected
        activity.storageHelper!!.onFolderSelected = { requestCode, folder ->
            run {
                val p = folder.getAbsolutePath(activity)
                val editor = sharedPref.edit()
                editor?.putString("gameFolder", p)
                editor?.apply()
            }
        }
    }

    private fun getPreferences(): SharedPreferences {
        return PreferenceManager.getDefaultSharedPreferences(activity)
    }

    fun initializeState(
        isHostMapped: MutableState<Boolean>,
        useNce: MutableState<Boolean>,
        enableVsync: MutableState<Boolean>,
        enableDocked: MutableState<Boolean>,
        enablePtc: MutableState<Boolean>,
        ignoreMissingServices: MutableState<Boolean>,
        enableShaderCache: MutableState<Boolean>,
        enableTextureRecompression: MutableState<Boolean>,
        resScale: MutableState<Float>,
        useVirtualController: MutableState<Boolean>,
        isGrid: MutableState<Boolean>,
        useSwitchLayout: MutableState<Boolean>,
        enableMotion: MutableState<Boolean>,
        enableDebugLogs: MutableState<Boolean>,
        enableStubLogs: MutableState<Boolean>,
        enableInfoLogs: MutableState<Boolean>,
        enableWarningLogs: MutableState<Boolean>,
        enableErrorLogs: MutableState<Boolean>,
        enableGuestLogs: MutableState<Boolean>,
        enableAccessLogs: MutableState<Boolean>,
        enableTraceLogs: MutableState<Boolean>
    ) {

        isHostMapped.value = sharedPref.getBoolean("isHostMapped", true)
        useNce.value = sharedPref.getBoolean("useNce", true)
        enableVsync.value = sharedPref.getBoolean("enableVsync", true)
        enableDocked.value = sharedPref.getBoolean("enableDocked", true)
        enablePtc.value = sharedPref.getBoolean("enablePtc", true)
        ignoreMissingServices.value = sharedPref.getBoolean("ignoreMissingServices", false)
        enableShaderCache.value = sharedPref.getBoolean("enableShaderCache", true)
        enableTextureRecompression.value =
            sharedPref.getBoolean("enableTextureRecompression", false)
        resScale.value = sharedPref.getFloat("resScale", 1f)
        useVirtualController.value = sharedPref.getBoolean("useVirtualController", true)
        isGrid.value = sharedPref.getBoolean("isGrid", true)
        useSwitchLayout.value = sharedPref.getBoolean("useSwitchLayout", true)
        enableMotion.value = sharedPref.getBoolean("enableMotion", true)

        enableDebugLogs.value = sharedPref.getBoolean("enableDebugLogs", false)
        enableStubLogs.value = sharedPref.getBoolean("enableStubLogs", false)
        enableInfoLogs.value = sharedPref.getBoolean("enableInfoLogs", true)
        enableWarningLogs.value = sharedPref.getBoolean("enableWarningLogs", true)
        enableErrorLogs.value = sharedPref.getBoolean("enableErrorLogs", true)
        enableGuestLogs.value = sharedPref.getBoolean("enableGuestLogs", true)
        enableAccessLogs.value = sharedPref.getBoolean("enableAccessLogs", false)
        enableTraceLogs.value = sharedPref.getBoolean("enableStubLogs", false)
    }

    fun save(
        isHostMapped: MutableState<Boolean>,
        useNce: MutableState<Boolean>,
        enableVsync: MutableState<Boolean>,
        enableDocked: MutableState<Boolean>,
        enablePtc: MutableState<Boolean>,
        ignoreMissingServices: MutableState<Boolean>,
        enableShaderCache: MutableState<Boolean>,
        enableTextureRecompression: MutableState<Boolean>,
        resScale: MutableState<Float>,
        useVirtualController: MutableState<Boolean>,
        isGrid: MutableState<Boolean>,
        useSwitchLayout: MutableState<Boolean>,
        enableMotion: MutableState<Boolean>,
        enableDebugLogs: MutableState<Boolean>,
        enableStubLogs: MutableState<Boolean>,
        enableInfoLogs: MutableState<Boolean>,
        enableWarningLogs: MutableState<Boolean>,
        enableErrorLogs: MutableState<Boolean>,
        enableGuestLogs: MutableState<Boolean>,
        enableAccessLogs: MutableState<Boolean>,
        enableTraceLogs: MutableState<Boolean>
    ) {
        val editor = sharedPref.edit()

        editor.putBoolean("isHostMapped", isHostMapped.value)
        editor.putBoolean("useNce", useNce.value)
        editor.putBoolean("enableVsync", enableVsync.value)
        editor.putBoolean("enableDocked", enableDocked.value)
        editor.putBoolean("enablePtc", enablePtc.value)
        editor.putBoolean("ignoreMissingServices", ignoreMissingServices.value)
        editor.putBoolean("enableShaderCache", enableShaderCache.value)
        editor.putBoolean("enableTextureRecompression", enableTextureRecompression.value)
        editor.putFloat("resScale", resScale.value)
        editor.putBoolean("useVirtualController", useVirtualController.value)
        editor.putBoolean("isGrid", isGrid.value)
        editor.putBoolean("useSwitchLayout", useSwitchLayout.value)
        editor.putBoolean("enableMotion", enableMotion.value)

        editor.putBoolean("enableDebugLogs", enableDebugLogs.value)
        editor.putBoolean("enableStubLogs", enableStubLogs.value)
        editor.putBoolean("enableInfoLogs", enableInfoLogs.value)
        editor.putBoolean("enableWarningLogs", enableWarningLogs.value)
        editor.putBoolean("enableErrorLogs", enableErrorLogs.value)
        editor.putBoolean("enableGuestLogs", enableGuestLogs.value)
        editor.putBoolean("enableAccessLogs", enableAccessLogs.value)
        editor.putBoolean("enableTraceLogs", enableTraceLogs.value)

        editor.apply()
        activity.storageHelper!!.onFolderSelected = previousFolderCallback

        RyujinxNative.instance.loggingSetEnabled(LogLevel.Debug.ordinal, enableDebugLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Info.ordinal, enableInfoLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Stub.ordinal, enableStubLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Warning.ordinal, enableWarningLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Error.ordinal, enableErrorLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.AccessLog.ordinal, enableAccessLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Guest.ordinal, enableGuestLogs.value)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Trace.ordinal, enableTraceLogs.value)
    }

    fun openGameFolder() {
        val path = sharedPref?.getString("gameFolder", "") ?: ""

        if (path.isEmpty())
            activity.storageHelper?.storage?.openFolderPicker()
        else
            activity.storageHelper?.storage?.openFolderPicker(
                activity.storageHelper!!.storage.requestCodeFolderPicker,
                FileFullPath(activity, path)
            )
    }

    fun importProdKeys() {
        activity.storageHelper!!.onFileSelected = { requestCode, files ->
            run {
                activity.storageHelper!!.onFileSelected = previousFileCallback
                val file = files.firstOrNull()
                file?.apply {
                    if (name == "prod.keys") {
                        val outputFile = File(MainActivity.AppPath + "/system");
                        outputFile.delete()

                        thread {
                            file.copyFileTo(
                                activity,
                                outputFile,
                                callback = object : FileCallback() {
                                    override fun onCompleted(result: Any) {
                                        super.onCompleted(result)
                                    }
                                })
                        }
                    }
                }
            }
        }
        activity.storageHelper?.storage?.openFilePicker()
    }

    fun selectFirmware(installState: MutableState<FirmwareInstallState>) {
        if (installState.value != FirmwareInstallState.None)
            return
        activity.storageHelper!!.onFileSelected = { _, files ->
            run {
                activity.storageHelper!!.onFileSelected = previousFileCallback
                val file = files.firstOrNull()
                file?.apply {
                    if (extension == "xci" || extension == "zip") {
                        installState.value = FirmwareInstallState.Verifying
                        thread {
                            val descriptor =
                                activity.contentResolver.openFileDescriptor(file.uri, "rw")
                            descriptor?.use { d ->
                                val version = RyujinxNative.instance.deviceVerifyFirmware(
                                    d.fd,
                                    extension == "xci"
                                )
                                selectedFirmwareFile = file
                                if (version != -1L) {
                                    selectedFirmwareVersion =
                                        NativeHelpers.instance.getStringJava(version)
                                    installState.value = FirmwareInstallState.Query
                                } else {
                                    installState.value = FirmwareInstallState.Cancelled
                                }
                            }
                        }
                    } else {
                        installState.value = FirmwareInstallState.Cancelled
                    }
                }
            }
        }
        activity.storageHelper?.storage?.openFilePicker()
    }

    fun installFirmware(installState: MutableState<FirmwareInstallState>) {
        if (installState.value != FirmwareInstallState.Query)
            return
        if (selectedFirmwareFile == null) {
            installState.value = FirmwareInstallState.None
            return
        }
        selectedFirmwareFile?.apply {
            val descriptor =
                activity.contentResolver.openFileDescriptor(uri, "rw")
            descriptor?.use { d ->
                installState.value = FirmwareInstallState.Install
                thread {
                    try {
                        RyujinxNative.instance.deviceInstallFirmware(
                            d.fd,
                            extension == "xci"
                        )
                    } finally {
                        MainActivity.mainViewModel?.refreshFirmwareVersion()
                        installState.value = FirmwareInstallState.Done
                    }
                }
            }
        }
    }

    fun clearFirmwareSelection(installState: MutableState<FirmwareInstallState>){
        selectedFirmwareFile = null
        selectedFirmwareVersion = ""
        installState.value = FirmwareInstallState.None
    }
}


enum class FirmwareInstallState{
    None,
    Cancelled,
    Verifying,
    Query,
    Install,
    Done
}
