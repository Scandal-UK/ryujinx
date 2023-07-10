package org.ryujinx.android.viewmodels

import androidx.compose.runtime.MutableState
import com.anggrayudi.storage.file.extension
import com.google.gson.Gson
import org.ryujinx.android.Helpers
import org.ryujinx.android.MainActivity
import java.io.File
import java.util.zip.ZipFile

class VulkanDriverViewModel(val activity: MainActivity) {
    var selected: String = ""

    companion object {
        val DriverRequestCode: Int = 1003
        const val DriverFolder: String = "drivers"
    }

    private fun getAppPath() : String {
        var appPath =
            (MainActivity.AppPath ?: activity.getExternalFilesDir(null)?.absolutePath ?: "");
        appPath += "/"

        return appPath
    }

    fun ensureDriverPath() : File {
        var driverPath = getAppPath() + DriverFolder

        var driverFolder = File(driverPath)

        if(!driverFolder.exists())
            driverFolder.mkdirs()

        return driverFolder
    }

    fun getAvailableDrivers() : MutableList<DriverMetadata> {
        var driverFolder = ensureDriverPath()

        var folders = driverFolder.walkTopDown()

        var drivers = mutableListOf<DriverMetadata>()
        
        var selectedDriverFile = File(driverFolder.absolutePath + "/selected");
        if(selectedDriverFile.exists()){
            selected = selectedDriverFile.readText()

            if(!File(selected).exists()) {
                selected = ""
                saveSelected()
            }
        }

        var gson = Gson()

        for (folder in folders){
            if(folder.isDirectory() && folder.parent == driverFolder.absolutePath){
                var meta = File(folder.absolutePath + "/meta.json")

                if(meta.exists()){
                    var metadata = gson.fromJson(meta.readText(), DriverMetadata::class.java)
                    if(metadata.name.isNotEmpty()) {
                        var driver = folder.absolutePath + "/${metadata.libraryName}"
                        metadata.driverPath = driver
                        if (File(driver).exists())
                            drivers.add(metadata)
                    }
                }
            }
        }

        return drivers
    }

    fun saveSelected() {
        var driverFolder = ensureDriverPath()

        var selectedDriverFile = File(driverFolder.absolutePath + "/selected")
        selectedDriverFile.writeText(selected)
    }

    fun removeSelected(){
        if(selected.isNotEmpty()){
            var sel = File(selected)
            if(sel.exists()) {
                var parent = sel.parentFile
                parent.deleteRecursively()
            }
            selected = ""

            saveSelected()
        }
    }

    fun add(refresh: MutableState<Boolean>) {
        activity.storageHelper?.apply {

            var callBack = this.onFileSelected

            onFileSelected = { requestCode, files ->
                run {
                    onFileSelected = callBack
                    if(requestCode == DriverRequestCode)
                    {
                        var file = files.firstOrNull()
                        file?.apply {
                            var path = Helpers.getPath(storage.context, file.uri)
                            if(!path.isNullOrEmpty()){
                                var name = file.name?.removeSuffix("." + file.extension) ?: ""
                                var driverFolder = ensureDriverPath()
                                var extractionFolder = File(driverFolder.absolutePath + "/${name}")
                                extractionFolder.mkdirs()
                                ZipFile(path)?.use { zip ->
                                    zip.entries().asSequence().forEach { entry ->

                                        zip.getInputStream(entry).use { input ->
                                            val filePath = extractionFolder.absolutePath + File.separator + entry.name

                                            if (!entry.isDirectory) {
                                                var length = input.available()
                                                val bytesIn = ByteArray(length)
                                                input.read(bytesIn)
                                                File(filePath).writeBytes(bytesIn)
                                            } else {
                                                val dir = File(filePath)
                                                dir.mkdir()
                                            }

                                        }

                                    }
                                }
                            }
                        }

                        refresh.value = true
                    }
                }
            }
            openFilePicker(DriverRequestCode,
                filterMimeTypes = arrayOf("application/zip")
            )
        }
    }
}

data class DriverMetadata(
    var schemaVersion : Int = 0,
    var name : String = "",
    var description : String = "",
    var author : String = "",
    var packageVersion : String = "",
    var vendor : String = "",
    var driverVersion : String = "",
    var minApi : Int = 0,
    var libraryName : String = "",
    var driverPath : String = ""
)