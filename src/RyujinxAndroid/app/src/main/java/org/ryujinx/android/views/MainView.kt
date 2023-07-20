package org.ryujinx.android.views

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.PathFillType
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.path
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.lifecycleScope
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import org.ryujinx.android.GameController
import org.ryujinx.android.GameHost
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.SettingsViewModel
import kotlin.math.roundToInt

class MainView {
    companion object {
        @Composable
        fun Main(mainViewModel: MainViewModel){
            val navController = rememberNavController()
            mainViewModel.setNavController(navController)

            NavHost(navController = navController, startDestination = "home") {
                composable("home") { HomeViews.Home(mainViewModel.homeViewModel, navController) }
                composable("game") { GameView(mainViewModel) }
                composable("settings") { SettingViews.Main(SettingsViewModel(navController, mainViewModel.activity)) }
            }
        }

        @Composable
        fun GameView(mainViewModel: MainViewModel){
            Box(modifier = Modifier.fillMaxSize()) {
                val controller = remember {
                    GameController(mainViewModel.activity)
                }
                AndroidView(
                    modifier = Modifier.fillMaxSize(),
                    factory = { context ->
                        GameHost(context, controller, mainViewModel)
                    }
                )
                GameOverlay(mainViewModel, controller)
            }
        }

        @Composable
        fun GameOverlay(mainViewModel: MainViewModel, controller: GameController){
            Box(modifier = Modifier.fillMaxSize()) {
                GameStats(mainViewModel)

                var ryujinxNative = RyujinxNative()

                // touch surface
                Surface(color = Color.Transparent, modifier = Modifier
                    .fillMaxSize()
                    .padding(0.dp)
                    .pointerInput(Unit) {
                        awaitPointerEventScope {
                            while (true) {
                                Thread.sleep(2);
                                val event = awaitPointerEvent()

                                if(controller.isVisible)
                                    continue

                                var change = event
                                    .component1()
                                    .firstOrNull()
                                change?.apply {
                                    var position = this.position

                                    if (event.type == PointerEventType.Press) {
                                        ryujinxNative.inputSetTouchPoint(
                                            position.x.roundToInt(),
                                            position.y.roundToInt()
                                        )
                                    } else if (event.type == PointerEventType.Release) {
                                        ryujinxNative.inputReleaseTouchPoint()

                                    } else if (event.type == PointerEventType.Move) {
                                        ryujinxNative.inputSetTouchPoint(
                                            position.x.roundToInt(),
                                            position.y.roundToInt()
                                        )

                                    }
                                }
                            }
                        }
                    }) {
                }
                controller.Compose(mainViewModel.activity.lifecycleScope, mainViewModel.activity.lifecycle)
                Row(modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .padding(8.dp)) {
                    IconButton(modifier = Modifier.padding(4.dp),onClick = {
                        controller.setVisible(!controller.isVisible)
                    }) {
                        Icon(imageVector = rememberVideogameAsset(), contentDescription = "Toggle Virtual Pad")
                    }
                }
            }
        }
        @Composable
        fun rememberVideogameAsset(): ImageVector {
            var primaryColor = MaterialTheme.colorScheme.primary
            return remember {
                ImageVector.Builder(
                    name = "videogame_asset",
                    defaultWidth = 40.0.dp,
                    defaultHeight = 40.0.dp,
                    viewportWidth = 40.0f,
                    viewportHeight = 40.0f
                ).apply {
                    path(
                        fill = SolidColor(Color.Black.copy(alpha = 0.5f)),
                        fillAlpha = 1f,
                        stroke = SolidColor(primaryColor),
                        strokeAlpha = 1f,
                        strokeLineWidth = 1.0f,
                        strokeLineCap = StrokeCap.Butt,
                        strokeLineJoin = StrokeJoin.Miter,
                        strokeLineMiter = 1f,
                        pathFillType = PathFillType.NonZero
                    ) {
                        moveTo(6.25f, 29.792f)
                        quadToRelative(-1.083f, 0f, -1.854f, -0.792f)
                        quadToRelative(-0.771f, -0.792f, -0.771f, -1.833f)
                        verticalLineTo(12.833f)
                        quadToRelative(0f, -1.083f, 0.771f, -1.854f)
                        quadToRelative(0.771f, -0.771f, 1.854f, -0.771f)
                        horizontalLineToRelative(27.5f)
                        quadToRelative(1.083f, 0f, 1.854f, 0.771f)
                        quadToRelative(0.771f, 0.771f, 0.771f, 1.854f)
                        verticalLineToRelative(14.334f)
                        quadToRelative(0f, 1.041f, -0.771f, 1.833f)
                        reflectiveQuadToRelative(-1.854f, 0.792f)
                        close()
                        moveToRelative(0f, -2.625f)
                        horizontalLineToRelative(27.5f)
                        verticalLineTo(12.833f)
                        horizontalLineTo(6.25f)
                        verticalLineToRelative(14.334f)
                        close()
                        moveToRelative(7.167f, -1.792f)
                        quadToRelative(0.541f, 0f, 0.916f, -0.375f)
                        reflectiveQuadToRelative(0.375f, -0.917f)
                        verticalLineToRelative(-2.791f)
                        horizontalLineToRelative(2.75f)
                        quadToRelative(0.584f, 0f, 0.959f, -0.375f)
                        reflectiveQuadToRelative(0.375f, -0.917f)
                        quadToRelative(0f, -0.542f, -0.375f, -0.938f)
                        quadToRelative(-0.375f, -0.395f, -0.959f, -0.395f)
                        horizontalLineToRelative(-2.75f)
                        verticalLineToRelative(-2.75f)
                        quadToRelative(0f, -0.542f, -0.375f, -0.938f)
                        quadToRelative(-0.375f, -0.396f, -0.916f, -0.396f)
                        quadToRelative(-0.584f, 0f, -0.959f, 0.396f)
                        reflectiveQuadToRelative(-0.375f, 0.938f)
                        verticalLineToRelative(2.75f)
                        horizontalLineToRelative(-2.75f)
                        quadToRelative(-0.541f, 0f, -0.937f, 0.395f)
                        quadTo(8f, 19.458f, 8f, 20f)
                        quadToRelative(0f, 0.542f, 0.396f, 0.917f)
                        reflectiveQuadToRelative(0.937f, 0.375f)
                        horizontalLineToRelative(2.75f)
                        verticalLineToRelative(2.791f)
                        quadToRelative(0f, 0.542f, 0.396f, 0.917f)
                        reflectiveQuadToRelative(0.938f, 0.375f)
                        close()
                        moveToRelative(11.125f, -0.5f)
                        quadToRelative(0.791f, 0f, 1.396f, -0.583f)
                        quadToRelative(0.604f, -0.584f, 0.604f, -1.375f)
                        quadToRelative(0f, -0.834f, -0.604f, -1.417f)
                        quadToRelative(-0.605f, -0.583f, -1.396f, -0.583f)
                        quadToRelative(-0.834f, 0f, -1.417f, 0.583f)
                        quadToRelative(-0.583f, 0.583f, -0.583f, 1.375f)
                        quadToRelative(0f, 0.833f, 0.583f, 1.417f)
                        quadToRelative(0.583f, 0.583f, 1.417f, 0.583f)
                        close()
                        moveToRelative(3.916f, -5.833f)
                        quadToRelative(0.834f, 0f, 1.417f, -0.584f)
                        quadToRelative(0.583f, -0.583f, 0.583f, -1.416f)
                        quadToRelative(0f, -0.792f, -0.583f, -1.375f)
                        quadToRelative(-0.583f, -0.584f, -1.417f, -0.584f)
                        quadToRelative(-0.791f, 0f, -1.375f, 0.584f)
                        quadToRelative(-0.583f, 0.583f, -0.583f, 1.375f)
                        quadToRelative(0f, 0.833f, 0.583f, 1.416f)
                        quadToRelative(0.584f, 0.584f, 1.375f, 0.584f)
                        close()
                        moveTo(6.25f, 27.167f)
                        verticalLineTo(12.833f)
                        verticalLineToRelative(14.334f)
                        close()
                    }
                }.build()
            }
        }

        @Composable
        fun GameStats(mainViewModel: MainViewModel){
            var fifo = remember {
                mutableStateOf(0.0)
            }
            var gameFps = remember {
                mutableStateOf(0.0)
            }
            var gameTime = remember {
                mutableStateOf(0.0)
            }

            Surface(modifier = Modifier.padding(16.dp),
            color = MaterialTheme.colorScheme.surface.copy(0.4f)) {
                Column {
                    var gameTimeVal = 0.0;
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
}