package org.ryujinx.android

import android.content.Context
import android.os.ParcelFileDescriptor
import android.view.MotionEvent
import android.view.SurfaceHolder
import android.view.SurfaceView
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.ryujinx.android.viewmodels.GameModel
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.QuickSettings
import kotlin.concurrent.thread
import kotlin.math.roundToInt

class GameHost(context: Context?, val controller: GameController, val mainViewModel: MainViewModel) : SurfaceView(context), SurfaceHolder.Callback {
    private var _height: Int = 0
    private var _width: Int = 0
    private var _updateThread: Thread? = null
    private var nativeInterop: NativeGraphicsInterop? = null
    private var _guestThread: Thread? = null
    private var _isInit: Boolean = false
    private var _isStarted: Boolean = false
    private var _nativeWindow: Long = 0
    private var _nativeHelper: NativeHelpers = NativeHelpers()

    private var _nativeRyujinx: RyujinxNative = RyujinxNative()

    companion object {
        var gameModel: GameModel? = null
    }

    init {
        holder.addCallback(this)
    }

    override fun onTouchEvent(event: MotionEvent?): Boolean {
        if (_isStarted)
            return  when (event!!.actionMasked) {
                MotionEvent.ACTION_MOVE -> {
                    _nativeRyujinx.inputSetTouchPoint(event.x.roundToInt(), event.y.roundToInt())
                    true
                }
                MotionEvent.ACTION_DOWN -> {
                    _nativeRyujinx.inputSetTouchPoint(event.x.roundToInt(), event.y.roundToInt())
                    true
                }
                MotionEvent.ACTION_UP -> {
                    _nativeRyujinx.inputReleaseTouchPoint()
                    true
                }
                else -> super.onTouchEvent(event)
            }
        return super.onTouchEvent(event)
    }

    override fun surfaceCreated(holder: SurfaceHolder) {
    }

    override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        var isStarted = _isStarted;

        start(holder)

        if(isStarted && (_width != width || _height != height))
        {
            var nativeHelpers = NativeHelpers()
            var window = nativeHelpers.getNativeWindow(holder.surface);
            _nativeRyujinx.graphicsSetSurface(window);
        }

        _width = width;
        _height = height;

        if(_isStarted)
        {
            _nativeRyujinx.inputSetClientSize(width, height)
        }
    }

    override fun surfaceDestroyed(holder: SurfaceHolder) {

    }

    private fun start(surfaceHolder: SurfaceHolder) : Unit {
        var game = gameModel ?: return
        var path = game.getPath() ?: return
        if (_isStarted)
            return

        var surface = surfaceHolder.surface;

        var settings = QuickSettings(mainViewModel.activity)

        var success = _nativeRyujinx.graphicsInitialize(GraphicsConfiguration().apply {
            EnableShaderCache = settings.enableShaderCache
            EnableTextureRecompression = settings.enableTextureRecompression
            ResScale = settings.resScale
        })


        var nativeHelpers = NativeHelpers()
        var window = nativeHelpers.getNativeWindow(surfaceHolder.surface);
        nativeInterop = NativeGraphicsInterop()
        nativeInterop!!.VkRequiredExtensions = arrayOf(
            "VK_KHR_surface", "VK_KHR_android_surface"
        );
        nativeInterop!!.VkCreateSurface = nativeHelpers.getCreateSurfacePtr()
        nativeInterop!!.SurfaceHandle = window;

        success = _nativeRyujinx.graphicsInitializeRenderer(
            nativeInterop!!.VkRequiredExtensions!!,
            window
        )


        success = _nativeRyujinx.deviceInitialize(
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
        );

        success = _nativeRyujinx.deviceLoad(path)

        _nativeRyujinx.inputInitialize(width, height)

        controller.connect()

        _nativeRyujinx.graphicsRendererSetSize(
            surfaceHolder.surfaceFrame.width(),
            surfaceHolder.surfaceFrame.height()
        );

        _guestThread = thread(start = true) {
            runGame()
        }
        _isStarted = success;

        _updateThread = thread(start = true) {
            var c = 0
            while (_isStarted) {
                _nativeRyujinx.inputUpdate()
                Thread.sleep(1)
                c++
                if (c >= 1000) {
                    c = 0
                    mainViewModel.updateStats(_nativeRyujinx.deviceGetGameFifo(), _nativeRyujinx.deviceGetGameFrameRate(), _nativeRyujinx.deviceGetGameFrameTime())
                }
            }
        }
    }

    private fun runGame() : Unit{
        _nativeRyujinx.graphicsRendererRunLoop()
    }

}