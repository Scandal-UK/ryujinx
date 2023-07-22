package org.ryujinx.android.views

import android.content.res.Resources
import android.view.Gravity
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.DockedSearchBar
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FabPosition
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SearchBarDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.DialogWindowProvider
import androidx.compose.ui.zIndex
import androidx.navigation.NavHostController
import coil.compose.AsyncImage
import org.ryujinx.android.MainActivity
import org.ryujinx.android.R
import org.ryujinx.android.viewmodels.GameModel
import org.ryujinx.android.viewmodels.HomeViewModel
import java.io.File
import kotlin.math.roundToInt

class HomeViews {
    companion object {
        const val ImageSize = 150

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun MainTopBar(navController: NavHostController) {
            val topBarSize = remember {
                mutableStateOf(0)
            }
            Column {
                val showOptionsPopup = remember {
                    mutableStateOf(false)
                }
                TopAppBar(
                    modifier = Modifier
                        .zIndex(1f)
                        .padding(top = 16.dp)
                        .onSizeChanged {
                            topBarSize.value = it.height
                        },
                    title = {
                        DockedSearchBar(
                            shape = SearchBarDefaults.inputFieldShape,
                            query = "",
                            onQueryChange = {},
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
                                Text(text = "Search Games")
                            }
                        ) {

                        }
                    },
                    actions = {
                        IconButton(
                            onClick = {
                                showOptionsPopup.value = true
                            }
                        ) {
                            Icon(
                                Icons.Filled.MoreVert,
                                contentDescription = "More"
                            )
                        }
                    }
                )
                Box {
                    if(showOptionsPopup.value)
                    {
                        AlertDialog(
                            modifier = Modifier.padding(top = (topBarSize.value / Resources.getSystem().displayMetrics.density + 10).dp,
                            start = 16.dp, end = 16.dp),
                            onDismissRequest = {
                                showOptionsPopup.value = false
                            }) {
                            val dialogWindowProvider = LocalView.current.parent as DialogWindowProvider
                            dialogWindowProvider.window.setGravity(Gravity.TOP)
                            Surface(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .wrapContentHeight()
                                    .padding(16.dp),
                                shape = MaterialTheme.shapes.large,
                                tonalElevation = AlertDialogDefaults.TonalElevation
                            ) {
                                Column {
                                    TextButton(onClick = {
                                                         navController.navigate("settings")
                                    }, modifier = Modifier.fillMaxWidth()
                                        .align(Alignment.Start),
                                    ) {
                                        Icon(
                                            Icons.Filled.Settings,
                                            contentDescription = "Settings"
                                        )
                                        Text(text = "Settings", modifier = Modifier.padding(16.dp)
                                            .align(Alignment.CenterVertically))
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun Home(viewModel: HomeViewModel = HomeViewModel(), navController: NavHostController? = null) {
            val sheetState = rememberModalBottomSheetState()
            val scope = rememberCoroutineScope()
            val showBottomSheet = remember { mutableStateOf(false) }

            Scaffold(
                modifier = Modifier.fillMaxSize(),
                topBar = {
                    navController?.apply {
                        MainTopBar(navController)
                    }
                },
                floatingActionButtonPosition = FabPosition.End,
                floatingActionButton = {
                    FloatingActionButton(onClick = {
                       viewModel.openGameFolder()
                    },
                    shape = CircleShape) {
                            Icon(
                                Icons.Filled.Add,
                                contentDescription = "Options"
                            )
                    }
                }

            ) { contentPadding ->
                Box(modifier = Modifier.padding(contentPadding)) {
                    val list = remember {
                        mutableStateListOf<GameModel>()
                    }
                    viewModel.setViewList(list)
                    LazyColumn(Modifier.fillMaxSize()) {
                        items(list) {
                            it.titleName?.apply {
                                if (this.isNotEmpty())
                                    GameItem(it, viewModel, showBottomSheet)
                            }
                        }
                    }
                }
                
                if(showBottomSheet.value) {
                    ModalBottomSheet(onDismissRequest = { 
                        showBottomSheet.value = false
                    },
                    sheetState = sheetState) {
                        val openDialog = remember { mutableStateOf(false) }

                        if(openDialog.value) {
                            AlertDialog(onDismissRequest = { 
                                openDialog.value = false
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
                                    TitleUpdateViews.Main(titleId, name, openDialog)
                                }

                            }
                        }
                        Surface(color =  MaterialTheme.colorScheme.surface,
                        modifier = Modifier.padding(16.dp)) {
                            Column(modifier = Modifier.fillMaxSize()) {
                                Row(modifier = Modifier.align(Alignment.CenterHorizontally)) {
                                    Card(
                                        onClick = {
                                            openDialog.value = true
                                        }
                                    ) {
                                        Column(modifier = Modifier.padding(16.dp)) {
                                            Icon(
                                                painter = painterResource(R.drawable.app_update),
                                                contentDescription = "More",
                                                tint = Color.Green,
                                                modifier = Modifier
                                                    .width(48.dp)
                                                    .height(48.dp)
                                                    .align(Alignment.CenterHorizontally)
                                            )
                                            Text(text = "Game Updates",
                                                modifier = Modifier.align(Alignment.CenterHorizontally),
                                            color = MaterialTheme.colorScheme.onSurface)

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        @OptIn(ExperimentalFoundationApi::class)
        @Composable
        fun GameItem(gameModel: GameModel, viewModel: HomeViewModel, showSheet : MutableState<Boolean>) {
            Card(shape = MaterialTheme.shapes.medium,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(8.dp)
                    .combinedClickable(
                        onClick = {
                            if (gameModel.titleId.isNullOrEmpty() || gameModel.titleId != "0000000000000000") {
                                viewModel.mainViewModel?.loadGame(gameModel)
                            }
                        },
                        onLongClick = {
                            viewModel.mainViewModel?.selected = gameModel
                            showSheet.value = true
                        })) {
                Row(modifier = Modifier
                    .fillMaxWidth()
                    .padding(8.dp),
                    horizontalArrangement = Arrangement.SpaceBetween) {
                    Row {
                        if(!gameModel.titleId.isNullOrEmpty() && gameModel.titleId != "0000000000000000")
                        {
                            val iconSource = MainActivity.AppPath + "/iconCache/" + gameModel.iconCache
                            val imageFile = File(iconSource)
                            if(imageFile.exists()) {
                                val size = ImageSize / Resources.getSystem().displayMetrics.density
                                AsyncImage(model = imageFile,
                                    contentDescription = gameModel.titleName + " icon",
                                modifier = Modifier
                                    .padding(end = 8.dp)
                                    .width(size.roundToInt().dp)
                                    .height(size.roundToInt().dp))
                            }
                            else NotAvailableIcon()
                        } else NotAvailableIcon()
                        Column{
                            Text(text = gameModel.titleName ?: "")
                            Text(text = gameModel.developer ?: "")
                            Text(text = gameModel.titleId ?: "")
                        }
                    }
                    Column{
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