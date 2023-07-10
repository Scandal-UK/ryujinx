package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.preference.PreferenceManager
import org.ryujinx.android.MainActivity

class QuickSettings(val activity: MainActivity) {
    var ignoreMissingServices: Boolean
    var enablePtc: Boolean
    var enableDocked: Boolean
    var enableVsync: Boolean
    var useNce: Boolean
    var isHostMapped: Boolean
    var enableShaderCache: Boolean
    var enableTextureRecompression: Boolean
    var resScale : Float

    private var sharedPref: SharedPreferences = PreferenceManager.getDefaultSharedPreferences(activity)

    init {
        isHostMapped = sharedPref.getBoolean("isHostMapped", true)
        useNce = sharedPref.getBoolean("useNce", true)
        enableVsync = sharedPref.getBoolean("enableVsync", true)
        enableDocked = sharedPref.getBoolean("enableDocked", true)
        enablePtc = sharedPref.getBoolean("enablePtc", true)
        ignoreMissingServices = sharedPref.getBoolean("ignoreMissingServices", false)
        enableShaderCache = sharedPref.getBoolean("enableShaderCache", true)
        enableTextureRecompression = sharedPref.getBoolean("enableTextureRecompression", false)
        resScale = sharedPref.getFloat("resScale", 1f)
    }
}