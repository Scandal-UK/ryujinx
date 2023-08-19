package org.ryujinx.android.viewmodels

import androidx.compose.runtime.MutableState
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.compose.ui.text.intl.Locale
import androidx.compose.ui.text.toLowerCase
import androidx.documentfile.provider.DocumentFile
import com.anggrayudi.storage.SimpleStorageHelper
import com.anggrayudi.storage.callback.FileCallback
import com.anggrayudi.storage.file.DocumentFileCompat
import com.anggrayudi.storage.file.DocumentFileType
import com.anggrayudi.storage.file.copyFileTo
import com.anggrayudi.storage.file.getAbsolutePath
import com.google.gson.Gson
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.ryujinx.android.MainActivity
import java.io.File
import java.util.LinkedList
import java.util.Queue
import kotlin.math.max

class TitleUpdateViewModel(val titleId: String) {
    private var basePath: String
    private var updateJsonName = "updates.json"
    private var stagingUpdateJsonName = "staging_updates.json"
    private var storageHelper: SimpleStorageHelper
    var pathsState: SnapshotStateList<String>? = null

    companion object {
        const val UpdateRequestCode = 1002
    }

    fun Remove(index: Int) {
        if (index <= 0)
            return

        data?.paths?.apply {
            removeAt(index - 1)
            pathsState?.clear()
            pathsState?.addAll(this)
        }
    }

    fun Add() {
        val callBack = storageHelper.onFileSelected

        storageHelper.onFileSelected = { requestCode, files ->
            run {
                storageHelper.onFileSelected = callBack
                if(requestCode == UpdateRequestCode)
                {
                    val file = files.firstOrNull()
                    file?.apply {
                        val path = file.getAbsolutePath(storageHelper.storage.context)
                        if(path.isNotEmpty()){
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

    fun save(
        index: Int,
        isCopying: MutableState<Boolean>,
        openDialog: MutableState<Boolean>,
        copyProgress: MutableState<Float>,
        currentProgressName: MutableState<String>
    ) {
        data?.apply {
            this.selected = ""
            if (paths.isNotEmpty() && index > 0) {
                val ind = max(index - 1, paths.count() - 1)
                this.selected = paths[ind]
            }
            val gson = Gson()
            var json = gson.toJson(this)
            File(basePath).mkdirs()
            File("$basePath/$stagingUpdateJsonName").writeText(json)

            // Copy updates to internal data folder
            val updatePath = "$basePath/update"
            File(updatePath).mkdirs()

            val ioScope = CoroutineScope(Dispatchers.IO)

            var metadata = TitleUpdateMetadata()
            var queue: Queue<String> = LinkedList()

            var callback: FileCallback? = null

            fun copy(path: String) {
                isCopying.value = true
                val documentFile = DocumentFileCompat.fromFullPath(
                    storageHelper.storage.context,
                    path,
                    DocumentFileType.FILE
                )
                documentFile?.apply {
                    val stagedPath = "$basePath/${name}"
                    if (!File(stagedPath).exists()) {
                        var file = this
                        ioScope.launch {
                            file.copyFileTo(
                                storageHelper.storage.context,
                                File(updatePath),
                                callback = callback!!
                            )

                        }

                        metadata.paths.add(stagedPath)
                    }
                }
            }

            fun finish() {
                val savedUpdates = mutableListOf<String>()
                File(updatePath).listFiles()?.forEach { savedUpdates.add(it.absolutePath) }
                var missingFiles =
                    savedUpdates.filter { i -> paths.find { it.endsWith(File(i).name) } == null }
                for (path in missingFiles) {
                    File(path).delete()
                }

                val selectedName = File(selected).name
                val newSelectedPath = "$updatePath/$selectedName"
                if (File(newSelectedPath).exists()) {
                    metadata.selected = newSelectedPath
                }

                json = gson.toJson(metadata)
                File("$basePath/$updateJsonName").writeText(json)

                openDialog.value = false
                isCopying.value = false
            }
            callback = object : FileCallback() {
                override fun onFailed(errorCode: FileCallback.ErrorCode) {
                    super.onFailed(errorCode)
                }

                override fun onStart(file: Any, workerThread: Thread): Long {
                    copyProgress.value = 0f

                    (file as DocumentFile)?.apply {
                        currentProgressName.value = "Copying ${file.name}"
                    }
                    return super.onStart(file, workerThread)
                }

                override fun onReport(report: Report) {
                    super.onReport(report)

                    copyProgress.value = report.progress / 100f
                }

                override fun onCompleted(result: Any) {
                    super.onCompleted(result)

                    if (queue.isNotEmpty())
                        copy(queue.remove())
                    else {
                        finish()
                    }
                }
            }
            for (path in paths) {
                queue.add(path)
            }

            ioScope.launch {
                if (queue.isNotEmpty()) {
                    copy(queue.remove())
                } else {
                    finish()
                }

            }
        }
    }

    fun setPaths(paths: SnapshotStateList<String>) {
        pathsState = paths
        data?.apply {
            pathsState?.clear()
            pathsState?.addAll(this.paths)
        }
    }

    var data: TitleUpdateMetadata? = null
    private var jsonPath: String

    init {
        basePath = MainActivity.AppPath + "/games/" + titleId.toLowerCase(Locale.current)
        val stagingJson = "${basePath}/${stagingUpdateJsonName}"
        jsonPath = "${basePath}/${updateJsonName}"

        data = TitleUpdateMetadata()
        if (File(stagingJson).exists()) {
            val gson = Gson()
            data = gson.fromJson(File(stagingJson).readText(), TitleUpdateMetadata::class.java)

            data?.apply {
                val existingPaths = mutableListOf<String>()
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
