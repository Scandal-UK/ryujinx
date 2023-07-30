package org.ryujinx.android.viewmodels

import android.annotation.SuppressLint
import android.content.Context
import android.os.Build
import android.os.PerformanceHintManager
import androidx.compose.runtime.MutableState
import androidx.navigation.NavHostController
import org.ryujinx.android.GameController
import org.ryujinx.android.GameHost
import org.ryujinx.android.GraphicsConfiguration
import org.ryujinx.android.MainActivity
import org.ryujinx.android.NativeGraphicsInterop
import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.PerformanceManager
import org.ryujinx.android.RegionCode
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.SystemLanguage
import java.io.File

@SuppressLint("WrongConstant")
class MainViewModel(val activity: MainActivity) {
    var gameHost: GameHost? = null
    var controller: GameController? = null
    var performanceManager: PerformanceManager? = null
    var selected: GameModel? = null
    private var gameTimeState: MutableState<Double>? = null
    private var gameFpsState: MutableState<Double>? = null
    private var fifoState: MutableState<Double>? = null
    var navController : NavHostController? = null

    var homeViewModel: HomeViewModel = HomeViewModel(activity, this)

    init {
        if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            val hintService =
                activity.getSystemService(Context.PERFORMANCE_HINT_SERVICE) as PerformanceHintManager
            performanceManager = PerformanceManager(hintService)
        }
    }

    fun closeGame() {
        RyujinxNative().deviceSignalEmulationClose()
        gameHost?.close()
        RyujinxNative().deviceCloseEmulation()
        goBack()
        activity.setFullScreen(false)
    }

    fun goBack(){
        navController?.popBackStack()
    }

    fun loadGame(game:GameModel) : Boolean {
        var nativeRyujinx = RyujinxNative()

        val path = game.getPath() ?: return false

        val settings = QuickSettings(activity)

        var success = nativeRyujinx.graphicsInitialize(GraphicsConfiguration().apply {
            EnableShaderCache = settings.enableShaderCache
            EnableTextureRecompression = settings.enableTextureRecompression
            ResScale = settings.resScale
        })

        if(!success)
            return false

        val nativeHelpers = NativeHelpers()
        var nativeInterop = NativeGraphicsInterop()
        nativeInterop!!.VkRequiredExtensions = arrayOf(
            "VK_KHR_surface", "VK_KHR_android_surface"
        )
        nativeInterop!!.VkCreateSurface = nativeHelpers.getCreateSurfacePtr()
        nativeInterop!!.SurfaceHandle = 0

        var driverViewModel = VulkanDriverViewModel(activity);
        var drivers = driverViewModel.getAvailableDrivers()

        var driverHandle = 0L;

        if (driverViewModel.selected.isNotEmpty()) {
            var metaData = drivers.find { it.driverPath == driverViewModel.selected }

            metaData?.apply {
                var privatePath = activity.filesDir;
                var privateDriverPath = privatePath.canonicalPath + "/driver/"
                val pD = File(privateDriverPath)
                if (pD.exists())
                    pD.deleteRecursively()

                pD.mkdirs()

                var driver = File(driverViewModel.selected)
                var parent = driver.parentFile
                for (file in parent.walkTopDown()) {
                    if (file.absolutePath == parent.absolutePath)
                        continue
                    file.copyTo(File(privateDriverPath + file.name), true)
                }

                driverHandle = NativeHelpers().loadDriver(
                    activity.applicationInfo.nativeLibraryDir!! + "/",
                    privateDriverPath,
                    this.libraryName
                )
            }

        }

        success = nativeRyujinx.graphicsInitializeRenderer(
            nativeInterop!!.VkRequiredExtensions!!,
            driverHandle
        )
        if(!success)
            return false

        success = nativeRyujinx.deviceInitialize(
            settings.isHostMapped,
            settings.useNce,
            SystemLanguage.AmericanEnglish.ordinal,
            RegionCode.USA.ordinal,
            settings.enableVsync,
            settings.enableDocked,
            settings.enablePtc,
            false,
            "UTC",
            settings.ignoreMissingServices
        )
        if(!success)
            return false

        success = nativeRyujinx.deviceLoad(path)

        if(!success)
            return false

        return true
    }

    fun setStatStates(
        fifo: MutableState<Double>,
        gameFps: MutableState<Double>,
        gameTime: MutableState<Double>
    ) {
        fifoState = fifo
        gameFpsState = gameFps
        gameTimeState = gameTime
    }

    fun updateStats(
        fifo: Double,
        gameFps: Double,
        gameTime: Double
    ){
        fifoState?.apply {
            this.value = fifo
        }
        gameFpsState?.apply {
            this.value = gameFps
        }
        gameTimeState?.apply {
            this.value = gameTime
        }
    }

    fun setGameController(controller: GameController) {
        this.controller = controller
    }

    fun backCalled() {
    }
}