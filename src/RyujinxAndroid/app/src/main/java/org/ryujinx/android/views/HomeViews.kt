package org.ryujinx.android.views

import android.content.res.Resources
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
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.BottomAppBar
import androidx.compose.material3.BottomAppBarDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SearchBar
import androidx.compose.material3.SearchBarDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import coil.compose.AsyncImage
import com.anggrayudi.storage.extension.launchOnUiThread
import org.ryujinx.android.MainActivity
import org.ryujinx.android.NativeHelpers
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.viewmodels.GameModel
import org.ryujinx.android.viewmodels.HomeViewModel
import java.io.File
import java.util.Base64
import java.util.Locale
import kotlin.concurrent.thread
import kotlin.math.roundToInt

class HomeViews {
    companion object {
        const val ImageSize = 150

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun Home(
            viewModel: HomeViewModel = HomeViewModel(),
            navController: NavHostController? = null
        ) {
            val native = RyujinxNative()
            val showAppActions = remember { mutableStateOf(false) }
            val showLoading = remember { mutableStateOf(false) }
            val openTitleUpdateDialog = remember { mutableStateOf(false) }
            val canClose = remember { mutableStateOf(true) }
            val openDlcDialog = remember { mutableStateOf(false) }
            val query = remember {
                mutableStateOf("")
            }
            val refresh = remember {
                mutableStateOf(true)
            }
            val refreshUser = remember {
                mutableStateOf(true)
            }

            viewModel.mainViewModel?.setRefreshUserState(refreshUser)
            val user = remember {
                mutableStateOf("")
            }
            val pic = remember {
                mutableStateOf(ByteArray(0))
            }

            if (refreshUser.value) {
                native.userGetOpenedUser()
                user.value = NativeHelpers().popStringJava()
                if (user.value.isNotEmpty()) {
                    val decoder = Base64.getDecoder()
                    pic.value = decoder.decode(native.userGetUserPicture(user.value))
                }

                refreshUser.value = false;
            }
            Scaffold(
                modifier = Modifier.fillMaxSize(),
                topBar = {
                    TopAppBar(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(top = 8.dp),
                        title = {
                            SearchBar(
                                modifier = Modifier.fillMaxWidth(),
                                shape = SearchBarDefaults.inputFieldShape,
                                query = query.value,
                                onQueryChange = {
                                    query.value = it
                                },
                                onSearch = {},
                                active = false,
                                onActiveChange = {},
                                leadingIcon = {
                                    Icon(
                                        Icons.Filled.Search,
                                        contentDescription = "Search Games"
                                    )
                                },
                                placeholder = {
                                    Text(text = "Ryujinx")
                                }
                            ) { }
                        },
                        actions = {
                            IconButton(onClick = {
                                navController?.navigate("user")
                            }) {
                                if (pic.value.isNotEmpty()) {
                                    Image(
                                        bitmap = BitmapFactory.decodeByteArray(
                                            pic.value,
                                            0,
                                            pic.value.size
                                        )
                                            .asImageBitmap(),
                                        contentDescription = "user image",
                                        contentScale = ContentScale.Crop,
                                        modifier = Modifier
                                            .padding(4.dp)
                                            .size(52.dp)
                                            .clip(CircleShape)
                                    )
                                } else {
                                    Icon(
                                        Icons.Filled.Person,
                                        contentDescription = "user"
                                    )
                                }
                            }
                            IconButton(
                                onClick = {
                                    navController?.navigate("settings")
                                }
                            ) {
                                Icon(
                                    Icons.Filled.Settings,
                                    contentDescription = "Settings"
                                )
                            }
                        }
                    )
                },
                bottomBar = {
                    BottomAppBar(actions = {
                        if (showAppActions.value) {
                            IconButton(onClick = {
                            }) {
                                Icon(
                                    org.ryujinx.android.Icons.playArrow(MaterialTheme.colorScheme.onSurface),
                                    contentDescription = "Run"
                                )
                            }
                            val showAppMenu = remember { mutableStateOf(false) }
                            Box {
                                IconButton(onClick = {
                                    showAppMenu.value = true
                                }) {
                                    Icon(
                                        Icons.Filled.Menu,
                                        contentDescription = "Menu"
                                    )
                                }
                                DropdownMenu(
                                    expanded = showAppMenu.value,
                                    onDismissRequest = { showAppMenu.value = false }) {
                                    DropdownMenuItem(text = {
                                        Text(text = "Clear PPTC Cache")
                                    }, onClick = {
                                        showAppMenu.value = false
                                        viewModel.mainViewModel?.clearPptcCache(viewModel.mainViewModel?.selected?.titleId ?: "")
                                    })
                                    DropdownMenuItem(text = {
                                        Text(text = "Purge Shader Cache")
                                    }, onClick = {
                                        showAppMenu.value = false
                                        viewModel.mainViewModel?.purgeShaderCache(viewModel.mainViewModel?.selected?.titleId ?: "")
                                    })
                                    DropdownMenuItem(text = {
                                        Text(text = "Manage Updates")
                                    }, onClick = {
                                        showAppMenu.value = false
                                        openTitleUpdateDialog.value = true
                                    })
                                    DropdownMenuItem(text = {
                                        Text(text = "Manage DLC")
                                    }, onClick = {
                                        showAppMenu.value = false
                                        openDlcDialog.value = true
                                    })
                                }
                            }
                        }
                    },
                        floatingActionButton = {
                            FloatingActionButton(
                                onClick = {
                                    viewModel.openGameFolder()
                                },
                                containerColor = BottomAppBarDefaults.bottomAppBarFabColor,
                                elevation = FloatingActionButtonDefaults.bottomAppBarFabElevation()
                            ) {
                                Icon(
                                    org.ryujinx.android.Icons.folderOpen(MaterialTheme.colorScheme.onSurface),
                                    contentDescription = "Open Folder"
                                )
                            }
                        }
                    )
                }


            ) { contentPadding ->
                Box(modifier = Modifier.padding(contentPadding)) {
                    val list = remember {
                        mutableStateListOf<GameModel>()
                    }
                    if (refresh.value) {
                        viewModel.setViewList(list)
                        refresh.value = false
                        showAppActions.value = false
                    }
                    val selectedModel = remember {
                        mutableStateOf(viewModel.mainViewModel?.selected)
                    }
                    LazyColumn(Modifier.fillMaxSize()) {
                        items(list) {
                            it.titleName?.apply {
                                if (this.isNotEmpty() && (query.value.trim()
                                        .isEmpty() || this.lowercase(
                                        Locale.getDefault()
                                    )
                                        .contains(query.value))
                                )
                                    GameItem(
                                        it,
                                        viewModel,
                                        showAppActions,
                                        showLoading,
                                        selectedModel
                                    )
                            }
                        }
                    }
                }

                if (showLoading.value) {
                    AlertDialog(onDismissRequest = { }) {
                        Card(
                            modifier = Modifier
                                .padding(16.dp)
                                .fillMaxWidth(),
                            shape = MaterialTheme.shapes.medium
                        ) {
                            Column(
                                modifier = Modifier
                                    .padding(16.dp)
                                    .fillMaxWidth()
                            ) {
                                Text(text = "Loading")
                                LinearProgressIndicator(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(top = 16.dp)
                                )
                            }

                        }
                    }
                }

                if (openTitleUpdateDialog.value) {
                    AlertDialog(onDismissRequest = {
                        openTitleUpdateDialog.value = false
                    }) {
                        Surface(
                            modifier = Modifier
                                .wrapContentWidth()
                                .wrapContentHeight(),
                            shape = MaterialTheme.shapes.large,
                            tonalElevation = AlertDialogDefaults.TonalElevation
                        ) {
                            val titleId = viewModel.mainViewModel?.selected?.titleId ?: ""
                            val name = viewModel.mainViewModel?.selected?.titleName ?: ""
                            TitleUpdateViews.Main(titleId, name, openTitleUpdateDialog, canClose)
                        }

                    }
                }
                if (openDlcDialog.value) {
                    AlertDialog(onDismissRequest = {
                        openDlcDialog.value = false
                    }) {
                        Surface(
                            modifier = Modifier
                                .wrapContentWidth()
                                .wrapContentHeight(),
                            shape = MaterialTheme.shapes.large,
                            tonalElevation = AlertDialogDefaults.TonalElevation
                        ) {
                            val titleId = viewModel.mainViewModel?.selected?.titleId ?: ""
                            val name = viewModel.mainViewModel?.selected?.titleName ?: ""
                            DlcViews.Main(titleId, name, openDlcDialog)
                        }

                    }
                }
            }
        }

        @OptIn(ExperimentalFoundationApi::class)
        @Composable
        fun GameItem(
            gameModel: GameModel,
            viewModel: HomeViewModel,
            showAppActions: MutableState<Boolean>,
            showLoading: MutableState<Boolean>,
            selectedModel: MutableState<GameModel?>
        ) {
            remember {
                selectedModel
            }
            val color =
                if (selectedModel.value == gameModel) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surface

            Surface(
                shape = MaterialTheme.shapes.medium,
                color = color,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(8.dp)
                    .combinedClickable(
                        onClick = {
                            if (viewModel.mainViewModel?.selected != null) {
                                showAppActions.value = false
                                viewModel.mainViewModel?.apply {
                                    selected = null
                                }
                                selectedModel.value = null
                            } else if (gameModel.titleId.isNullOrEmpty() || gameModel.titleId != "0000000000000000") {
                                thread {
                                    showLoading.value = true
                                    val success =
                                        viewModel.mainViewModel?.loadGame(gameModel) ?: false
                                    if (success) {
                                        launchOnUiThread {
                                            viewModel.mainViewModel?.navigateToGame()
                                        }
                                    } else {
                                        gameModel.close()
                                    }
                                    showLoading.value = false
                                }
                            }
                        },
                        onLongClick = {
                            viewModel.mainViewModel?.selected = gameModel
                            showAppActions.value = true
                            selectedModel.value = gameModel
                        })
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Row {
                        if (!gameModel.titleId.isNullOrEmpty() && gameModel.titleId != "0000000000000000") {
                            val iconSource =
                                MainActivity.AppPath + "/iconCache/" + gameModel.iconCache
                            val imageFile = File(iconSource)
                            if (imageFile.exists()) {
                                val size = ImageSize / Resources.getSystem().displayMetrics.density
                                AsyncImage(
                                    model = imageFile,
                                    contentDescription = gameModel.titleName + " icon",
                                    modifier = Modifier
                                        .padding(end = 8.dp)
                                        .width(size.roundToInt().dp)
                                        .height(size.roundToInt().dp)
                                )
                            } else NotAvailableIcon()
                        } else NotAvailableIcon()
                        Column {
                            Text(text = gameModel.titleName ?: "")
                            Text(text = gameModel.developer ?: "")
                            Text(text = gameModel.titleId ?: "")
                        }
                    }
                    Column {
                        Text(text = gameModel.version ?: "")
                        Text(text = String.format("%.3f", gameModel.fileSize))
                    }
                }
            }
        }

        @Composable
        fun NotAvailableIcon() {
            val size = ImageSize / Resources.getSystem().displayMetrics.density
            Icon(
                Icons.Filled.Add,
                contentDescription = "Options",
                modifier = Modifier
                    .padding(end = 8.dp)
                    .width(size.roundToInt().dp)
                    .height(size.roundToInt().dp)
            )
        }

    }

    @Preview
    @Composable
    fun HomePreview() {
        Home()
    }
}
