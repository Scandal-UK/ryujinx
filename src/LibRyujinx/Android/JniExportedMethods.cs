using LibRyujinx.Jni;
using LibRyujinx.Jni.Pointers;
using LibRyujinx.Jni.Primitives;
using LibRyujinx.Jni.References;
using LibRyujinx.Jni.Values;
using LibRyujinx.Shared.Audio.Oboe;
using Rxmxnx.PInvoke;
using Ryujinx.Audio.Backends.OpenAL;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.Input;
using Silk.NET.Core.Loader;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
        internal extern static long storeString(string ch);

        [DllImport("libryujinxjni")]
        internal extern static IntPtr getString(long id);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerTitle(long title);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerMessage(long message);
        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerWatermark(long watermark);
        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerInitialText(long text);
        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerSubtitle(long text);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerType(int type);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerKeyboardMode(int mode);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerMinLength(int lenght);

        [DllImport("libryujinxjni")]
        internal extern static long setUiHandlerMaxLength(int lenght);

        internal static string GetStoredString(long id)
        {
            var pointer = getString(id);
            if (pointer != IntPtr.Zero)
            {
                var str = Marshal.PtrToStringAnsi(pointer) ?? "";

                Marshal.FreeHGlobal(pointer);
                return str;
            }

            return "";
        }

        [DllImport("libryujinxjni")]
        internal extern static void setRenderingThread();

        [DllImport("libryujinxjni")]
        internal extern static void debug_break(int code);

        [DllImport("libryujinxjni")]
        internal extern static void onFrameEnd(double time);

        [DllImport("libryujinxjni")]
        internal extern static void setProgressInfo(IntPtr info, float progress);

        [DllImport("libryujinxjni")]
        internal extern static void setCurrentTransform(long native_window, int transform);

        public delegate IntPtr JniCreateSurface(IntPtr native_surface, IntPtr instance);

        [UnmanagedCallersOnly(EntryPoint = "JNI_OnLoad")]
        internal static int LoadLibrary(JavaVMRef vm, IntPtr unknown)
        {
            return 0x00010006; //JNI_VERSION_1_6
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_initialize")]
        public static JBoolean JniInitialize(JEnvRef jEnv, JObjectLocalRef jObj, JLong jpathId)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            PlatformInfo.IsBionic = true;

            Logger.AddTarget(
                new AsyncLogTargetWrapper(
                    new AndroidLogTarget("RyujinxLog"),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));

            var path = GetStoredString(jpathId);

            var init = Initialize(path);

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

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceReloadFilesystem")]
        public static void JniReloadFileSystem()
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SwitchDevice?.ReloadFileSystem();
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
                                                         JLong timeZoneId,
                                                         JBoolean ignoreMissingServices)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            AudioDriver = new OpenALHardwareDeviceDriver();//new OboeHardwareDeviceDriver();
            return InitializeDevice(isHostMapped,
                                    useNce,
                                    (SystemLanguage)(int)systemLanguage,
                                    (RegionCode)(int)regionCode,
                                    enableVsync,
                                    enableDockedMode,
                                    enablePtc,
                                    enableInternetAccess,
                                    GetStoredString(timeZoneId),
                                    ignoreMissingServices);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFifo")]
        public static JDouble JniGetGameFifo(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var stats = SwitchDevice.EmulationContext?.Statistics.GetFifoPercent() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFrameTime")]
        public static JDouble JniGetGameFrameTime(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var stats = SwitchDevice.EmulationContext?.Statistics.GetGameFrameTime() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameFrameRate")]
        public static JDouble JniGetGameFrameRate(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var stats = SwitchDevice.EmulationContext?.Statistics.GetGameFrameRate() ?? 0;

            return stats;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLoad")]
        public static JBoolean JniLoadApplicationNative(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef pathPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var path = GetString(jEnv, pathPtr);

            return LoadApplication(path);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLaunchMiiEditor")]
        public static JBoolean JniLaunchMiiEditApplet(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            return LaunchMiiEditApplet();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetDlcContentList")]
        public static JArrayLocalRef JniGetDlcContentListNative(JEnvRef jEnv, JObjectLocalRef jObj, JLong pathPtr, JLong titleId)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var list = GetDlcContentList(GetStoredString(pathPtr), (ulong)(long)titleId);

            debug_break(4);

            return CreateStringArray(jEnv, list);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetDlcTitleId")]
        public static JLong JniGetDlcTitleIdNative(JEnvRef jEnv, JObjectLocalRef jObj, JLong pathPtr, JLong ncaPath)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            return storeString(GetDlcTitleId(GetStoredString(pathPtr), GetStoredString(ncaPath)));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceSignalEmulationClose")]
        public static void JniSignalEmulationCloseNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SignalEmulationClose();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceCloseEmulation")]
        public static void JniCloseEmulationNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            CloseEmulation();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLoadDescriptor")]
        public static JBoolean JniLoadApplicationNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt descriptor, JInt type, JInt updateDescriptor)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var stream = OpenFile(descriptor);
            var update = updateDescriptor == -1 ? null : OpenFile(updateDescriptor);

            return LoadApplication(stream, (FileType)(int)type, update);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceVerifyFirmware")]
        public static JLong JniVerifyFirmware(JEnvRef jEnv, JObjectLocalRef jObj, JInt descriptor, JBoolean isXci)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");

            var stream = OpenFile(descriptor);

            long stringHandle = -1;

            try
            {
                var version = VerifyFirmware(stream, isXci);

                if (version != null)
                {
                    stringHandle = storeString(version.VersionString);
                }
            }
            catch(Exception _)
            {

            }

            return stringHandle;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceInstallFirmware")]
        public static void JniInstallFirmware(JEnvRef jEnv, JObjectLocalRef jObj, JInt descriptor, JBoolean isXci)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");

            var stream = OpenFile(descriptor);

            InstallFirmware(stream, isXci);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetInstalledFirmwareVersion")]
        public static JLong JniGetInstalledFirmwareVersion(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");

            var version = GetInstalledFirmwareVersion();
            long stringHandle = -1;

            if (version != String.Empty)
            {
                stringHandle = storeString(version);
            }

            return stringHandle;
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitialize")]
        public static JBoolean JniInitializeGraphicsNative(JEnvRef jEnv, JObjectLocalRef jObj, JObjectLocalRef graphicObject)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
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
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
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
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
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

            if ((long)driverHandle != 0)
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
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            Renderer?.Window?.SetSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererRunLoop")]
        public static void JniRunLoopNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetSwapBuffersCallback(() =>
            {
                var time = SwitchDevice.EmulationContext.Statistics.GetGameFrameTime();
                onFrameEnd(time);
            });
            RunLoop();
        }


        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_loggingSetEnabled")]
        public static void JniSetLoggingEnabledNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt logLevel, JBoolean enabled)
        {
            Logger.SetEnable((LogLevel)(int)logLevel, enabled);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameInfoFromPath")]
        public static JObjectLocalRef JniGetGameInfo(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef path)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var info = GetGameInfo(GetString(jEnv, path));
            return GetInfo(jEnv, info);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameInfo")]
        public static JObjectLocalRef JniGetGameInfo(JEnvRef jEnv, JObjectLocalRef jObj, JInt fileDescriptor, JLong extension)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            using var stream = OpenFile(fileDescriptor);
            var ext = GetStoredString(extension);
            var info = GetGameInfo(stream, ext.ToLower());
            return GetInfo(jEnv, info);
        }

        private static JObjectLocalRef GetInfo(JEnvRef jEnv, GameInfo? info)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
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

            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleName"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.TitleName)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleId"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.TitleId)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Developer"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.Developer)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Version"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, info?.Version)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Icon"), GetCCharSequence("Ljava/lang/String;")), CreateString(jEnv, Convert.ToBase64String(info?.Icon ?? Array.Empty<byte>()))._value);
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
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetVsyncState(enabled);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSwapBufferCallback")]
        public static void JniSetSwapBuffersCallbackNative(JEnvRef jEnv, JObjectLocalRef jObj, IntPtr swapBuffersCallback)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            _swapBuffersCallback = Marshal.GetDelegateForFunctionPointer<SwapBuffersCallback>(swapBuffersCallback);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputInitialize")]
        public static void JniInitializeInput(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            InitializeInput(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetClientSize")]
        public static void JniSetClientSize(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetClientSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetTouchPoint")]
        public static void JniSetTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj, JInt x, JInt y)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetTouchPoint(x, y);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputReleaseTouchPoint")]
        public static void JniReleaseTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            ReleaseTouchPoint();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputUpdate")]
        public static void JniUpdateInput(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            UpdateInput();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonPressed")]
        public static void JniSetButtonPressed(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JInt id)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetButtonPressed((GamepadButtonInputId)(int)button, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonReleased")]
        public static void JniSetButtonReleased(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JInt id)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetButtonReleased((GamepadButtonInputId)(int)button, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetAccelerometerData")]
        public static void JniSetAccelerometerData(JEnvRef jEnv, JObjectLocalRef jObj, JFloat x, JFloat y, JFloat z, JInt id)
        {
            var accel = new Vector3(x, y, z);
            SetAccelerometerData(accel, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetGyroData")]
        public static void JniSetGyroData(JEnvRef jEnv, JObjectLocalRef jObj, JFloat x, JFloat y, JFloat z, JInt id)
        {
            var gryo = new Vector3(x, y, z);
            SetGryoData(gryo, id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetStickAxis")]
        public static void JniSetStickAxis(JEnvRef jEnv, JObjectLocalRef jObj, JInt stick, JFloat x, JFloat y, JInt id)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            SetStickAxis((StickInputId)(int)stick, new Vector2(float.IsNaN(x) ? 0 : x, float.IsNaN(y) ? 0 : y), id);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputConnectGamepad")]
        public static JInt JniConnectGamepad(JEnvRef jEnv, JObjectLocalRef jObj, JInt index)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            return ConnectGamepad(index);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userGetOpenedUser")]
        public static JLong JniGetOpenedUser(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetOpenedUser();

            return storeString(userId);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userGetUserPicture")]
        public static JLong JniGetUserPicture(JEnvRef jEnv, JObjectLocalRef jObj, JLong userIdPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetStoredString(userIdPtr) ?? "";

            return storeString(GetUserPicture(userId));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userSetUserPicture")]
        public static void JniGetUserPicture(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef userIdPtr, JStringLocalRef picturePtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetString(jEnv, userIdPtr) ?? "";
            var picture = GetString(jEnv, picturePtr) ?? "";

            SetUserPicture(userId, picture);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userGetUserName")]
        public static JLong JniGetUserName(JEnvRef jEnv, JObjectLocalRef jObj, JLong userIdPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetStoredString(userIdPtr) ?? "";

            return storeString(GetUserName(userId));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userSetUserName")]
        public static void JniSetUserName(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef userIdPtr, JStringLocalRef userNamePtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetString(jEnv, userIdPtr) ?? "";
            var userName = GetString(jEnv, userNamePtr) ?? "";

            SetUserName(userId, userName);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userGetAllUsers")]
        public static JArrayLocalRef JniGetAllUsers(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var users = GetAllUsers();

            return CreateStringArray(jEnv, users.ToList());
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userAddUser")]
        public static void JniAddUser(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef userNamePtr, JStringLocalRef picturePtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userName = GetString(jEnv, userNamePtr) ?? "";
            var picture = GetString(jEnv, picturePtr) ?? "";

            AddUser(userName, picture);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userDeleteUser")]
        public static void JniDeleteUser(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef userIdPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetString(jEnv, userIdPtr) ?? "";

            DeleteUser(userId);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_uiHandlerSetup")]
        public static void JniSetupUiHandler(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            SetupUiHandler();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_uiHandlerWait")]
        public static void JniWaitUiHandler(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            WaitUiHandler();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_uiHandlerStopWait")]
        public static void JniStopUiHandlerWait(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            StopUiHandlerWait();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_uiHandlerSetResponse")]
        public static void JniSetUiHandlerResponse(JEnvRef jEnv, JObjectLocalRef jObj, JBoolean isOkPressed, JLong input)
        {
            SetUiHandlerResponse(isOkPressed, input);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userOpenUser")]
        public static void JniOpenUser(JEnvRef jEnv, JObjectLocalRef jObj, JLong userIdPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetStoredString(userIdPtr) ?? "";

            OpenUser(userId);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_userCloseUser")]
        public static void JniCloseUser(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef userIdPtr)
        {
            Logger.Trace?.Print(LogClass.Application, "Jni Function Call");
            var userId = GetString(jEnv, userIdPtr) ?? "";

            CloseUser(userId);
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
