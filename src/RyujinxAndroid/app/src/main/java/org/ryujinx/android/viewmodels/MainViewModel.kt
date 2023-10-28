package org.ryujinx.android.viewmodels

import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.PerformanceHintManager
import androidx.compose.runtime.MutableState
import androidx.navigation.NavHostController
import com.anggrayudi.storage.extension.launchOnUiThread
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.sync.Semaphore
import org.ryujinx.android.GameActivity
import org.ryujinx.android.GameController
import org.ryujinx.android.GameHost
import org.ryujinx.android.GraphicsConfiguration
import org.ryujinx.android.MainActivity
import org.ryujinx.android.NativeGraphicsInterop
import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.PerformanceManager
import org.ryujinx.android.PhysicalControllerManager
import org.ryujinx.android.RegionCode
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.SystemLanguage
import java.io.File

@SuppressLint("WrongConstant")
class MainViewModel(val activity: MainActivity) {
    var physicalControllerManager: PhysicalControllerManager? = null
    var gameModel: GameModel? = null
    var controller: GameController? = null
    var performanceManager: PerformanceManager? = null
    var selected: GameModel? = null
    private var gameTimeState: MutableState<Double>? = null
    private var gameFpsState: MutableState<Double>? = null
    private var fifoState: MutableState<Double>? = null
    private var progress: MutableState<String>? = null
    private var progressValue: MutableState<Float>? = null
    private var showLoading: MutableState<Boolean>? = null
    private var refreshUser: MutableState<Boolean>? = null
    var gameHost: GameHost? = null
        set(value) {
            field = value
            field?.setProgressStates(showLoading, progressValue, progress)
        }
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
    }

    fun loadGame(game:GameModel) : Boolean {
        val nativeRyujinx = RyujinxNative()

        val descriptor = game.open()

        if (descriptor == 0)
            return false

        gameModel = game

        val settings = QuickSettings(activity)

        var success = nativeRyujinx.graphicsInitialize(GraphicsConfiguration().apply {
            EnableShaderCache = settings.enableShaderCache
            EnableTextureRecompression = settings.enableTextureRecompression
            ResScale = settings.resScale
            BackendThreading = org.ryujinx.android.BackendThreading.Auto.ordinal
        })

        if (!success)
            return false

        val nativeHelpers = NativeHelpers()
        val nativeInterop = NativeGraphicsInterop()
        nativeInterop.VkRequiredExtensions = arrayOf(
            "VK_KHR_surface", "VK_KHR_android_surface"
        )
        nativeInterop.VkCreateSurface = nativeHelpers.getCreateSurfacePtr()
        nativeInterop.SurfaceHandle = 0

        val driverViewModel = VulkanDriverViewModel(activity)
        val drivers = driverViewModel.getAvailableDrivers()

        var driverHandle = 0L

        if (driverViewModel.selected.isNotEmpty()) {
            val metaData = drivers.find { it.driverPath == driverViewModel.selected }

            metaData?.apply {
                val privatePath = activity.filesDir
                val privateDriverPath = privatePath.canonicalPath + "/driver/"
                val pD = File(privateDriverPath)
                if (pD.exists())
                    pD.deleteRecursively()

                pD.mkdirs()

                val driver = File(driverViewModel.selected)
                val parent = driver.parentFile
                if (parent != null) {
                    for (file in parent.walkTopDown()) {
                        if (file.absolutePath == parent.absolutePath)
                            continue
                        file.copyTo(File(privateDriverPath + file.name), true)
                    }
                }

                driverHandle = NativeHelpers().loadDriver(
                    activity.applicationInfo.nativeLibraryDir!! + "/",
                    privateDriverPath,
                    this.libraryName
                )
            }

        }

        success = nativeRyujinx.graphicsInitializeRenderer(
            nativeInterop.VkRequiredExtensions!!,
            driverHandle
        )
        if (!success)
            return false

        val semaphore = Semaphore(1, 0)
        runBlocking {
            semaphore.acquire()
            launchOnUiThread {
                // We are only able to initialize the emulation context on the main thread
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

                semaphore.release()
            }
            semaphore.acquire()
            semaphore.release()
        }

        if (!success)
            return false

        success = nativeRyujinx.deviceLoadDescriptor(descriptor, game.isXci())

        if (!success)
            return false

        return true
    }

    fun clearPptcCache(titleId :String){
        if(titleId.isNotEmpty()){
            val basePath = MainActivity.AppPath + "/games/$titleId/cache/cpu"
            if(File(basePath).exists()){
                var caches = mutableListOf<String>()

                val mainCache = basePath + "${File.separator}0"
                File(mainCache).listFiles()?.forEach {
                    if(it.isFile && it.name.endsWith(".cache"))
                        caches.add(it.absolutePath)
                }
                val backupCache = basePath + "${File.separator}1"
                File(backupCache).listFiles()?.forEach {
                    if(it.isFile && it.name.endsWith(".cache"))
                        caches.add(it.absolutePath)
                }
                for(path in caches)
                    File(path).delete()
            }
        }
    }

    fun purgeShaderCache(titleId :String) {
        if(titleId.isNotEmpty()){
            val basePath = MainActivity.AppPath + "/games/$titleId/cache/shader"
            if(File(basePath).exists()){
                var caches = mutableListOf<String>()
                File(basePath).listFiles()?.forEach {
                    if(!it.isFile)
                        it.delete()
                    else{
                        if(it.name.endsWith(".toc") || it.name.endsWith(".data"))
                            caches.add(it.absolutePath)
                    }
                }
                for(path in caches)
                    File(path).delete()
            }
        }
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

    fun navigateToGame() {
        val intent = Intent(activity, GameActivity::class.java)
        activity.startActivity(intent)
    }

    fun setProgressStates(
        showLoading: MutableState<Boolean>,
        progressValue: MutableState<Float>,
        progress: MutableState<String>
    ) {
        this.showLoading = showLoading
        this.progressValue = progressValue
        this.progress = progress
        gameHost?.setProgressStates(showLoading, progressValue, progress)
    }

    fun setRefreshUserState(refreshUser: MutableState<Boolean>)
    {
        this.refreshUser = refreshUser
    }

    fun requestUserRefresh(){
        refreshUser?.apply {
            value = true
        }
    }
}
