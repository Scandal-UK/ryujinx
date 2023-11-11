package org.ryujinx.android.viewmodels

import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.RyujinxNative
import java.util.Base64

class UserViewModel {
    var openedUser = UserModel()
    val userList = mutableListOf<UserModel>()

    init {
        refreshUsers()
    }

    fun refreshUsers() {
        userList.clear()
        val native = RyujinxNative.instance
        val helper = NativeHelpers.instance
        val decoder = Base64.getDecoder()
        openedUser = UserModel()
        openedUser.id = helper.getStringJava(native.userGetOpenedUser())
        if (openedUser.id.isNotEmpty()) {
            openedUser.username =
                helper.getStringJava(native.userGetUserName(helper.storeStringJava(openedUser.id)))
            openedUser.userPicture = decoder.decode(
                helper.getStringJava(
                    native.userGetUserPicture(
                        helper.storeStringJava(openedUser.id)
                    )
                )
            )
        }

        val users = native.userGetAllUsers()
        for (user in users) {
            userList.add(
                UserModel(
                    user,
                    helper.getStringJava(native.userGetUserName(helper.storeStringJava(user))),
                    decoder.decode(
                        helper.getStringJava(
                            native.userGetUserPicture(
                                helper.storeStringJava(user)
                            )
                        )
                    )
                )
            )
        }
    }

    fun openUser(userModel: UserModel){
        val native = RyujinxNative.instance
        val helper = NativeHelpers.instance
        native.userOpenUser(helper.storeStringJava(userModel.id))

        refreshUsers()
    }
}


data class UserModel(var id : String = "", var username: String = "", var userPicture: ByteArray? = null) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as UserModel

        if (id != other.id) return false
        if (username != other.username) return false
        if (userPicture != null) {
            if (other.userPicture == null) return false
            if (!userPicture.contentEquals(other.userPicture)) return false
        } else if (other.userPicture != null) return false

        return true
    }

    override fun hashCode(): Int {
        var result = id.hashCode()
        result = 31 * result + username.hashCode()
        result = 31 * result + (userPicture?.contentHashCode() ?: 0)
        return result
    }
}
