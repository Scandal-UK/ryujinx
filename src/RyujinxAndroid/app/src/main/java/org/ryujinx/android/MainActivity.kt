package org.ryujinx.android

import android.annotation.SuppressLint
import android.content.Intent
import android.content.pm.ActivityInfo
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.view.KeyEvent
import android.view.MotionEvent
import android.view.WindowManager
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.anggrayudi.storage.SimpleStorageHelper
import com.halilibo.richtext.ui.RichTextThemeIntegration
import org.ryujinx.android.ui.theme.RyujinxAndroidTheme
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.QuickSettings
import org.ryujinx.android.views.MainView
import kotlin.math.abs


class MainActivity : BaseActivity() {
    private var physicalControllerManager: PhysicalControllerManager =
        PhysicalControllerManager(this)
    private lateinit var motionSensorManager: MotionSensorManager
    private var _isInit: Boolean = false
    var isGameRunning = false
    var storageHelper: SimpleStorageHelper? = null
    lateinit var uiHandler: UiHandler
    companion object {
        var mainViewModel: MainViewModel? = null
        var AppPath : String = ""
        var StorageHelper: SimpleStorageHelper? = null

        @JvmStatic
        fun updateRenderSessionPerformance(gameTime : Long)
        {
            if(gameTime <= 0)
                return

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                mainViewModel?.performanceManager?.updateRenderingSessionTime(gameTime)
            }

            mainViewModel?.gameHost?.hideProgressIndicator()
        }
    }

    init {
        storageHelper = SimpleStorageHelper(this)
        StorageHelper = storageHelper
        System.loadLibrary("ryujinxjni")
        initVm()
    }

    external fun getRenderingThreadId() : Long
    private external fun initVm()

    private fun initialize() {
        if (_isInit)
            return

        val appPath: String = AppPath

        var quickSettings = QuickSettings(this)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Debug.ordinal, quickSettings.enableDebugLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Info.ordinal, quickSettings.enableInfoLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Stub.ordinal, quickSettings.enableStubLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Warning.ordinal, quickSettings.enableWarningLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Error.ordinal, quickSettings.enableErrorLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.AccessLog.ordinal, quickSettings.enableAccessLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Guest.ordinal, quickSettings.enableGuestLogs)
        RyujinxNative.instance.loggingSetEnabled(LogLevel.Trace.ordinal, quickSettings.enableTraceLogs)
        val success = RyujinxNative.instance.initialize(NativeHelpers.instance.storeStringJava(appPath))

        uiHandler = UiHandler()
        _isInit = success
    }
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        motionSensorManager = MotionSensorManager(this)
        Thread.setDefaultUncaughtExceptionHandler(crashHandler)

        if(
            !Environment.isExternalStorageManager()
        ) {
            storageHelper?.storage?.requestFullStorageAccess()
        }

        AppPath = this.getExternalFilesDir(null)!!.absolutePath

        initialize()

        window.attributes.layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
        WindowCompat.setDecorFitsSystemWindows(window,false)

        mainViewModel = MainViewModel(this)
        mainViewModel!!.physicalControllerManager = physicalControllerManager
        mainViewModel!!.motionSensorManager = motionSensorManager

        mainViewModel!!.refreshFirmwareVersion()

        mainViewModel?.apply {
            setContent {
                RyujinxAndroidTheme {
                    RichTextThemeIntegration(contentColor = { MaterialTheme.colorScheme.onSurface }) {
                        // A surface container using the 'background' color from the theme
                        Surface(
                            modifier = Modifier.fillMaxSize(),
                            color = MaterialTheme.colorScheme.background
                        ) {
                            MainView.Main(mainViewModel = this)
                        }
                    }
                }
            }
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        storageHelper?.onSaveInstanceState(outState)
        super.onSaveInstanceState(outState)
    }

    override fun onRestoreInstanceState(savedInstanceState: Bundle) {
        super.onRestoreInstanceState(savedInstanceState)
        storageHelper?.onRestoreInstanceState(savedInstanceState)
    }

    // Game Stuff
    private fun force60HzRefreshRate(enable: Boolean) {
        // Hack for MIUI devices since they don't support the standard Android APIs
        try {
            val setFpsIntent = Intent("com.miui.powerkeeper.SET_ACTIVITY_FPS")
            setFpsIntent.putExtra("package_name", "org.ryujinx.android")
            setFpsIntent.putExtra("isEnter", enable)
            sendBroadcast(setFpsIntent)
        } catch (_: Exception) {
        }

        if (enable)
            display?.supportedModes?.minByOrNull { abs(it.refreshRate - 60f) }
                ?.let { window.attributes.preferredDisplayModeId = it.modeId }
        else
            display?.supportedModes?.maxByOrNull { it.refreshRate }
                ?.let { window.attributes.preferredDisplayModeId = it.modeId }
    }

    fun setFullScreen(fullscreen: Boolean) {
        requestedOrientation =
            if (fullscreen) ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE else ActivityInfo.SCREEN_ORIENTATION_FULL_USER

        val insets = WindowCompat.getInsetsController(window, window.decorView)

        insets.apply {
            if (fullscreen) {
                insets.hide(WindowInsetsCompat.Type.statusBars() or WindowInsetsCompat.Type.navigationBars())
                insets.systemBarsBehavior =
                    WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
            } else {
                insets.show(WindowInsetsCompat.Type.statusBars() or WindowInsetsCompat.Type.navigationBars())
                insets.systemBarsBehavior =
                    WindowInsetsControllerCompat.BEHAVIOR_DEFAULT
            }
        }
    }


    @SuppressLint("RestrictedApi")
    override fun dispatchKeyEvent(event: KeyEvent?): Boolean {
        event?.apply {
            if (physicalControllerManager.onKeyEvent(this))
                return true
        }
        return super.dispatchKeyEvent(event)
    }

    override fun dispatchGenericMotionEvent(ev: MotionEvent?): Boolean {
        ev?.apply {
            physicalControllerManager.onMotionEvent(this)
        }
        return super.dispatchGenericMotionEvent(ev)
    }

    override fun onStop() {
        super.onStop()

        if(isGameRunning) {
            NativeHelpers.instance.setTurboMode(false)
            force60HzRefreshRate(false)
        }
    }

    override fun onResume() {
        super.onResume()

        if(isGameRunning) {
            setFullScreen(true)
            NativeHelpers.instance.setTurboMode(true)
            force60HzRefreshRate(true)
            if (QuickSettings(this).enableMotion)
                motionSensorManager.register()
        }
    }

    override fun onPause() {
        super.onPause()

        if(isGameRunning) {
            NativeHelpers.instance.setTurboMode(false)
            force60HzRefreshRate(false)
        }

        motionSensorManager.unregister()
    }
}
