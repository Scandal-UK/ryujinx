package org.ryujinx.android.views

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import org.ryujinx.android.viewmodels.TitleUpdateViewModel
import java.io.File

class TitleUpdateViews {
    companion object {
        @Composable
        fun Main(titleId: String, name: String, openDialog: MutableState<Boolean>, canClose: MutableState<Boolean>) {
            val viewModel = TitleUpdateViewModel(titleId)

            val selected = remember { mutableStateOf(0) }
            viewModel.data?.apply {
                selected.value = paths.indexOf(this.selected) + 1
            }

            Column(modifier = Modifier.padding(16.dp)) {
                val isCopying = remember {
                    mutableStateOf(false)
                }
                val copyProgress = remember {
                    mutableStateOf(0.0f)
                }
                var currentProgressName = remember {
                    mutableStateOf("Starting Copy")
                }
                Column {
                    Text(text = "Updates for ${name}", textAlign = TextAlign.Center)
                    Surface(
                        modifier = Modifier
                            .padding(8.dp),
                        color = MaterialTheme.colorScheme.surfaceVariant,
                        shape = MaterialTheme.shapes.medium
                    ) {
                        Column(
                            modifier = Modifier
                                .height(250.dp)
                                .fillMaxWidth()
                                .verticalScroll(rememberScrollState())
                        ) {
                            Row(modifier = Modifier.padding(8.dp)) {
                                RadioButton(
                                    selected = (selected.value == 0),
                                    onClick = {
                                        selected.value = 0
                                    })
                                Text(
                                    text = "None",
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .align(Alignment.CenterVertically)
                                )
                            }

                            val paths = remember {
                                mutableStateListOf<String>()
                            }

                            viewModel.setPaths(paths, canClose)
                            var index = 1
                            for (path in paths) {
                                val i = index
                                Row(modifier = Modifier.padding(8.dp)) {
                                    RadioButton(
                                        selected = (selected.value == i),
                                        onClick = { selected.value = i })
                                    Text(
                                        text = File(path).name,
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .align(Alignment.CenterVertically)
                                    )
                                }

                                index++
                            }
                        }
                    }
                    Row(modifier = Modifier.align(Alignment.End)) {
                        IconButton(
                            onClick = {
                                viewModel.Remove(selected.value)
                            }
                        ) {
                            Icon(
                                Icons.Filled.Delete,
                                contentDescription = "Remove"
                            )
                        }

                        IconButton(
                            onClick = {
                                viewModel.Add(isCopying, copyProgress, currentProgressName)
                            }
                        ) {
                            Icon(
                                Icons.Filled.Add,
                                contentDescription = "Add"
                            )
                        }
                    }

                }
                if (isCopying.value) {
                    Text(text = "Copying updates to local storage")
                    Text(text = currentProgressName.value)
                    Row {
                        LinearProgressIndicator(
                            modifier = Modifier.fillMaxWidth(),
                            progress = copyProgress.value
                        )
                        TextButton(
                            onClick = {
                                isCopying.value = false
                                canClose.value = true
                                viewModel.refreshPaths()
                            },
                        ) {
                            Text("Cancel")
                        }
                    }
                }
                Spacer(modifier = Modifier.height(18.dp))
                TextButton(
                    modifier = Modifier.align(Alignment.End),
                    onClick = {
                        if (!isCopying.value) {
                            canClose.value = true
                            viewModel.save(selected.value, openDialog)
                        }
                    },
                ) {
                    Text("Save")
                }
            }
        }
    }
}
