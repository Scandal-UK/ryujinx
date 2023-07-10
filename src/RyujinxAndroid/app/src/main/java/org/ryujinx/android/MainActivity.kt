package org.ryujinx.android

import android.content.Context
import android.content.Intent
import android.content.pm.ActivityInfo
import android.media.AudioDeviceInfo
import android.media.AudioManager
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.view.WindowManager
import androidx.activity.ComponentActivity
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
import org.ryujinx.android.viewmodels.HomeViewModel
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.views.HomeViews
import org.ryujinx.android.views.MainView


class MainActivity : ComponentActivity() {
    private var mainViewModel: MainViewModel? = null
    private var _isInit: Boolean = false
    var storageHelper: SimpleStorageHelper? = null
    companion object {
        var AppPath : String?
        var StorageHelper: SimpleStorageHelper? = null
        init {
            AppPath = ""
        }
    }

    init {
        storageHelper = SimpleStorageHelper(this)
        StorageHelper = storageHelper
    }

    fun setFullScreen() :Unit {
        requestedOrientation =
            ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE;
        window.attributes.layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES

        WindowCompat.setDecorFitsSystemWindows(window,false)

        var insets = WindowCompat.getInsetsController(window, window.decorView)

        insets?.apply {
            insets.hide(WindowInsetsCompat.Type.statusBars() or WindowInsetsCompat.Type.navigationBars())
            insets.systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
        }
    }

    private fun getAudioDevice () : Int {
        var audioManager = getSystemService(Context.AUDIO_SERVICE) as AudioManager

        var devices = audioManager.getDevices(AudioManager.GET_DEVICES_OUTPUTS);


        return if (devices.isEmpty())
            0
        else {
            var speaker = devices.find { it.type == AudioDeviceInfo.TYPE_BUILTIN_SPEAKER }
            var earPiece = devices.find { it.type == AudioDeviceInfo.TYPE_WIRED_HEADPHONES || it.type == AudioDeviceInfo.TYPE_WIRED_HEADSET }
            if(earPiece != null)
                return earPiece.id
            if(speaker != null)
                return  speaker.id
            devices.first().id
        }
    }

    private fun initialize() : Unit
    {
        if(_isInit)
            return

        var appPath: String = AppPath ?: return
        var success = RyujinxNative().initialize(appPath, false)
        _isInit = success
    }
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        AppPath = this.getExternalFilesDir(null)!!.absolutePath

        initialize()

        if(if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                !Environment.isExternalStorageManager()
            } else {
                false
            }
        ) {
            storageHelper?.storage?.requestFullStorageAccess()
        }
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