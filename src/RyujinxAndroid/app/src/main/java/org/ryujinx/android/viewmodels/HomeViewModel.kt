package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.compose.runtime.snapshots.SnapshotStateList
import androidx.documentfile.provider.DocumentFile
import androidx.preference.PreferenceManager
import com.anggrayudi.storage.file.DocumentFileCompat
import com.anggrayudi.storage.file.DocumentFileType
import com.anggrayudi.storage.file.FileFullPath
import com.anggrayudi.storage.file.extension
import com.anggrayudi.storage.file.fullName
import com.anggrayudi.storage.file.getAbsolutePath
import com.anggrayudi.storage.file.search
import org.ryujinx.android.MainActivity

class HomeViewModel(
    val activity: MainActivity? = null,
    val mainViewModel: MainViewModel? = null
) {
    private var gameList: SnapshotStateList<GameModel>? = null
    private var loadedCache: List<GameModel> = listOf()
    private var gameFolderPath: DocumentFile? = null
    private var sharedPref: SharedPreferences? = null;

    init {
        if (activity != null) {
            sharedPref = PreferenceManager.getDefaultSharedPreferences(activity)
            activity.storageHelper!!.onFolderSelected = { requestCode, folder ->
                run {
                    gameFolderPath = folder
                    var p = folder.getAbsolutePath(activity!!)
                    var editor = sharedPref?.edit()
                    editor?.putString("gameFolder", p);
                    editor?.apply()
                    reloadGameList()
                }
            }

            var savedFolder = sharedPref?.getString("gameFolder", "") ?: ""

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
        var path = sharedPref?.getString("gameFolder", "") ?: ""

        if (path.isNullOrEmpty())
            activity?.storageHelper?.storage?.openFolderPicker();
        else
            activity?.storageHelper?.storage?.openFolderPicker(
                activity!!.storageHelper!!.storage.requestCodeFolderPicker,
                FileFullPath(activity, path)
            )
    }

    fun reloadGameList() {
        var storage = activity?.storageHelper ?: return
        var folder = gameFolderPath ?: return

        var files = mutableListOf<GameModel>()

        for (file in folder.search(false, DocumentFileType.FILE)) {
            if (file.extension == "xci" || file.extension == "nsp")
                activity?.let {
                    files.add(GameModel(file, it))
                }
        }

        loadedCache = files.toList()

        applyFilter()
    }

    private fun applyFilter() {
        gameList?.clear()
        gameList?.addAll(loadedCache)
    }

    fun setViewList(list: SnapshotStateList<GameModel>) {
        gameList = list;
        applyFilter()
    }
}