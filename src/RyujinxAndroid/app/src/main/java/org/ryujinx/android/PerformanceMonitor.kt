package org.ryujinx.android

import android.app.ActivityManager
import android.content.Context.ACTIVITY_SERVICE
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import java.io.RandomAccessFile

class PerformanceMonitor {
    val numberOfCores = Runtime.getRuntime().availableProcessors()

    fun getFrequencies() : List<Double> {
        val frequencies = mutableListOf<Double>()
        for (i in 0..<numberOfCores){
            var freq = 0.0;
            try {
                val reader = RandomAccessFile(
                    "/sys/devices/system/cpu/cpu${i}/cpufreq/scaling_cur_freq",
                    "r"
                )
                val f = reader.readLine()
                reader.close()
                freq = f.toDouble() / 1000.0
            }
            catch (e:Exception){

            }

            frequencies.add(freq)
        }

        return  frequencies.toList()
    }

    fun getMemoryUsage() : List<Int> {
        val mem = mutableListOf<Int>()
        MainActivity.mainViewModel?.activity?.apply {
            val actManager = getSystemService(ACTIVITY_SERVICE) as ActivityManager
            val memInfo = ActivityManager.MemoryInfo()
            actManager.getMemoryInfo(memInfo)
            val availMemory = memInfo.availMem.toDouble()/(1024*1024)
            val totalMemory= memInfo.totalMem.toDouble()/(1024*1024)

            mem.add((totalMemory - availMemory).toInt())
            mem.add(totalMemory.toInt())
        }
        return mem.toList()
    }

    @Composable
    fun RenderUsage() {
        LazyColumn{
            val frequencies = getFrequencies()
            val mem = getMemoryUsage()

            for (i in 0..<numberOfCores){
                item {
                    Row {
                        Text(modifier = Modifier.padding(2.dp), text = "CPU ${i}")
                        Spacer(Modifier.weight(1f))
                        Text(text = "${frequencies[i]} MHz")
                    }
                }
            }

            if(mem.isNotEmpty()) {
                item {
                    Row {
                        Text(modifier = Modifier.padding(2.dp), text = "Used")
                        Spacer(Modifier.weight(1f))
                        Text(text = "${mem[0]} MB")
                    }
                }
                item {
                    Row {
                        Text(modifier = Modifier.padding(2.dp), text = "Total")
                        Spacer(Modifier.weight(1f))
                        Text(text = "${mem[1]} MB")
                    }
                }
            }
        }
    }
}