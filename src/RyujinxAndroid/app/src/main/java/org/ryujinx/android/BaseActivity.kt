package org.ryujinx.android

import android.os.Bundle
import android.os.PersistableBundle
import androidx.activity.ComponentActivity

abstract class BaseActivity : ComponentActivity() {
    companion object{
        val crashHandler = CrashHandler()
    }

    override fun onCreate(savedInstanceState: Bundle?, persistentState: PersistableBundle?) {
        Thread.setDefaultUncaughtExceptionHandler(crashHandler)
        super.onCreate(savedInstanceState, persistentState)
    }
}
