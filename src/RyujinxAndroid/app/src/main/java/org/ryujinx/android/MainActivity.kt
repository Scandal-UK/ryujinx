package org.ryujinx.android

import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.view.WindowManager
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import androidx.core.view.WindowCompat
import com.anggrayudi.storage.SimpleStorageHelper
import org.ryujinx.android.ui.theme.RyujinxAndroidTheme
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.views.MainView


class MainActivity : BaseActivity() {
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
