package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.documentfile.provider.DocumentFile
import androidx.preference.PreferenceManager
import com.anggrayudi.storage.file.DocumentFileCompat
import com.anggrayudi.storage.file.DocumentFileType
import com.anggrayudi.storage.file.extension
import com.anggrayudi.storage.file.search
import org.ryujinx.android.MainActivity
import java.util.Locale
import kotlin.concurrent.thread

class HomeViewModel(
    val activity: MainActivity? = null,
    val mainViewModel: MainViewModel? = null
) {
    private var shouldReload: Boolean = false
    private var savedFolder: String = ""
    private var isLoading: Boolean = false
    private var loadedCache: MutableList<GameModel> = mutableListOf()
    private var gameFolderPath: DocumentFile? = null
    private var sharedPref: SharedPreferences? = null
    val gameList: SnapshotStateList<GameModel> = SnapshotStateList()

    init {
        if (activity != null) {
            sharedPref = PreferenceManager.getDefaultSharedPreferences(activity)
        }
    }

    fun ensureReloadIfNecessary() {
        val oldFolder = savedFolder
        savedFolder = sharedPref?.getString("gameFolder", "") ?: ""

        if (savedFolder.isNotEmpty() && (shouldReload || savedFolder != oldFolder)) {
            gameFolderPath = DocumentFileCompat.fromFullPath(
                mainViewModel?.activity!!,
                savedFolder,
                documentType = DocumentFileType.FOLDER,
                requiresWriteAccess = true
            )

            reloadGameList()
        }
    }

    fun filter(query : String){
        gameList.clear()
        gameList.addAll(loadedCache.filter { it.titleName != null && it.titleName!!.isNotEmpty() && (query.trim()
            .isEmpty() || it.titleName!!.lowercase(Locale.getDefault())
            .contains(query)) })
    }

    fun requestReload(){
        shouldReload = true
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
                loadedCache.clear()
                val files = mutableListOf<GameModel>()
                for (file in folder.search(false, DocumentFileType.FILE)) {
                    if (file.extension == "xci" || file.extension == "nsp" || file.extension == "nro")
                        activity.let {
                            val item = GameModel(file, it)

                            if(item.titleId?.isNotEmpty() == true && item.titleName?.isNotEmpty() == true) {
                                loadedCache.add(item)
                                gameList.add(item)
                            }
                        }
                }

                isLoading = false
            } finally {
                isLoading = false
            }
        }
    }
}
