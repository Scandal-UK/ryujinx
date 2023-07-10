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

JNIEXPORT jlong JNICALL
Java_org_ryujinx_android_NativeHelpers_createSurface(
        JNIEnv *env,
        jobject instance,
        jlong vulkanInstance,
        jlong window) {
    auto nativeWindow = (ANativeWindow *) window;

    if (nativeWindow != NULL)
        return -1;
    VkSurfaceKHR surface;
    auto vkInstance = VkInstance(vulkanInstance);
    auto fpCreateAndroidSurfaceKHR =
            reinterpret_cast<PFN_vkCreateAndroidSurfaceKHR>(vkGetInstanceProcAddr(vkInstance, "vkCreateAndroidSurfaceKHR"));
    if (!fpCreateAndroidSurfaceKHR)
        return -1;
    VkAndroidSurfaceCreateInfoKHR info = { VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR };
    info.window = nativeWindow;
    VK_CHECK(fpCreateAndroidSurfaceKHR(vkInstance, &info, nullptr, &surface));
    return (jlong)surface;
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


}