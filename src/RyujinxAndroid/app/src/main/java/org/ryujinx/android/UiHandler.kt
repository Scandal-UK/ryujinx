package org.ryujinx.android

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.DialogProperties
import com.halilibo.richtext.markdown.Markdown
import com.halilibo.richtext.ui.material3.RichText

internal enum class KeyboardMode {
    Default, Numeric, ASCII, FullLatin, Alphabet, SimplifiedChinese, TraditionalChinese, Korean, LanguageSet2, LanguageSet2Latin
}

class UiHandler {
    private var initialText: String = ""
    private var subtitle: String = ""
    private var maxLength: Int = 0
    private var minLength: Int = 0
    private var watermark: String = ""
    private var type: Int = -1
    private var mode: KeyboardMode = KeyboardMode.Default
    val showMessage = mutableStateOf(false)
    val inputText = mutableStateOf("")
    var title: String = ""
    var message: String = ""
    var shouldListen = true

    init {
        RyujinxNative.instance.uiHandlerSetup()
    }

    fun listen() {
        showMessage.value = false
        while (shouldListen) {
            RyujinxNative.instance.uiHandlerWait()

            title =
                NativeHelpers.instance.getStringJava(NativeHelpers.instance.getUiHandlerRequestTitle())
            message =
                NativeHelpers.instance.getStringJava(NativeHelpers.instance.getUiHandlerRequestMessage())
            watermark =
                NativeHelpers.instance.getStringJava(NativeHelpers.instance.getUiHandlerRequestWatermark())
            type = NativeHelpers.instance.getUiHandlerRequestType()
            minLength = NativeHelpers.instance.getUiHandlerMinLength()
            maxLength = NativeHelpers.instance.getUiHandlerMaxLength()
            mode = KeyboardMode.values()[NativeHelpers.instance.getUiHandlerKeyboardMode()]
            subtitle =
                NativeHelpers.instance.getStringJava(NativeHelpers.instance.getUiHandlerRequestSubtitle())
            initialText =
                NativeHelpers.instance.getStringJava(NativeHelpers.instance.getUiHandlerRequestInitialText())
            inputText.value = initialText
            showMessage.value = type > 0
        }
    }

    fun stop() {
        shouldListen = false
        RyujinxNative.instance.uiHandlerStopWait()
    }

    @OptIn(ExperimentalMaterial3Api::class)
    @Composable
    fun Compose() {
        val showMessageListener = remember {
            showMessage
        }

        val inputListener = remember {
            inputText
        }
        val validation = remember {
            mutableStateOf("")
        }

        fun validate() : Boolean{
            if(inputText.value.isEmpty()){
                validation.value = "Must be between ${minLength} and ${maxLength} characters"
            }
            else{
                return inputText.value.length < minLength || inputText.value.length > maxLength
            }

            return false;
        }

        fun getInputType(): KeyboardType {
            return when(mode){
                KeyboardMode.Default -> KeyboardType.Text
                KeyboardMode.Numeric ->  KeyboardType.Decimal
                KeyboardMode.ASCII ->  KeyboardType.Ascii
                else -> { KeyboardType.Text}
            }
        }

        fun submit() {
            var input: Long = -1
            if (type == 2) {
                if (inputListener.value.length < minLength || inputListener.value.length > maxLength)
                    return
                input =
                    NativeHelpers.instance.storeStringJava(inputListener.value)
            }
            showMessageListener.value = false
            RyujinxNative.instance.uiHandlerSetResponse(true, input)
        }

        if (showMessageListener.value) {
            AlertDialog(
                modifier = Modifier
                    .wrapContentWidth()
                    .wrapContentHeight(),
                onDismissRequest = { },
                properties = DialogProperties(dismissOnBackPress = false, false)
            ) {
                Column {
                    Surface(
                        modifier = Modifier
                            .wrapContentWidth()
                            .wrapContentHeight(),
                        shape = MaterialTheme.shapes.large,
                        tonalElevation = AlertDialogDefaults.TonalElevation
                    ) {
                        Column(
                            modifier = Modifier
                                .padding(16.dp)
                        ) {
                            Text(text = title)
                            Column(
                                modifier = Modifier
                                    .height(128.dp)
                                    .verticalScroll(rememberScrollState())
                                    .padding(8.dp),
                                verticalArrangement = Arrangement.Center
                            ) {
                                RichText {
                                    Markdown(content = message)
                                }
                                if (type == 2) {
                                    validate()
                                    if (watermark.isNotEmpty())
                                        TextField(
                                            value = inputListener.value,
                                            onValueChange = { inputListener.value = it },
                                            modifier = Modifier
                                                .fillMaxWidth()
                                                .padding(4.dp),
                                            label = {
                                                Text(text = watermark)
                                            },
                                            keyboardOptions = KeyboardOptions(keyboardType = getInputType()),
                                            isError = validate()
                                        )
                                    else
                                        TextField(
                                            value = inputListener.value,
                                            onValueChange = { inputListener.value = it },
                                            modifier = Modifier
                                                .fillMaxWidth()
                                                .padding(4.dp),
                                            keyboardOptions = KeyboardOptions(
                                                keyboardType = getInputType(),
                                                imeAction = ImeAction.Done
                                            ),
                                            isError = validate(),
                                            singleLine = true,
                                            keyboardActions = KeyboardActions(onDone = { submit() })
                                        )
                                    if (subtitle.isNotEmpty())
                                        Text(text = subtitle)
                                    Text(text = validation.value)
                                }
                            }
                            Row(
                                horizontalArrangement = Arrangement.End,
                                modifier = Modifier
                                    .fillMaxWidth()
                            ) {
                                Button(onClick = {
                                    submit()
                                }) {
                                    Text(text = "OK")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

