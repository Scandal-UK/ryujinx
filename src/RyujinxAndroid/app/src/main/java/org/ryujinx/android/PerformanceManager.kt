package org.ryujinx.android

import android.os.Build
import android.os.PerformanceHintManager
import androidx.annotation.RequiresApi

class PerformanceManager(val performanceHintManager: PerformanceHintManager) {
    private var _isEnabled: Boolean = false
    private var renderingSession: PerformanceHintManager.Session? = null
    val DEFAULT_TARGET_NS = 16666666L

    @RequiresApi(Build.VERSION_CODES.S)
    fun initializeRenderingSession(threadId : Long){
        if(!_isEnabled || renderingSession != null)
            return

        val threads = IntArray(1)
        threads[0] = threadId.toInt()
        renderingSession = performanceHintManager.createHintSession(threads, DEFAULT_TARGET_NS)
    }

    @RequiresApi(Build.VERSION_CODES.S)
    fun closeCurrentRenderingSession() {
        if (_isEnabled)
            renderingSession?.apply {
                renderingSession = null
                this.close()
            }
    }

    fun enable(){
        _isEnabled = true
    }

    @RequiresApi(Build.VERSION_CODES.S)
    fun updateRenderingSessionTime(newTime : Long){
        if(!_isEnabled)
            return

        var effectiveTime = newTime

        if(newTime < DEFAULT_TARGET_NS)
            effectiveTime = DEFAULT_TARGET_NS

        renderingSession?.apply {
            this.reportActualWorkDuration(effectiveTime)
        }
    }
}