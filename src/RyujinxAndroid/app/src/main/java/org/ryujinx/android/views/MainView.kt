package org.ryujinx.android.views

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.lifecycleScope
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import org.ryujinx.android.GameController
import org.ryujinx.android.GameHost
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.SettingsViewModel

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
            Box {
                var controller = GameController(mainViewModel.activity)
                AndroidView(
                    modifier = Modifier.fillMaxSize(),
                    factory = { context ->
                        GameHost(context, controller, mainViewModel)
                    }
                )
                GameStats(mainViewModel)
                controller.Compose(mainViewModel.activity.lifecycleScope, mainViewModel.activity.lifecycle)
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