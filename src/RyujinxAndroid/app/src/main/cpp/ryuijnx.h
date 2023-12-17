//
// Created by Emmanuel Hansen on 6/19/2023.
//

#ifndef RYUJINXNATIVE_RYUIJNX_H
#define RYUJINXNATIVE_RYUIJNX_H
#include <stdlib.h>
#include <dlfcn.h>
#include <string.h>
#include <string>
#include <jni.h>
#include <exception>
#include <android/log.h>
#include <android/native_window.h>
#include <android/native_window_jni.h>
#include "vulkan_wrapper.h"
#include <vulkan/vulkan_android.h>
#include <cassert>
#include <fcntl.h>
#include "adrenotools/driver.h"
#include "native_window.h"
#include "string_helper.h"

// A macro to pass call to Vulkan and check for return value for success
#define CALL_VK(func)                                                 \
  if (VK_SUCCESS != (func)) {                                         \
    __android_log_print(ANDROID_LOG_ERROR, "Tutorial ",               \
                        "Vulkan error. File[%s], line[%d]", __FILE__, \
                        __LINE__);                                    \
    assert(false);                                                    \
  }

// A macro to check value is VK_SUCCESS
// Used also for non-vulkan functions but return VK_SUCCESS
#define VK_CHECK(x)  CALL_VK(x)

#define LoadLib(a) dlopen(a, RTLD_NOW)

void* _ryujinxNative = NULL;

class UiHandler {
public:
    void setTitle(long storedTitle);
    void setMessage(long storedMessage);
    void setWatermark(long wm);
    void setType(int t);
    void setMode(int t);
    void setMinLength(int t);
    void setMaxLength(int t);
    void setInitialText(long text);
    void setSubtitle(long text);

    long getTitle();
    long getMessage();
    long getWatermark();
    long getInitialText();
    long getSubtitle();
    int type = 0;
    int keyboardMode = 0;
    int min_length = -1;
    int max_length = -1;

private:
    long title = -1;
    long message = -1;
    long watermark = -1;
    long initialText = -1;
    long subtitle = -1;
};

// Ryujinx imported functions
bool (*initialize)(char*) = NULL;

long _renderingThreadId = 0;
long _currentRenderingThreadId = 0;
JavaVM* _vm = nullptr;
jobject _mainActivity = nullptr;
jclass _mainActivityClass = nullptr;
string_helper str_helper = string_helper();
UiHandler ui_handler = UiHandler();

#endif //RYUJINXNATIVE_RYUIJNX_H
