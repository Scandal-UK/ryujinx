// Write C++ code here.
//
// Do not forget to dynamically load the C++ library into your application.
//
// For instance,
//
// In MainActivity.java:
//    static {
//       System.loadLibrary("ryuijnx");
//    }
//
// Or, in MainActivity.kt:
//    companion object {
//      init {
//         System.loadLibrary("ryuijnx")
//      }
//    }

#include "ryuijnx.h"
#include "pthread.h"
#include <chrono>
#include <csignal>

jmethodID _updateFrameTime;
JNIEnv* _rendererEnv = nullptr;

std::chrono::time_point<std::chrono::steady_clock, std::chrono::nanoseconds> _currentTimePoint;

std::string progressInfo = "";
float progress = -1;

JNIEnv* getEnv(bool isRenderer){
    JNIEnv* env;
    if(isRenderer){
        env = _rendererEnv;
    }

    if(env != nullptr)
        return env;

    auto result = _vm->AttachCurrentThread(&env, NULL);

    return env;
}

void detachEnv(){
    auto result = _vm->DetachCurrentThread();
}

extern "C"
{
    JNIEXPORT jlong JNICALL
    Java_org_ryujinx_android_NativeHelpers_getNativeWindow(
            JNIEnv *env,
            jobject instance,
            jobject surface) {
        auto nativeWindow = ANativeWindow_fromSurface(env, surface);
        return nativeWindow == NULL ? -1 : (jlong) nativeWindow;
    }

    JNIEXPORT void JNICALL
    Java_org_ryujinx_android_NativeHelpers_releaseNativeWindow(
            JNIEnv *env,
            jobject instance,
            jlong window) {
        auto nativeWindow = (ANativeWindow *) window;

        if (nativeWindow != NULL)
            ANativeWindow_release(nativeWindow);
    }

JNIEXPORT void JNICALL
Java_org_ryujinx_android_NativeHelpers_attachCurrentThread(
        JNIEnv *env,
        jobject instance) {
        JavaVM* jvm = NULL;
        env->GetJavaVM(&jvm);

        if(jvm != NULL)
            jvm->AttachCurrentThread(&env, NULL);
}

JNIEXPORT void JNICALL
Java_org_ryujinx_android_NativeHelpers_detachCurrentThread(
        JNIEnv *env,
        jobject instance) {
    JavaVM* jvm = NULL;
    env->GetJavaVM(&jvm);

    if(jvm != NULL)
        jvm->DetachCurrentThread();
}

long createSurface(long native_surface, long instance)
{
    auto nativeWindow = (ANativeWindow *) native_surface;
    VkSurfaceKHR surface;
    auto vkInstance = (VkInstance)instance;
    auto fpCreateAndroidSurfaceKHR =
            reinterpret_cast<PFN_vkCreateAndroidSurfaceKHR>(vkGetInstanceProcAddr(vkInstance, "vkCreateAndroidSurfaceKHR"));
    if (!fpCreateAndroidSurfaceKHR)
        return -1;
    VkAndroidSurfaceCreateInfoKHR info = { VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR };
    info.window = nativeWindow;
    VK_CHECK(fpCreateAndroidSurfaceKHR(vkInstance, &info, nullptr, &surface));
    return (long)surface;
}

JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getCreateSurfacePtr(
        JNIEnv *env,
        jobject instance) {
    return (jlong)createSurface;
}

char* getStringPointer(
        JNIEnv *env,
        jstring jS) {
    const char *cparam = env->GetStringUTFChars(jS, 0);
    auto len = env->GetStringUTFLength(jS);
    char* s= new char[len];
    strcpy(s, cparam);
    env->ReleaseStringUTFChars(jS, cparam);

    return s;
}

jstring createString(
        JNIEnv *env,
        char* ch) {
    auto str = env->NewStringUTF(ch);

    return str;
}

jstring createStringFromStdString(
        JNIEnv *env,
        std::string s) {
    auto str = env->NewStringUTF(s.c_str());

    return str;
}


}
extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_MainActivity_getRenderingThreadId(JNIEnv *env, jobject thiz) {
    return _currentRenderingThreadId;
}
extern "C"
void setRenderingThread(){
    auto currentId = pthread_self();

    _currentRenderingThreadId = currentId;
    _renderingThreadId = currentId;

    _currentTimePoint = std::chrono::high_resolution_clock::now();
}
extern "C"
JNIEXPORT void JNICALL
Java_org_ryujinx_android_MainActivity_initVm(JNIEnv *env, jobject thiz) {
    JavaVM* vm = nullptr;
    auto success = env->GetJavaVM(&vm);
    _vm = vm;
    _mainActivity = thiz;
    _mainActivityClass = env->GetObjectClass(thiz);
}

extern "C"
void onFrameEnd(double time) {
    auto env = getEnv(true);
    auto cl = env->FindClass("org/ryujinx/android/MainActivity");
    _updateFrameTime = env->GetStaticMethodID(cl, "updateRenderSessionPerformance", "(J)V");

    auto now = std::chrono::high_resolution_clock::now();
    auto nano = std::chrono::duration_cast<std::chrono::nanoseconds>(
            now - _currentTimePoint).count();
    env->CallStaticVoidMethod(cl, _updateFrameTime,
                              nano);
}
extern "C"
void setProgressInfo(char* info, float progressValue) {
    progressInfo = std::string (info);
    progress = progressValue;
}

bool isInitialOrientationFlipped = true;

extern "C"
void setCurrentTransform(long native_window, int transform){
    if(native_window == 0 || native_window == -1)
        return;
    auto nativeWindow = (ANativeWindow *) native_window;

    auto nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;

    transform = transform >> 1;

    // transform is a valid VkSurfaceTransformFlagBitsKHR
    switch (transform) {
        case 0x1:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;
            break;
        case 0x2:
            nativeTransform =  ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_90;
            break;
        case 0x4:
            nativeTransform = isInitialOrientationFlipped ? ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY : ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_180;
            break;
        case 0x8:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_270;
            break;
        case 0x10:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_HORIZONTAL;
            break;
        case 0x20:
            nativeTransform = static_cast<ANativeWindowTransform>(
                    ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_HORIZONTAL |
                    ANATIVEWINDOW_TRANSFORM_ROTATE_90);
            break;
        case 0x40:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_VERTICAL;
            break;
        case 0x80:
            nativeTransform = static_cast<ANativeWindowTransform>(
                    ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_VERTICAL |
                    ANATIVEWINDOW_TRANSFORM_ROTATE_90);
            break;
        case 0x100:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;
            break;
    }

    nativeWindow->perform(nativeWindow, NATIVE_WINDOW_SET_BUFFERS_TRANSFORM, static_cast<int32_t>(nativeTransform));
}

extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_loadDriver(JNIEnv *env, jobject thiz,
                                                  jstring native_lib_path,
                                                  jstring private_apps_path,
                                                  jstring driver_name) {
    auto libPath = getStringPointer(env, native_lib_path);
    auto privateAppsPath = getStringPointer(env, private_apps_path);
    auto driverName = getStringPointer(env, driver_name);

    auto handle = adrenotools_open_libvulkan(
            RTLD_NOW,
            ADRENOTOOLS_DRIVER_CUSTOM,
            nullptr,
            libPath,
            privateAppsPath,
            driverName,
            nullptr,
            nullptr
            );

    delete libPath;
    delete privateAppsPath;
    delete driverName;

    return (jlong)handle;
}

extern "C"
void debug_break(int code){
    if(code >= 3)
    int r = 0;
}

extern "C"
JNIEXPORT void JNICALL
Java_org_ryujinx_android_NativeHelpers_setTurboMode(JNIEnv *env, jobject thiz, jboolean enable) {
    adrenotools_set_turbo(enable);
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getMaxSwapInterval(JNIEnv *env, jobject thiz,
                                                       jlong native_window) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->maxSwapInterval;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getMinSwapInterval(JNIEnv *env, jobject thiz,
                                                          jlong native_window) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->minSwapInterval;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_setSwapInterval(JNIEnv *env, jobject thiz,
                                                       jlong native_window, jint swap_interval) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->setSwapInterval(nativeWindow, swap_interval);
}

extern "C"
JNIEXPORT jfloat JNICALL
Java_org_ryujinx_android_NativeHelpers_getProgressValue(JNIEnv *env, jobject thiz) {
    return progress;
}

extern "C"
JNIEXPORT jstring JNICALL
Java_org_ryujinx_android_NativeHelpers_getProgressInfo(JNIEnv *env, jobject thiz) {
    return createStringFromStdString(env, progressInfo);
}

extern "C"
long storeString(char* str){
    return str_helper.store_cstring(str);
}

extern "C"
const char* getString(long id){
    auto str = str_helper.get_stored(id);
    auto cstr = (char*)::malloc(str.length() + 1);
    ::strcpy(cstr, str.c_str());
    return cstr;
}

extern "C"
{
void setUiHandlerTitle(long title) {
    ui_handler.setTitle(title);
}
void setUiHandlerMessage(long message) {
    ui_handler.setMessage(message);
}
void setUiHandlerWatermark(long wm) {
    ui_handler.setWatermark(wm);
}
void setUiHandlerType(int type) {
    ui_handler.setType(type);
}
void setUiHandlerKeyboardMode(int mode) {
    ui_handler.setMode(mode);
}
void setUiHandlerMinLength(int length) {
    ui_handler.setMinLength(length);
}
void setUiHandlerMaxLength(int length) {
    ui_handler.setMaxLength(length);
}
void setUiHandlerInitialText(long text) {
    ui_handler.setInitialText(text);
}
void setUiHandlerSubtitle(long text) {
    ui_handler.setSubtitle(text);
}
}

extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_storeStringJava(JNIEnv *env, jobject thiz, jstring string) {
    auto str = getStringPointer(env, string);
    return str_helper.store_cstring(str);
}

extern "C"
JNIEXPORT jstring JNICALL
Java_org_ryujinx_android_NativeHelpers_getStringJava(JNIEnv *env, jobject thiz, jlong id) {
    return createStringFromStdString(env, id > -1 ? str_helper.get_stored(id) : "");
}

extern "C"
JNIEXPORT void JNICALL
Java_org_ryujinx_android_NativeHelpers_setIsInitialOrientationFlipped(JNIEnv *env, jobject thiz,
                                                                      jboolean is_flipped) {
    isInitialOrientationFlipped = is_flipped;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestType(JNIEnv *env, jobject thiz) {
    return  ui_handler.type;
}
extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestTitle(JNIEnv *env, jobject thiz) {
    return ui_handler.getTitle();
}
extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestMessage(JNIEnv *env, jobject thiz) {
    return ui_handler.getMessage();
}


void UiHandler::setTitle(long storedTitle) {
    if(title != -1){
        str_helper.get_stored(title);
        title = -1;
    }

    title = storedTitle;
}

void UiHandler::setMessage(long storedMessage) {
    if(message != -1){
        str_helper.get_stored(message);
        message = -1;
    }

    message = storedMessage;
}

void UiHandler::setType(int t) {
    this->type = t;
}

long UiHandler::getTitle() {
    auto v = title;
    title = -1;
    return v;
}

long UiHandler::getMessage() {
    auto v = message;
    message = -1;
    return v;
}

void UiHandler::setWatermark(long wm) {
    if(watermark != -1){
        str_helper.get_stored(watermark);
        watermark = -1;
    }

    watermark = wm;
}

void UiHandler::setMinLength(int t) {
    this->min_length = t;
}

void UiHandler::setMaxLength(int t) {
    this->max_length = t;
}

long UiHandler::getWatermark() {
    auto v = watermark;
    watermark = -1;
    return v;
}

void UiHandler::setInitialText(long text) {
    if(initialText != -1){
        str_helper.get_stored(watermark);
        initialText = -1;
    }

    initialText = text;
}

void UiHandler::setSubtitle(long text) {
    if(subtitle != -1){
        str_helper.get_stored(subtitle);
        subtitle = -1;
    }

    subtitle = text;
}

long UiHandler::getInitialText() {
    auto v = initialText;
    initialText = -1;
    return v;
}

long UiHandler::getSubtitle() {
    auto v = subtitle;
    subtitle = -1;
    return v;
}

void UiHandler::setMode(int t) {
    keyboardMode = t;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerMinLength(JNIEnv *env, jobject thiz) {
    return  ui_handler.min_length;
}
extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerMaxLength(JNIEnv *env, jobject thiz) {
    return  ui_handler.max_length;
}

extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestWatermark(JNIEnv *env, jobject thiz) {
    return ui_handler.getWatermark();
}

extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestInitialText(JNIEnv *env, jobject thiz) {
    return ui_handler.getInitialText();
}
extern "C"
JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerRequestSubtitle(JNIEnv *env, jobject thiz) {
    return ui_handler.getSubtitle();
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_ryujinx_android_NativeHelpers_getUiHandlerKeyboardMode(JNIEnv *env, jobject thiz) {
    return ui_handler.keyboardMode;
}
