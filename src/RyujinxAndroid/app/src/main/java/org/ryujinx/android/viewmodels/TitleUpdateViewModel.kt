package org.ryujinx.android.viewmodels

import androidx.compose.runtime.MutableState
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
    private var canClose: MutableState<Boolean>? = null
    private var basePath: String
    private var updateJsonName = "updates.json"
    private var storageHelper: SimpleStorageHelper
    var pathsState: SnapshotStateList<String>? = null

    companion object {
        const val UpdateRequestCode = 1002
    }

    fun Remove(index: Int) {
        if (index <= 0)
            return

        data?.paths?.apply {
            val removed = removeAt(index - 1)
            File(removed).deleteRecursively()
            pathsState?.clear()
            pathsState?.addAll(this)
        }
    }

    fun Add(
        isCopying: MutableState<Boolean>,
        copyProgress: MutableState<Float>,
        currentProgressName: MutableState<String>
    ) {
        val callBack = storageHelper.onFileSelected

        storageHelper.onFileSelected = { requestCode, files ->
            run {
                storageHelper.onFileSelected = callBack
                if (requestCode == UpdateRequestCode) {
                    val file = files.firstOrNull()
                    file?.apply {
                        // Copy updates to internal data folder
                        val updatePath = "$basePath/update"
                        File(updatePath).mkdirs()
                        Helpers.copyToData(
                            this,
                            updatePath,
                            storageHelper,
                            isCopying,
                            copyProgress,
                            currentProgressName, ::refreshPaths
                        )
                    }
                }
            }
        }
        storageHelper.openFilePicker(UpdateRequestCode)
    }

    fun refreshPaths() {
        data?.apply {
            val updatePath = "$basePath/update"
            val existingPaths = mutableListOf<String>()
            File(updatePath).listFiles()?.forEach { existingPaths.add(it.absolutePath) }

            if (!existingPaths.contains(selected)) {
                selected = ""
            }
            pathsState?.clear()
            pathsState?.addAll(existingPaths)
            paths = existingPaths
            canClose?.apply {
                value = true
            }
        }
    }

    fun save(
        index: Int,
        openDialog: MutableState<Boolean>
    ) {
        data?.apply {
            val updatePath = "$basePath/update"
            this.selected = ""
            if (paths.isNotEmpty() && index > 0) {
                val ind = max(index - 1, paths.count() - 1)
                this.selected = paths[ind]
            }
            val gson = Gson()
            File(basePath).mkdirs()


            var metadata = TitleUpdateMetadata()
            val savedUpdates = mutableListOf<String>()
            File(updatePath).listFiles()?.forEach { savedUpdates.add(it.absolutePath) }
            metadata.paths = savedUpdates

            val selectedName = File(selected).name
            val newSelectedPath = "$updatePath/$selectedName"
            if (File(newSelectedPath).exists()) {
                metadata.selected = newSelectedPath
            }

            var json = gson.toJson(metadata)
            File("$basePath/$updateJsonName").writeText(json)

            openDialog.value = false
        }
    }

    fun setPaths(paths: SnapshotStateList<String>, canClose: MutableState<Boolean>) {
        pathsState = paths
        this.canClose = canClose
        data?.apply {
            pathsState?.clear()
            pathsState?.addAll(this.paths)
        }
    }

    var data: TitleUpdateMetadata? = null
    private var jsonPath: String

    init {
        basePath = MainActivity.AppPath + "/games/" + titleId.toLowerCase(Locale.current)
        jsonPath = "${basePath}/${updateJsonName}"

        data = TitleUpdateMetadata()
        if (File(jsonPath).exists()) {
            val gson = Gson()
            data = gson.fromJson(File(jsonPath).readText(), TitleUpdateMetadata::class.java)

            refreshPaths()
        }

        storageHelper = MainActivity.StorageHelper!!
    }
}

data class TitleUpdateMetadata(
    var selected: String = "",
    var paths: MutableList<String> = mutableListOf()
)
