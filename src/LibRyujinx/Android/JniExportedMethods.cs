using LibRyujinx.Jni;
using LibRyujinx.Jni.Pointers;
using LibRyujinx.Jni.Primitives;
using LibRyujinx.Jni.References;
using LibRyujinx.Jni.Values;
using LibRyujinx.Shared.Audio.Oboe;
using Microsoft.Win32.SafeHandles;
using Rxmxnx.PInvoke;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Ryujinx.Common.SystemInfo;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.Input;
using Silk.NET.Core.Loader;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        private static ManualResetEvent _surfaceEvent;
        private static long _surfacePtr;
        private static long _window = 0;

        public static VulkanLoader? VulkanLoader { get; private set; }

        [DllImport("libryujinxjni")]
        private extern static IntPtr getStringPointer(JEnvRef jEnv, JStringLocalRef s);

        [DllImport("libryujinxjni")]
        private extern static JStringLocalRef createString(JEnvRef jEnv, IntPtr ch);

        [DllImport("libryujinxjni")]
        internal extern static void setRenderingThread();

        [DllImport("libryujinxjni")]
        internal extern static void debug_break(int code);

        [DllImport("libryujinxjni")]
        internal extern static void onFrameEnd(double time);

        [DllImport("libryujinxjni")]
        internal extern static void setCurrentTransform(long native_window, int transform);

        public delegate IntPtr JniCreateSurface(IntPtr native_surface, IntPtr instance);

        [UnmanagedCallersOnly(EntryPoint = "JNI_OnLoad")]
        internal static int LoadLibrary(JavaVMRef vm, IntPtr unknown)
        {
            return 0x00010006; //JNI_VERSION_1_6
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_initialize")]
        public static JBoolean JniInitialize(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef jpath, JBoolean enableDebugLogs)
        {
            SystemInfo.IsBionic = true;

            Logger.AddTarget(
                new AsyncLogTargetWrapper(
                    new AndroidLogTarget("Ryujinx"),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));

            var path = GetString(jEnv, jpath);

            var init = Initialize(path, enableDebugLogs);

            _surfaceEvent?.Set();

            _surfaceEvent = new ManualResetEvent(false);

            return init;
        }

        private static string? GetString(JEnvRef jEnv, JStringLocalRef jString)
        {
            var stringPtr = getStringPointer(jEnv, jString);

            var s = Marshal.PtrToStringAnsi(stringPtr);
            Marshal.FreeHGlobal(stringPtr);
            return s;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceInitialize")]
        public static JBoolean JniInitializeDeviceNative(JEnvRef jEnv,
                                                         JObjectLocalRef jObj,
                                                         JBoolean isHostMapped,
                                                         JBoolean useNce,
                                                         JInt systemLanguage,
                                                         JInt regionCode,
                                                         JBoolean enableVsync,
                                                         JBoolean enableDockedMode,
                                                         JBoolean enablePtc,
                                                         JBoolean enableInternetAccess,
                                                         JStringLocalRef timeZone,
                                                         JBoolean ignoreMissingServices)
        {
            AudioDriver = new OboeHardwareDeviceDriver();
            return InitializeDevice(isHostMapped,
                                    useNce,
                                    (SystemLanguage)(int)systemLanguage,
                                    (RegionCode)(int)regionCode,
                                    enableVsync,
                                    enableDockedMode,
                                    enablePtc,
                                    enableInternetAccess,
                                    GetString(jEnv, timeZone),
                                    ignoreMissingServices);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFifo")]
        public static JDouble JniGetGameFifo(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            var stats = SwitchDevice.EmulationContext?.Statistics.GetFifoPercent() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFrameTime")]
        public static JDouble JniGetGameFrameTime(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            var stats = SwitchDevice.EmulationContext?.Statistics.GetGameFrameTime() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFrameRate")]
        public static JDouble JniGetGameFrameRate(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            var stats = SwitchDevice.EmulationContext?.Statistics.GetGameFrameRate() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLoad")]
        public static JBoolean JniLoadApplicationNative(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef pathPtr)
        {
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var path = GetString(jEnv, pathPtr);

            return LoadApplication(path);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetDlcContentList")]
        public static JArrayLocalRef JniGetDlcContentListNative(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef pathPtr, JLong titleId)
        {
            var list = GetDlcContentList(GetString(jEnv, pathPtr), (ulong)(long)titleId);

            debug_break(4);

            return CreateStringArray(jEnv, list);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetDlcTitleId")]
        public static JStringLocalRef JniGetDlcTitleIdNative(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef pathPtr, JStringLocalRef ncaPath)
        {
            return CreateString(jEnv, GetDlcTitleId(GetString(jEnv, pathPtr), GetString(jEnv, ncaPath)));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceSignalEmulationClose")]
        public static void JniSignalEmulationCloseNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            SignalEmulationClose();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceCloseEmulation")]
        public static void JniCloseEmulationNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            CloseEmulation();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLoadDescriptor")]
        public static JBoolean JniLoadApplicationNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt descriptor, JBoolean isXci)
        {
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var stream = OpenFile(descriptor);

            return LoadApplication(stream, isXci);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitialize")]
        public static JBoolean JniInitializeGraphicsNative(JEnvRef jEnv, JObjectLocalRef jObj, JObjectLocalRef graphicObject)
        {
            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr getObjectClassPtr = jInterface.GetObjectClassPointer;
            IntPtr getFieldIdPtr = jInterface.GetFieldIdPointer;
            IntPtr getIntFieldPtr = jInterface.GetIntFieldPointer;
            IntPtr getLongFieldPtr = jInterface.GetLongFieldPointer;
            IntPtr getFloatFieldPtr = jInterface.GetFloatFieldPointer;
            IntPtr getBooleanFieldPtr = jInterface.GetBooleanFieldPointer;

            var getObjectClass = getObjectClassPtr.GetUnsafeDelegate<GetObjectClassDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getLongField = getLongFieldPtr.GetUnsafeDelegate<GetLongFieldDelegate>();
            var getIntField = getIntFieldPtr.GetUnsafeDelegate<GetIntFieldDelegate>();
            var getBooleanField = getBooleanFieldPtr.GetUnsafeDelegate<GetBooleanFieldDelegate>();
            var getFloatField = getFloatFieldPtr.GetUnsafeDelegate<GetFloatFieldDelegate>();

            var jobject = getObjectClass(jEnv, graphicObject);

            GraphicsConfiguration graphicsConfiguration = new()
            {
                EnableShaderCache = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableShaderCache"), GetCCharSequence("Z"))),
                EnableMacroHLE = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableMacroHLE"), GetCCharSequence("Z"))),
                EnableMacroJit = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableMacroJit"), GetCCharSequence("Z"))),
                EnableTextureRecompression = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableTextureRecompression"), GetCCharSequence("Z"))),
                Fast2DCopy = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("Fast2DCopy"), GetCCharSequence("Z"))),
                FastGpuTime = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("FastGpuTime"), GetCCharSequence("Z"))),
                ResScale = getFloatField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("ResScale"), GetCCharSequence("F"))),
                MaxAnisotropy = getFloatField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("MaxAnisotropy"), GetCCharSequence("F"))),
                BackendThreading = (BackendThreading)(int)getIntField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("BackendThreading"), GetCCharSequence("I")))
            };
            SearchPathContainer.Platform = UnderlyingPlatform.Android;
            return InitializeGraphics(graphicsConfiguration);
        }

        private static CCharSequence GetCCharSequence(string s)
        {
            return Encoding.UTF8.GetBytes(s).AsSpan();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsSetSurface")]
        public static void JniSetSurface(JEnvRef jEnv, JObjectLocalRef jObj, JLong surfacePtr, JLong window)
        {
            _surfacePtr = surfacePtr;
            _window = window;

            _surfaceEvent.Set();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitializeRenderer")]
        public unsafe static JBoolean JniInitializeGraphicsRendererNative(JEnvRef jEnv,
                                                                          JObjectLocalRef jObj,
                                                                          JArrayLocalRef extensionsArray,
                                                                          JLong driverHandle)
        {
            if (Renderer != null)
            {
                return false;
            }

            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr getObjectClassPtr = jInterface.GetObjectClassPointer;
            IntPtr getFieldIdPtr = jInterface.GetFieldIdPointer;
            IntPtr getLongFieldPtr = jInterface.GetLongFieldPointer;
            IntPtr getArrayLengthPtr = jInterface.GetArrayLengthPointer;
            IntPtr getObjectArrayElementPtr = jInterface.GetObjectArrayElementPointer;
            IntPtr getObjectFieldPtr = jInterface.GetObjectFieldPointer;

            var getObjectClass = getObjectClassPtr.GetUnsafeDelegate<GetObjectClassDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getArrayLength = getArrayLengthPtr.GetUnsafeDelegate<GetArrayLengthDelegate>();
            var getObjectArrayElement = getObjectArrayElementPtr.GetUnsafeDelegate<GetObjectArrayElementDelegate>();
            var getLongField = getLongFieldPtr.GetUnsafeDelegate<GetLongFieldDelegate>();
            var getObjectField = getObjectFieldPtr.GetUnsafeDelegate<GetObjectFieldDelegate>();

            List<string?> extensions = new();

            var count = getArrayLength(jEnv, extensionsArray);

            for (int i = 0; i < count; i++)
            {
                var obj = getObjectArrayElement(jEnv, extensionsArray, i);
                var ext = obj.Transform<JObjectLocalRef, JStringLocalRef>();

                extensions.Add(GetString(jEnv, ext));
            }

            if((long)driverHandle != 0)
            {
                VulkanLoader = new VulkanLoader((IntPtr)(long)driverHandle);
            }

            CreateSurface createSurfaceFunc = instance =>
            {
                _surfaceEvent.WaitOne();
                _surfaceEvent.Reset();

                var api = VulkanLoader?.GetApi() ?? Vk.GetApi();
                if (api.TryGetInstanceExtension(new Instance(instance), out KhrAndroidSurface surfaceExtension))
                {
                    var createInfo = new AndroidSurfaceCreateInfoKHR
                    {
                        SType = StructureType.AndroidSurfaceCreateInfoKhr,
                        Window = (nint*)_surfacePtr,
                    };

                    var result = surfaceExtension.CreateAndroidSurface(new Instance(instance), createInfo, null, out var surface);

                    return (nint)surface.Handle;
                }

                return IntPtr.Zero;
            };

            return InitializeGraphicsRenderer(GraphicsBackend.Vulkan, createSurfaceFunc, extensions.ToArray());
        }

        private static JArrayLocalRef CreateStringArray(JEnvRef jEnv, List<string> strings)
        {
            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr newObjectArrayPtr = jInterface.NewObjectArrayPointer;
            IntPtr findClassPtr = jInterface.FindClassPointer;
            IntPtr setObjectArrayElementPtr = jInterface.SetObjectArrayElementPointer;

            var newObjectArray = newObjectArrayPtr.GetUnsafeDelegate<NewObjectArrayDelegate>();
            var findClass = findClassPtr.GetUnsafeDelegate<FindClassDelegate>();
            var setObjectArrayElement = setObjectArrayElementPtr.GetUnsafeDelegate<SetObjectArrayElementDelegate>();
            var array = newObjectArray(jEnv, strings.Count, findClass(jEnv, GetCCharSequence("java/lang/String")), CreateString(jEnv, "")._value);

            for (int i = 0; i < strings.Count; i++)
            {
                setObjectArrayElement(jEnv, array, i, CreateString(jEnv, strings[i])._value);
            }

            return array;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSize")]
        public static void JniSetRendererSizeNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            Renderer?.Window?.SetSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererRunLoop")]
        public static void JniRunLoopNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            SetSwapBuffersCallback(() =>
            {
                var time = SwitchDevice.EmulationContext.Statistics.GetGameFrameTime();
                onFrameEnd(time);
            });
            RunLoop();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameInfoFromPath")]
        public static JObjectLocalRef JniGetGameInfo(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef path)
        {
            var info = GetGameInfo(GetString(jEnv, path));
            return GetInfo(jEnv, info, out SHA256 _);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameInfo")]
        public static JObjectLocalRef JniGetGameInfo(JEnvRef jEnv, JObjectLocalRef jObj, JInt fileDescriptor, JBoolean isXci)
        {
            using var stream = OpenFile(fileDescriptor);

            var info = GetGameInfo(stream, isXci);
            return GetInfo(jEnv, info, out SHA256 _);
        }

        private static JObjectLocalRef GetInfo(JEnvRef jEnv, GameInfo? info, out SHA256 sha)
        {
            var javaClassName = GetCCharSequence("org/ryujinx/android/viewmodels/GameInfo");

            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr findClassPtr = jInterface.FindClassPointer;
            IntPtr newGlobalRefPtr = jInterface.NewGlobalRefPointer;
            IntPtr getFieldIdPtr = jInterface.GetFieldIdPointer;
            IntPtr getMethodPtr = jInterface.GetMethodIdPointer;
            IntPtr newObjectPtr = jInterface.NewObjectPointer;
            IntPtr setObjectFieldPtr = jInterface.SetObjectFieldPointer;
            IntPtr setDoubleFieldPtr = jInterface.SetDoubleFieldPointer;


            var findClass = findClassPtr.GetUnsafeDelegate<FindClassDelegate>();
            var newGlobalRef = newGlobalRefPtr.GetUnsafeDelegate<NewGlobalRefDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getMethod = getMethodPtr.GetUnsafeDelegate<GetMethodIdDelegate>();
            var newObject = newObjectPtr.GetUnsafeDelegate<NewObjectDelegate>();
            var setObjectField = setObjectFieldPtr.GetUnsafeDelegate<SetObjectFieldDelegate>();
            var setDoubleField = setDoubleFieldPtr.GetUnsafeDelegate<SetDoubleFieldDelegate>();

            var javaClass = findClass(jEnv, javaClassName);
            var newGlobal = newGlobalRef(jEnv, javaClass._value);
            var constructor = getMethod(jEnv, javaClass, GetCCharSequence("<init>"), GetCCharSequence("()V"));
            var newObj = newObject(jEnv, javaClass, constructor, 0);
            sha = SHA256.Create();
            var iconCacheByte = sha.ComputeHash(info?.Icon ?? Array.Empty<byte>());
            var iconCache = BitConverter.ToString(iconCacheByte).Replace("-", "");

            var cacheDirectory = Path.Combine(AppDataManager.BaseDirPath, "iconCache");
            Directory.CreateDirectory(cacheDirectory);

            var cachePath = Path.Combine(cacheDirectory, iconCache);
            if (!File.Exists(cachePath))
            {
                File.WriteAllBytes(cachePath, info?.Icon ?? Array.Empty<byte>());
            }

            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleName"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.TitleName)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleId"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.TitleId)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Developer"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.Developer)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Version"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.Version)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("IconCache"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, iconCache)._value);
            setDoubleField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("FileSize"), GetCCharSequence("D")), info?.FileSize ?? 0d);

            return newObj;
        }

        private static JStringLocalRef CreateString(JEnvRef jEnv, string? s)
        {
            s ??= string.Empty;

            var ptr = Marshal.StringToHGlobalAnsi(s);

            var str = createString(jEnv, ptr);

            Marshal.FreeHGlobal(ptr);

            return str;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetVsync")]
        public static void JniSetVsyncStateNative(JEnvRef jEnv, JObjectLocalRef jObj, JBoolean enabled)
        {
            SetVsyncState(enabled);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSwapBufferCallback")]
        public static void JniSetSwapBuffersCallbackNative(JEnvRef jEnv, JObjectLocalRef jObj, IntPtr swapBuffersCallback)
        {
            _swapBuffersCallback = Marshal.GetDelegateForFunctionPointer<SwapBuffersCallback>(swapBuffersCallback);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputInitialize")]
        public static void JniInitializeInput(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            InitializeInput(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetClientSize")]
        public static void JniSetClientSize(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            SetClientSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetTouchPoint")]
        public static void JniSetTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj, JInt x, JInt y)
        {
            SetTouchPoint(x, y);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputReleaseTouchPoint")]
        public static void JniReleaseTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            ReleaseTouchPoint();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputUpdate")]
        public static void JniUpdateInput(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            UpdateInput();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonPressed")]
        public static void JniSetButtonPressed(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JInt id)
        {
            SetButtonPressed((GamepadButtonInputId)(int)button, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonReleased")]
        public static void JniSetButtonReleased(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JInt id)
        {
            SetButtonReleased((GamepadButtonInputId)(int)button, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetStickAxis")]
        public static void JniSetStickAxis(JEnvRef jEnv, JObjectLocalRef jObj, JInt stick, JFloat x, JFloat y, JInt id)
        {
            SetStickAxis((StickInputId)(int)stick, new Vector2(x, y), id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputConnectGamepad")]
        public static JInt JniConnectGamepad(JEnvRef jEnv, JObjectLocalRef jObj, JInt index)
        {
            return ConnectGamepad(index);
        }

        private static FileStream OpenFile(int descriptor)
        {
            var safeHandle = new SafeFileHandle(descriptor, false);

            return new FileStream(safeHandle, FileAccess.ReadWrite);
        }
    }

    internal static partial class Logcat
    {
        [LibraryImport("liblog", StringMarshalling = StringMarshalling.Utf8)]
        private static partial void __android_log_print(LogLevel level, string? tag, string format, string args, IntPtr ptr);

        internal static void AndroidLogPrint(LogLevel level, string? tag, string message) =>
            __android_log_print(level, tag, "%s", message, IntPtr.Zero);

        internal enum LogLevel
        {
            Unknown = 0x00,
            Default = 0x01,
            Verbose = 0x02,
            Debug = 0x03,
            Info = 0x04,
            Warn = 0x05,
            Error = 0x06,
            Fatal = 0x07,
            Silent = 0x08,
        }
    }
}
