package org.ryujinx.android.viewmodels

import androidx.appcompat.widget.ThemedSpinnerAdapter.Helper
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.compose.ui.text.intl.Locale
import androidx.compose.ui.text.toLowerCase
import com.anggrayudi.storage.SimpleStorageHelper
import com.google.gson.Gson
import org.ryujinx.android.Helpers
import org.ryujinx.android.MainActivity
import java.io.File
import kotlin.math.max

class TitleUpdateViewModel(val titleId: String) {
    private var storageHelper: SimpleStorageHelper
    var pathsState: SnapshotStateList<String>? = null

    companion object {
        const val UpdateRequestCode = 1002
    }

    fun Remove(index: Int) {
        if (index <= 0)
            return

        data?.paths?.apply {
            removeAt(index - 1);
            pathsState?.clear()
            pathsState?.addAll(this)
        }
    }

    fun Add() {
        var callBack = storageHelper.onFileSelected

        storageHelper.onFileSelected = { requestCode, files ->
            run {
                storageHelper.onFileSelected = callBack
                if(requestCode == UpdateRequestCode)
                {
                    var file = files.firstOrNull()
                    file?.apply {
                        var path = Helpers.getPath(storageHelper.storage.context, file.uri)
                        if(!path.isNullOrEmpty()){
                            data?.apply {
                                if(!paths.contains(path)) {
                                    paths.add(path)
                                    pathsState?.clear()
                                    pathsState?.addAll(paths)
                                }
                            }
                        }
                    }
                }
            }
        }
        storageHelper.openFilePicker(UpdateRequestCode)
    }

    fun save(index: Int) {
        data?.apply {
            this.selected = ""
            if(paths.isNotEmpty() && index > 0)
            {
                var ind = max(index - 1, paths.count() - 1)
                this.selected = paths[ind]
            }
            var gson = Gson()
            var json = gson.toJson(this)
            jsonPath = (MainActivity.AppPath
                ?: "") + "/games/" + titleId.toLowerCase(Locale.current)
            File(jsonPath).mkdirs()
            File(jsonPath + "/updates.json").writeText(json)
        }
    }

    fun setPaths(paths: SnapshotStateList<String>) {
        pathsState = paths;
        data?.apply {
            pathsState?.clear()
            pathsState?.addAll(this.paths)
        }
    }

    var data: TitleUpdateMetadata? = null
    private var jsonPath: String

    init {
        jsonPath = (MainActivity.AppPath
            ?: "") + "/games/" + titleId.toLowerCase(Locale.current) + "/updates.json"

        data = TitleUpdateMetadata()
        if (File(jsonPath).exists()) {
            var gson = Gson()
            data = gson.fromJson(File(jsonPath).readText(), TitleUpdateMetadata::class.java)

            data?.apply {
                var existingPaths = mutableListOf<String>()
                for (path in paths) {
                    if (File(path).exists()) {
                        existingPaths.add(path)
                    }
                }

                if(!existingPaths.contains(selected)){
                    selected = ""
                }
                pathsState?.clear()
                pathsState?.addAll(existingPaths)
                paths = existingPaths
            }
        }

        storageHelper = MainActivity.StorageHelper!!
    }
}

data class TitleUpdateMetadata(
    var selected: String = "",
    var paths: MutableList<String> = mutableListOf()
)