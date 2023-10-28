package org.ryujinx.android

import android.annotation.SuppressLint
import android.content.Intent
import android.content.pm.ActivityInfo
import android.os.Bundle
import android.view.KeyEvent
import android.view.MotionEvent
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.ui.window.Popup
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import compose.icons.CssGgIcons
import compose.icons.cssggicons.ToolbarBottom
import org.ryujinx.android.ui.theme.RyujinxAndroidTheme
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.QuickSettings
import kotlin.math.abs
import kotlin.math.roundToInt

class GameActivity : BaseActivity() {
    private var physicalControllerManager: PhysicalControllerManager =
        PhysicalControllerManager(this)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        MainActivity.mainViewModel!!.physicalControllerManager = physicalControllerManager
        setContent {
            RyujinxAndroidTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    GameView(mainViewModel = MainActivity.mainViewModel!!)
                }
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

        NativeHelpers().setTurboMode(false)
        force60HzRefreshRate(false)
    }

    override fun onResume() {
        super.onResume()

        setFullScreen(true)
        NativeHelpers().setTurboMode(true)
        force60HzRefreshRate(true)
    }

    override fun onPause() {
        super.onPause()

        NativeHelpers().setTurboMode(false)
        force60HzRefreshRate(false)
    }

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

    private fun setFullScreen(fullscreen: Boolean) {
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

    @Composable
    fun GameView(mainViewModel: MainViewModel) {
        Box(modifier = Modifier.fillMaxSize()) {
            AndroidView(
                modifier = Modifier.fillMaxSize(),
                factory = { context ->
                    GameHost(context, mainViewModel)
                }
            )
            GameOverlay(mainViewModel)
        }
    }

    @OptIn(ExperimentalMaterial3Api::class)
    @Composable
    fun GameOverlay(mainViewModel: MainViewModel) {
        Box(modifier = Modifier.fillMaxSize()) {
            GameStats(mainViewModel)

            val ryujinxNative = RyujinxNative()

            val showController = remember {
                mutableStateOf(QuickSettings(this@GameActivity).useVirtualController)
            }
            val enableVsync = remember {
                mutableStateOf(QuickSettings(this@GameActivity).enableVsync)
            }
            val showMore = remember {
                mutableStateOf(false)
            }

            val showLoading = remember {
                mutableStateOf(true)
            }

            val progressValue = remember {
                mutableStateOf(0.0f)
            }

            val progress = remember {
                mutableStateOf("Loading")
            }

            mainViewModel.setProgressStates(showLoading, progressValue, progress)

            // touch surface
            Surface(color = Color.Transparent, modifier = Modifier
                .fillMaxSize()
                .padding(0.dp)
                .pointerInput(Unit) {
                    awaitPointerEventScope {
                        while (true) {
                            val event = awaitPointerEvent()
                            if (showController.value)
                                continue

                            val change = event
                                .component1()
                                .firstOrNull()
                            change?.apply {
                                val position = this.position

                                when (event.type) {
                                    PointerEventType.Press -> {
                                        ryujinxNative.inputSetTouchPoint(
                                            position.x.roundToInt(),
                                            position.y.roundToInt()
                                        )
                                    }

                                    PointerEventType.Release -> {
                                        ryujinxNative.inputReleaseTouchPoint()

                                    }

                                    PointerEventType.Move -> {
                                        ryujinxNative.inputSetTouchPoint(
                                            position.x.roundToInt(),
                                            position.y.roundToInt()
                                        )

                                    }
                                }
                            }
                        }
                    }
                }) {
            }
            if (!showLoading.value) {
                GameController.Compose(mainViewModel)

                Row(
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(8.dp)
                ) {
                    IconButton(modifier = Modifier.padding(4.dp), onClick = {
                        showMore.value = true
                    }) {
                        Icon(
                            imageVector = CssGgIcons.ToolbarBottom,
                            contentDescription = "Open Panel"
                        )
                    }
                }

                if (showMore.value) {
                    Popup(
                        alignment = Alignment.BottomCenter,
                        onDismissRequest = { showMore.value = false }) {
                        Surface(
                            modifier = Modifier.padding(16.dp),
                            shape = MaterialTheme.shapes.medium
                        ) {
                            Row(modifier = Modifier.padding(8.dp)) {
                                IconButton(modifier = Modifier.padding(4.dp), onClick = {
                                    showMore.value = false
                                    showController.value = !showController.value
                                    mainViewModel.controller?.setVisible(showController.value)
                                }) {
                                    Icon(
                                        imageVector = Icons.videoGame(),
                                        contentDescription = "Toggle Virtual Pad"
                                    )
                                }
                                IconButton(modifier = Modifier.padding(4.dp), onClick = {
                                    showMore.value = false
                                    enableVsync.value = !enableVsync.value
                                    RyujinxNative().graphicsRendererSetVsync(enableVsync.value)
                                }) {
                                    Icon(
                                        imageVector = Icons.vSync(),
                                        tint = if (enableVsync.value) Color.Green else Color.Red,
                                        contentDescription = "Toggle VSync"
                                    )
                                }
                            }
                        }
                    }
                }
            }

            val showBackNotice = remember {
                mutableStateOf(false)
            }

            BackHandler {
                showBackNotice.value = true
            }

            if (showLoading.value) {
                Card(
                    modifier = Modifier
                        .padding(16.dp)
                        .fillMaxWidth(0.5f)
                        .align(Alignment.Center),
                    shape = MaterialTheme.shapes.medium
                ) {
                    Column(
                        modifier = Modifier
                            .padding(16.dp)
                            .fillMaxWidth()
                    ) {
                        Text(text = progress.value)

                        if (progressValue.value > -1)
                            LinearProgressIndicator(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(top = 16.dp),
                                progress = progressValue.value
                            )
                        else
                            LinearProgressIndicator(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(top = 16.dp)
                            )
                    }

                }
            }

            if (showBackNotice.value) {
                AlertDialog(onDismissRequest = { showBackNotice.value = false }) {
                    Column {
                        Surface(
                            modifier = Modifier
                                .wrapContentWidth()
                                .wrapContentHeight(),
                            shape = MaterialTheme.shapes.large,
                            tonalElevation = AlertDialogDefaults.TonalElevation
                        ) {
                            Column {
                                Column(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(16.dp)
                                ) {
                                    Text(text = "Are you sure you want to exit the game?")
                                    Text(text = "All unsaved data will be lost!")
                                }
                                Row(
                                    horizontalArrangement = Arrangement.End,
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(16.dp)
                                ) {
                                    Button(onClick = {
                                        showBackNotice.value = false
                                        mainViewModel.closeGame()
                                        setFullScreen(false)
                                        finishActivity(0)
                                    }, modifier = Modifier.padding(16.dp)) {
                                        Text(text = "Exit Game")
                                    }

                                    Button(onClick = {
                                        showBackNotice.value = false
                                    }, modifier = Modifier.padding(16.dp)) {
                                        Text(text = "Dismiss")
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    @Composable
    fun GameStats(mainViewModel: MainViewModel) {
        val fifo = remember {
            mutableStateOf(0.0)
        }
        val gameFps = remember {
            mutableStateOf(0.0)
        }
        val gameTime = remember {
            mutableStateOf(0.0)
        }

        Surface(
            modifier = Modifier.padding(16.dp),
            color = MaterialTheme.colorScheme.surface.copy(0.4f)
        ) {
            Column {
                var gameTimeVal = 0.0
                if (!gameTime.value.isInfinite())
                    gameTimeVal = gameTime.value
                Text(text = "${String.format("%.3f", fifo.value)} %")
                Text(text = "${String.format("%.3f", gameFps.value)} FPS")
                Text(text = "${String.format("%.3f", gameTimeVal)} ms")
            }
        }

        mainViewModel.setStatStates(fifo, gameFps, gameTime)
    }
}
