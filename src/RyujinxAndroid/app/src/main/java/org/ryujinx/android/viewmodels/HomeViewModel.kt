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
    private var loadedCache: List<GameModel> = listOf()
    private var gameFolderPath: DocumentFile? = null
    private var sharedPref: SharedPreferences? = null
    val gameList: SnapshotStateList<GameModel> = SnapshotStateList()

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

    fun reloadGameList() {
        var storage = activity?.storageHelper ?: return
        
        if(isLoading)
            return
        val folder = gameFolderPath ?: return

        gameList.clear()
        
        isLoading = true
        thread {
            try {
                val files = mutableListOf<GameModel>()
                for (file in folder.search(false, DocumentFileType.FILE)) {
                    if (file.extension == "xci" || file.extension == "nsp")
                        activity.let {
                            val item = GameModel(file, it)
                            files.add(item)
                            gameList.add(item)
                        }
                }

                loadedCache = files.toList()

                isLoading = false
            } finally {
                isLoading = false
            }
        }
    }

    fun clearLoadedCache(){
        loadedCache = listOf()
    }
}
