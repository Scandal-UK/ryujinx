package org.ryujinx.android.viewmodels

import android.annotation.SuppressLint
import android.content.Context
import android.os.Build
import android.os.PerformanceHintManager
import androidx.compose.runtime.MutableState
import androidx.navigation.NavHostController
import org.ryujinx.android.GameHost
import org.ryujinx.android.MainActivity
import org.ryujinx.android.PerformanceManager

@SuppressLint("WrongConstant")
class MainViewModel(val activity: MainActivity) {
    var performanceManager: PerformanceManager? = null
    var selected: GameModel? = null
    private var gameTimeState: MutableState<Double>? = null
    private var gameFpsState: MutableState<Double>? = null
    private var fifoState: MutableState<Double>? = null
    private var navController : NavHostController? = null

    var homeViewModel: HomeViewModel = HomeViewModel(activity, this)

    init {
        if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            var hintService =
                activity.getSystemService(Context.PERFORMANCE_HINT_SERVICE) as PerformanceHintManager
            performanceManager = PerformanceManager(hintService)
        }
    }

    fun loadGame(game:GameModel) {
        var controller = navController?: return;
        activity.setFullScreen()
        GameHost.gameModel = game
        controller.navigate("game")
    }

    fun setNavController(controller: NavHostController) {
        navController = controller
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
}