package org.ryujinx.android

import android.annotation.SuppressLint
import android.content.Context
import android.os.Build
import android.view.SurfaceHolder
import android.view.SurfaceView
import org.ryujinx.android.viewmodels.GameModel
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.QuickSettings
import kotlin.concurrent.thread

@SuppressLint("ViewConstructor")
class GameHost(context: Context?, private val mainViewModel: MainViewModel) : SurfaceView(context), SurfaceHolder.Callback {
    private var game: GameModel? = null
    private var _isClosed: Boolean = false
    private var _renderingThreadWatcher: Thread? = null
    private var _height: Int = 0
    private var _width: Int = 0
    private var _updateThread: Thread? = null
    private var _guestThread: Thread? = null
    private var _isInit: Boolean = false
    private var _isStarted: Boolean = false
    private val nativeWindow : NativeWindow

    private var _nativeRyujinx: RyujinxNative = RyujinxNative()

    init {
        holder.addCallback(this)

        nativeWindow = NativeWindow(this)
    }

    override fun surfaceCreated(holder: SurfaceHolder) {
    }

    override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        if(_isClosed)
            return
        start(holder)

        if(_width != width || _height != height)
        {
            val window = nativeWindow.requeryWindowHandle()
            _nativeRyujinx.graphicsSetSurface(window, nativeWindow.nativePointer)

            nativeWindow.swapInterval = 0;
        }

        _width = width
        _height = height

        _nativeRyujinx.graphicsRendererSetSize(
            width,
            height
        )

        if(_isStarted)
        {
            _nativeRyujinx.inputSetClientSize(width, height)
        }
    }

    override fun surfaceDestroyed(holder: SurfaceHolder) {

    }

    fun close(){
        _isClosed = true
        _isInit = false
        _isStarted = false

        _updateThread?.join()
        _renderingThreadWatcher?.join()
    }

    private fun start(surfaceHolder: SurfaceHolder) {
        mainViewModel.gameHost = this
        if(_isStarted)
            return

        game = mainViewModel.gameModel

        _nativeRyujinx.inputInitialize(width, height)

        val settings = QuickSettings(mainViewModel.activity)

        if(!settings.useVirtualController){
            mainViewModel.controller?.setVisible(false)
        }
        else{
            mainViewModel.controller?.connect()
        }

        mainViewModel.physicalControllerManager?.connect()

        _nativeRyujinx.graphicsRendererSetSize(
            surfaceHolder.surfaceFrame.width(),
            surfaceHolder.surfaceFrame.height()
        )

        _guestThread = thread(start = true) {
            runGame()
        }
        _isStarted = true

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

    private fun runGame() {
        // RenderingThreadWatcher
        _renderingThreadWatcher = thread(start = true) {
            var threadId = 0L
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                mainViewModel.performanceManager?.enable()
                while (_isStarted) {
                    Thread.sleep(1000)
                    val newthreadId = mainViewModel.activity.getRenderingThreadId()

                    if (threadId != newthreadId) {
                        mainViewModel.performanceManager?.closeCurrentRenderingSession()
                    }
                    threadId = newthreadId
                    if (threadId != 0L) {
                        mainViewModel.performanceManager?.initializeRenderingSession(threadId)
                    }
                }
                mainViewModel.performanceManager?.closeCurrentRenderingSession()
            }
        }
        _nativeRyujinx.graphicsRendererRunLoop()

        game?.close()
    }
}
