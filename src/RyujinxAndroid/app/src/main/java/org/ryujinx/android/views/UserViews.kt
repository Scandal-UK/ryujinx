package org.ryujinx.android.views

import android.graphics.BitmapFactory
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.Image
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.viewmodels.MainViewModel
import java.util.Base64

class UserViews {
    companion object {
        @OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
        @Composable
        fun Main(viewModel: MainViewModel? = null, navController: NavHostController? = null) {
            val ryujinxNative = RyujinxNative()
            val decoder = Base64.getDecoder()
            ryujinxNative.userGetOpenedUser()
            val openedUser = remember {
                mutableStateOf(NativeHelpers().popStringJava())
            }

            val openedUserPic = remember {
                mutableStateOf(decoder.decode(ryujinxNative.userGetUserPicture(openedUser.value)))
            }
            val openedUserName = remember {
                mutableStateOf(ryujinxNative.userGetUserName(openedUser.value))
            }

            val userList = remember {
                mutableListOf("")
            }

            fun refresh() {
                userList.clear()
                userList.addAll(ryujinxNative.userGetAllUsers())
            }

            refresh()

            Scaffold(modifier = Modifier.fillMaxSize(),
                topBar = {
                    TopAppBar(title = {
                        Text(text = "Users")
                    },
                        navigationIcon = {
                            IconButton(onClick = {
                                viewModel?.navController?.popBackStack()
                            }) {
                                Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                            }
                        })
                }) { contentPadding ->
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(contentPadding)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(8.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(text = "Selected user")
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(4.dp),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Image(
                                bitmap = BitmapFactory.decodeByteArray(
                                    openedUserPic.value,
                                    0,
                                    openedUserPic.value.size
                                ).asImageBitmap(),
                                contentDescription = "selected image",
                                contentScale = ContentScale.Crop,
                                modifier = Modifier
                                    .padding(4.dp)
                                    .size(96.dp)
                                    .clip(CircleShape)
                            )
                            Column(
                                modifier = Modifier.fillMaxWidth(),
                                verticalArrangement = Arrangement.spacedBy(4.dp)
                            ) {
                                Text(text = openedUserName.value)
                                Text(text = openedUser.value)
                            }
                        }

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Available Users")
                            IconButton(onClick = {
                                refresh()
                            }) {
                                Icon(
                                    imageVector = Icons.Filled.Refresh,
                                    contentDescription = "refresh users"
                                )
                            }
                        }
                        LazyVerticalGrid(
                            columns = GridCells.Adaptive(minSize = 96.dp),
                            modifier = Modifier
                                .fillMaxSize()
                                .padding(4.dp)
                        ) {
                            items(userList) { user ->
                                val pic = decoder.decode(ryujinxNative.userGetUserPicture(user))
                                val name = ryujinxNative.userGetUserName(user)
                                Image(
                                    bitmap = BitmapFactory.decodeByteArray(pic, 0, pic.size)
                                        .asImageBitmap(),
                                    contentDescription = "selected image",
                                    contentScale = ContentScale.Crop,
                                    modifier = Modifier
                                        .fillMaxSize()
                                        .padding(4.dp)
                                        .clip(CircleShape)
                                        .align(Alignment.CenterHorizontally)
                                        .combinedClickable(
                                            onClick = {
                                                ryujinxNative.userOpenUser(user)
                                                openedUser.value = user
                                                openedUserPic.value = pic
                                                openedUserName.value = name
                                                viewModel?.requestUserRefresh()
                                            })
                                )
                            }
                        }
                    }

                }
            }
        }

    }

    @Preview
    @Composable
    fun Preview() {
        UserViews.Main()
    }
}
