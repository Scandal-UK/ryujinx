package org.ryujinx.android.views

import android.annotation.SuppressLint
import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.tween
import androidx.compose.animation.core.updateTransition
import androidx.compose.animation.expandVertically
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Slider
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.documentfile.provider.DocumentFile
import com.anggrayudi.storage.file.extension
import org.ryujinx.android.Helpers
import org.ryujinx.android.MainActivity
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.SettingsViewModel
import org.ryujinx.android.viewmodels.VulkanDriverViewModel
import kotlin.concurrent.thread

class SettingViews {
    companion object {
        const val EXPANSTION_TRANSITION_DURATION = 450
        const val IMPORT_CODE = 12341

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun Main(settingsViewModel: SettingsViewModel, mainViewModel: MainViewModel) {
            val loaded = remember {
                mutableStateOf(false)
            }

            val isHostMapped = remember {
                mutableStateOf(false)
            }
            val useNce = remember {
                mutableStateOf(false)
            }
            val enableVsync = remember {
                mutableStateOf(false)
            }
            val enableDocked = remember {
                mutableStateOf(false)
            }
            val enablePtc = remember {
                mutableStateOf(false)
            }
            val ignoreMissingServices = remember {
                mutableStateOf(false)
            }
            val enableShaderCache = remember {
                mutableStateOf(false)
            }
            val enableTextureRecompression = remember {
                mutableStateOf(false)
            }
            val resScale = remember {
                mutableStateOf(1f)
            }
            val useVirtualController = remember {
                mutableStateOf(true)
            }

            if (!loaded.value) {
                settingsViewModel.initializeState(
                    isHostMapped,
                    useNce,
                    enableVsync, enableDocked, enablePtc, ignoreMissingServices,
                    enableShaderCache,
                    enableTextureRecompression,
                    resScale,
                    useVirtualController
                )
                loaded.value = true
            }
            Scaffold(modifier = Modifier.fillMaxSize(),
                topBar = {
                    TopAppBar(title = {
                        Text(text = "Settings")
                    },
                        modifier = Modifier.padding(top = 16.dp),
                        navigationIcon = {
                            IconButton(onClick = {
                                settingsViewModel.save(
                                    isHostMapped,
                                    useNce,
                                    enableVsync,
                                    enableDocked,
                                    enablePtc,
                                    ignoreMissingServices,
                                    enableShaderCache,
                                    enableTextureRecompression,
                                    resScale,
                                    useVirtualController
                                )
                                settingsViewModel.navController.popBackStack()
                            }) {
                                Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                            }
                        })
                }) { contentPadding ->
                Column(modifier = Modifier
                    .padding(contentPadding)
                    .verticalScroll(rememberScrollState())) {
                    ExpandableView(onCardArrowClick = { }, title = "System") {
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Use NCE",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = useNce.value, onCheckedChange = {
                                    useNce.value = !useNce.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Is Host Mapped",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = isHostMapped.value, onCheckedChange = {
                                    isHostMapped.value = !isHostMapped.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable VSync",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enableVsync.value, onCheckedChange = {
                                    enableVsync.value = !enableVsync.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable PTC",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enablePtc.value, onCheckedChange = {
                                    enablePtc.value = !enablePtc.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable Docked Mode",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enableDocked.value, onCheckedChange = {
                                    enableDocked.value = !enableDocked.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Ignore Missing Services",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = ignoreMissingServices.value, onCheckedChange = {
                                    ignoreMissingServices.value = !ignoreMissingServices.value
                                })
                            }
                            val isImporting = remember {
                                mutableStateOf(false)
                            }
                            val showImportWarning = remember {
                                mutableStateOf(false)
                            }
                            val showImportCompletion = remember {
                                mutableStateOf(false)
                            }
                            var importFile = remember {
                                mutableStateOf<DocumentFile?>(null)
                            }
                            Button(onClick = {
                                val storage = MainActivity.StorageHelper
                                storage?.apply {
                                    val s = this.storage
                                    val callBack = this.onFileSelected
                                    onFileSelected = { requestCode, files ->
                                        run {
                                            onFileSelected = callBack
                                            if (requestCode == IMPORT_CODE) {
                                                val file = files.firstOrNull()
                                                file?.apply {
                                                    if (this.extension == "zip") {
                                                        importFile.value = this
                                                        showImportWarning.value = true
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    openFilePicker(
                                        IMPORT_CODE,
                                        filterMimeTypes = arrayOf("application/zip")
                                    )
                                }
                            }) {
                                Text(text = "Import App Data")
                            }

                            if (showImportWarning.value) {
                                AlertDialog(onDismissRequest = {
                                    showImportWarning.value = false
                                    importFile.value = null
                                }) {
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
                                            Text(text = "Importing app data will delete your current profile. Do you still want to continue?")
                                            Row(
                                                horizontalArrangement = Arrangement.End,
                                                modifier = Modifier.fillMaxWidth()
                                            ) {
                                                Button(onClick = {
                                                    val file = importFile.value
                                                    showImportWarning.value = false
                                                    importFile.value = null
                                                    file?.apply {
                                                        thread {
                                                            Helpers.importAppData(this, isImporting)
                                                            showImportCompletion.value = true
                                                            mainViewModel.userViewModel.refreshUsers()
                                                        }
                                                    }
                                                }, modifier = Modifier.padding(horizontal = 8.dp)) {
                                                    Text(text = "Yes")
                                                }
                                                Button(onClick = {
                                                    showImportWarning.value = false
                                                    importFile.value = null
                                                }, modifier = Modifier.padding(horizontal = 8.dp)) {
                                                    Text(text = "No")
                                                }
                                            }
                                        }

                                    }
                                }
                            }

                            if (showImportCompletion.value) {
                                AlertDialog(onDismissRequest = {
                                    showImportCompletion.value = false
                                    importFile.value = null
                                    mainViewModel.userViewModel.refreshUsers()
                                    mainViewModel.homeViewModel.clearLoadedCache()
                                }) {
                                    Card(
                                        modifier = Modifier,
                                        shape = MaterialTheme.shapes.medium
                                    ) {
                                        Text(modifier = Modifier
                                            .padding(24.dp),
                                            text = "App Data import completed.")
                                    }
                                }
                            }

                            if (isImporting.value) {
                                Text(text = "Importing Files")

                                LinearProgressIndicator(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(8.dp)
                                )
                            }
                        }
                    }
                    ExpandableView(onCardArrowClick = { }, title = "Graphics") {
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable Shader Cache",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enableShaderCache.value, onCheckedChange = {
                                    enableShaderCache.value = !enableShaderCache.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Resolution Scale",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Text(text = resScale.value.toString() + "x")
                            }
                            Slider(value = resScale.value,
                                valueRange = 0.5f..4f,
                                steps = 6,
                                onValueChange = { it ->
                                    resScale.value = it
                                })
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable Texture Recompression",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(
                                    checked = enableTextureRecompression.value,
                                    onCheckedChange = {
                                        enableTextureRecompression.value =
                                            !enableTextureRecompression.value
                                    })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.Start,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                var isDriverSelectorOpen = remember {
                                    mutableStateOf(false)
                                }
                                var driverViewModel =
                                    VulkanDriverViewModel(settingsViewModel.activity)
                                var isChanged = remember {
                                    mutableStateOf(false)
                                }
                                var refresh = remember {
                                    mutableStateOf(false)
                                }
                                var drivers = driverViewModel.getAvailableDrivers()
                                var selectedDriver = remember {
                                    mutableStateOf(0)
                                }

                                if (refresh.value) {
                                    isChanged.value = true
                                    refresh.value = false
                                }

                                if (isDriverSelectorOpen.value) {
                                    AlertDialog(onDismissRequest = {
                                        isDriverSelectorOpen.value = false

                                        if (isChanged.value) {
                                            driverViewModel.saveSelected()
                                        }
                                    }) {
                                        Column {
                                            Surface(
                                                modifier = Modifier
                                                    .wrapContentWidth()
                                                    .wrapContentHeight(),
                                                shape = MaterialTheme.shapes.large,
                                                tonalElevation = AlertDialogDefaults.TonalElevation
                                            ) {
                                                if (!isChanged.value) {
                                                    selectedDriver.value =
                                                        drivers.indexOfFirst { it.driverPath == driverViewModel.selected } + 1
                                                    isChanged.value = true
                                                }
                                                Column {
                                                    Column(
                                                        modifier = Modifier
                                                            .fillMaxWidth()
                                                            .height(350.dp)
                                                            .verticalScroll(rememberScrollState())
                                                    ) {
                                                        Row(
                                                            modifier = Modifier
                                                                .fillMaxWidth()
                                                                .padding(8.dp),
                                                            verticalAlignment = Alignment.CenterVertically
                                                        ) {
                                                            RadioButton(
                                                                selected = selectedDriver.value == 0 || driverViewModel.selected.isEmpty(),
                                                                onClick = {
                                                                    selectedDriver.value = 0
                                                                    isChanged.value = true
                                                                    driverViewModel.selected = ""
                                                                })
                                                            Column {
                                                                Text(text = "Default",
                                                                    modifier = Modifier
                                                                        .fillMaxWidth()
                                                                        .clickable {
                                                                            selectedDriver.value = 0
                                                                            isChanged.value = true
                                                                            driverViewModel.selected =
                                                                                ""
                                                                        })
                                                            }
                                                        }
                                                        var driverIndex = 1
                                                        for (driver in drivers) {
                                                            var ind = driverIndex
                                                            Row(
                                                                modifier = Modifier
                                                                    .fillMaxWidth()
                                                                    .padding(4.dp),
                                                                verticalAlignment = Alignment.CenterVertically
                                                            ) {
                                                                RadioButton(
                                                                    selected = selectedDriver.value == ind,
                                                                    onClick = {
                                                                        selectedDriver.value = ind
                                                                        isChanged.value = true
                                                                        driverViewModel.selected =
                                                                            driver.driverPath
                                                                    })
                                                                Column(modifier = Modifier.clickable {
                                                                    selectedDriver.value =
                                                                        ind
                                                                    isChanged.value =
                                                                        true
                                                                    driverViewModel.selected =
                                                                        driver.driverPath
                                                                }) {
                                                                    Text(
                                                                        text = driver.libraryName,
                                                                        modifier = Modifier
                                                                            .fillMaxWidth()
                                                                    )
                                                                    Text(
                                                                        text = driver.driverVersion,
                                                                        modifier = Modifier
                                                                            .fillMaxWidth()
                                                                    )
                                                                    Text(
                                                                        text = driver.description,
                                                                        modifier = Modifier
                                                                            .fillMaxWidth()
                                                                    )
                                                                }
                                                            }

                                                            driverIndex++
                                                        }
                                                    }
                                                    Row(
                                                        horizontalArrangement = Arrangement.End,
                                                        modifier = Modifier
                                                            .fillMaxWidth()
                                                            .padding(16.dp)
                                                    ) {
                                                        Button(onClick = {
                                                            driverViewModel.removeSelected()
                                                            refresh.value = true
                                                        }, modifier = Modifier.padding(8.dp)) {
                                                            Text(text = "Remove")
                                                        }

                                                        Button(onClick = {
                                                            driverViewModel.add(refresh)
                                                            refresh.value = true
                                                        }, modifier = Modifier.padding(8.dp)) {
                                                            Text(text = "Add")
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                TextButton(
                                    {
                                        isChanged.value = false
                                        isDriverSelectorOpen.value = !isDriverSelectorOpen.value
                                    },
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                ) {
                                    Text(text = "Drivers")
                                }
                            }

                        }
                    }
                    ExpandableView(onCardArrowClick = { }, title = "Input") {
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Show virtual controller",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = useVirtualController.value, onCheckedChange = {
                                    useVirtualController.value = !useVirtualController.value
                                })
                            }
                        }
                    }
                }

                BackHandler() {
                    settingsViewModel.save(
                        isHostMapped,
                        useNce, enableVsync, enableDocked, enablePtc, ignoreMissingServices,
                        enableShaderCache,
                        enableTextureRecompression,
                        resScale,
                        useVirtualController
                    )
                    settingsViewModel.navController.popBackStack()
                }
            }
        }

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        @SuppressLint("UnusedTransitionTargetStateParameter")
        fun ExpandableView(
            onCardArrowClick: () -> Unit,
            title: String,
            content: @Composable () -> Unit
        ) {
            val expanded = false
            val mutableExpanded = remember {
                mutableStateOf(expanded)
            }
            val transitionState = remember {
                MutableTransitionState(expanded).apply {
                    targetState = !mutableExpanded.value
                }
            }
            val transition = updateTransition(transitionState, label = "transition")
            val arrowRotationDegree by transition.animateFloat({
                tween(durationMillis = EXPANSTION_TRANSITION_DURATION)
            }, label = "rotationDegreeTransition") {
                if (mutableExpanded.value) 0f else 180f
            }

            Card(
                shape = MaterialTheme.shapes.medium,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(
                        horizontal = 24.dp,
                        vertical = 8.dp
                    )
            ) {
                Column {
                    Card(
                        onClick = {
                            mutableExpanded.value = !mutableExpanded.value
                            onCardArrowClick()
                        }) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            CardTitle(title = title)
                            CardArrow(
                                degrees = arrowRotationDegree,
                            )

                        }
                    }
                    ExpandableContent(visible = mutableExpanded.value, content = content)
                }
            }
        }

        @Composable
        fun CardArrow(
            degrees: Float,
        ) {
            Icon(
                Icons.Filled.KeyboardArrowUp,
                contentDescription = "Expandable Arrow",
                modifier = Modifier
                    .padding(8.dp)
                    .rotate(degrees),
            )
        }

        @Composable
        fun CardTitle(title: String) {
            Text(
                text = title,
                modifier = Modifier
                    .padding(16.dp),
                textAlign = TextAlign.Center,
            )
        }

        @Composable
        fun ExpandableContent(
            visible: Boolean = true,
            content: @Composable () -> Unit
        ) {
            val enterTransition = remember {
                expandVertically(
                    expandFrom = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeIn(
                    initialAlpha = 0.3f,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                )
            }
            val exitTransition = remember {
                shrinkVertically(
                    // Expand from the top.
                    shrinkTowards = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeOut(
                    // Fade in with the initial alpha of 0.3f.
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                )
            }

            AnimatedVisibility(
                visible = visible,
                enter = enterTransition,
                exit = exitTransition
            ) {
                Column(modifier = Modifier.padding(8.dp)) {
                    content()
                }
            }
        }
    }
}
