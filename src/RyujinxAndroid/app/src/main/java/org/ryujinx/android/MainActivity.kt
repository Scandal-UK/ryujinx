package org.ryujinx.android

import android.annotation.SuppressLint
import android.content.Context
import android.content.pm.ActivityInfo
import android.media.AudioDeviceInfo
import android.media.AudioManager
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.view.KeyEvent
import android.view.MotionEvent
import android.view.WindowManager
import androidx.activity.ComponentActivity
import androidx.activity.addCallback
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Preview
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.anggrayudi.storage.SimpleStorageHelper
import org.ryujinx.android.ui.theme.RyujinxAndroidTheme
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.VulkanDriverViewModel
import org.ryujinx.android.views.HomeViews
import org.ryujinx.android.views.MainView
import java.io.File


class MainActivity : ComponentActivity() {
    var physicalControllerManager: PhysicalControllerManager = PhysicalControllerManager(this)
    private var _isInit: Boolean = false
    var storageHelper: SimpleStorageHelper? = null
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
        }
    }

    init {
        storageHelper = SimpleStorageHelper(this)
        StorageHelper = storageHelper
        System.loadLibrary("ryujinxjni")
        initVm()
    }

    external fun getRenderingThreadId() : Long
    external fun initVm()

    fun setFullScreen(fullscreen: Boolean) {
        requestedOrientation =
            if (fullscreen) ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE else ActivityInfo.SCREEN_ORIENTATION_FULL_USER

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

    private fun getAudioDevice () : Int {
        val audioManager = getSystemService(Context.AUDIO_SERVICE) as AudioManager

        val devices = audioManager.getDevices(AudioManager.GET_DEVICES_OUTPUTS)

        return if (devices.isEmpty())
            0
        else {
            val speaker = devices.find { it.type == AudioDeviceInfo.TYPE_BUILTIN_SPEAKER }
            val earPiece = devices.find { it.type == AudioDeviceInfo.TYPE_WIRED_HEADPHONES || it.type == AudioDeviceInfo.TYPE_WIRED_HEADSET }
            if(earPiece != null)
                return earPiece.id
            if(speaker != null)
                return  speaker.id
            devices.first().id
        }
    }

    @SuppressLint("RestrictedApi")
    override fun dispatchKeyEvent(event: KeyEvent?): Boolean {
        event?.apply {
            if(physicalControllerManager.onKeyEvent(this))
                return true;
        }
        return super.dispatchKeyEvent(event)
    }

    override fun dispatchGenericMotionEvent(ev: MotionEvent?): Boolean {
        ev?.apply {
            physicalControllerManager.onMotionEvent(this)
        }
        return super.dispatchGenericMotionEvent(ev)
    }

    private fun initialize() {
        if (_isInit)
            return

        val appPath: String = AppPath
        val success = RyujinxNative().initialize(appPath, false)
        _isInit = success
    }
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

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

        mainViewModel?.apply {
            setContent {
                RyujinxAndroidTheme {
                    // A surface container using the 'background' color from the theme
                    Surface(
                        modifier = Modifier.fillMaxSize(),
                        color = MaterialTheme.colorScheme.background
                    ) {
                        /*Box {
                        AndroidView(
                            modifier = Modifier.fillMaxSize(),
                            factory = { context ->
                                GameHost(context)
                            }
                        )
                        controller.Compose(lifecycleScope, lifecycle)
                    }*/
                        MainView.Main(mainViewModel = this)
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
}

@Composable
fun Greeting(name: String, modifier: Modifier = Modifier) {

}

@Preview(showBackground = true)
@Composable
fun GreetingPreview() {
    RyujinxAndroidTheme {
        HomeViews.Home()
    }
}