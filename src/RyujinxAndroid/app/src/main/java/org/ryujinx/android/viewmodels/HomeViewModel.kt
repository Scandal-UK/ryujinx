package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.documentfile.provider.DocumentFile
import androidx.preference.PreferenceManager
import com.anggrayudi.storage.file.DocumentFileCompat
import com.anggrayudi.storage.file.DocumentFileType
import com.anggrayudi.storage.file.FileFullPath
import com.anggrayudi.storage.file.extension
import com.anggrayudi.storage.file.getAbsolutePath
import com.anggrayudi.storage.file.search
import org.ryujinx.android.MainActivity
import kotlin.concurrent.thread

class HomeViewModel(
    val activity: MainActivity? = null,
    val mainViewModel: MainViewModel? = null
) {
    private var isLoading: Boolean = false
    private var gameList: SnapshotStateList<GameModel>? = null
    private var loadedCache: List<GameModel> = listOf()
    private var gameFolderPath: DocumentFile? = null
    private var sharedPref: SharedPreferences? = null

    init {
        if (activity != null) {
            sharedPref = PreferenceManager.getDefaultSharedPreferences(activity)
            activity.storageHelper!!.onFolderSelected = { requestCode, folder ->
                run {
                    gameFolderPath = folder
                    val p = folder.getAbsolutePath(activity!!)
                    val editor = sharedPref?.edit()
                    editor?.putString("gameFolder", p)
                    editor?.apply()
                    reloadGameList()
                }
            }

            val savedFolder = sharedPref?.getString("gameFolder", "") ?: ""

            if (savedFolder.isNotEmpty()) {
                try {
                    gameFolderPath = DocumentFileCompat.fromFullPath(
                        activity,
                        savedFolder,
                        documentType = DocumentFileType.FOLDER,
                        requiresWriteAccess = true
                    )

                    reloadGameList()
                } catch (e: Exception) {

                }
            }
        }
    }

    fun openGameFolder() {
        val path = sharedPref?.getString("gameFolder", "") ?: ""

        if (path.isEmpty())
            activity?.storageHelper?.storage?.openFolderPicker()
        else
            activity?.storageHelper?.storage?.openFolderPicker(
                activity.storageHelper!!.storage.requestCodeFolderPicker,
                FileFullPath(activity, path)
            )
    }

    fun reloadGameList(ignoreCache: Boolean = false) {
        var storage = activity?.storageHelper ?: return
        
        if(isLoading)
            return
        val folder = gameFolderPath ?: return
        
        isLoading = true

        if(!ignoreCache) {
            val files = mutableListOf<GameModel>()

            thread {
                try {
                    for (file in folder.search(false, DocumentFileType.FILE)) {
                        if (file.extension == "xci" || file.extension == "nsp")
                            activity.let {
                                files.add(GameModel(file, it))
                            }
                    }

                    loadedCache = files.toList()

                    isLoading = false

                    applyFilter()
                } finally {
                    isLoading = false
                }
            }
        }
        else{
            isLoading = false
            applyFilter()
        }
    }

    private fun applyFilter() {
        if(isLoading)
            return
        gameList?.clear()
        gameList?.addAll(loadedCache)
    }

    fun setViewList(list: SnapshotStateList<GameModel>) {
        gameList = list
        reloadGameList(loadedCache.isNotEmpty())
    }

    fun clearLoadedCache(){
        loadedCache = listOf()
    }
}
