package org.ryujinx.android.viewmodels

import android.content.Context
import android.net.Uri
import android.os.ParcelFileDescriptor
import androidx.documentfile.provider.DocumentFile
import com.anggrayudi.storage.file.extension
import org.ryujinx.android.Helpers
import org.ryujinx.android.RyujinxNative


class GameModel(var file: DocumentFile, val context: Context) {
    private var descriptor: ParcelFileDescriptor? = null
    var fileName: String?
    var fileSize = 0.0
    var titleName: String? = null
    var titleId: String? = null
    var developer: String? = null
    var version: String? = null
    var iconCache: String? = null

    init {
        fileName = file.name
        val absPath = getPath()
        val gameInfo = RyujinxNative().deviceGetGameInfoFromPath(absPath ?: "")

        fileSize = gameInfo.FileSize
        titleId = gameInfo.TitleId
        titleName = gameInfo.TitleName
        developer = gameInfo.Developer
        version = gameInfo.Version
        iconCache = gameInfo.IconCache
    }

    fun getPath() : String? {
        var uri = file.uri
        if (uri.scheme != "file")
            uri = Uri.parse("file://" + Helpers.getPath(context, file.uri))
        return uri.path
    }

    fun open() : Int {
        descriptor = context.contentResolver.openFileDescriptor(file.uri, "rw")

        return descriptor?.fd ?: 0
    }

    fun close() {
        descriptor?.close()
        descriptor = null
    }

    fun isXci() : Boolean {
        return file.extension == "xci"
    }
}

class GameInfo {
    var FileSize = 0.0
    var TitleName: String? = null
    var TitleId: String? = null
    var Developer: String? = null
    var Version: String? = null
    var IconCache: String? = null
}
