using System;
using System.Runtime.InteropServices;
using Ryujinx.Common.Configuration;
using System.Collections.Generic;
using LibRyujinx.Jni.Pointers;
using LibRyujinx.Jni.References;
using LibRyujinx.Jni.Values;
using LibRyujinx.Jni.Primitives;
using LibRyujinx.Jni;
using Rxmxnx.PInvoke;
using System.Text;
using LibRyujinx.Jni.Internal.Pointers;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using LibRyujinx.Shared.Audio.Oboe;
using System.Threading;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        private static ManualResetEvent _surfaceEvent;
        private static long _surfacePtr;

        [DllImport("libryujinxjni")]
        private extern static IntPtr getStringPointer(JEnvRef jEnv, JStringLocalRef s);

        public delegate IntPtr JniCreateSurface(IntPtr native_surface, IntPtr instance);

        [UnmanagedCallersOnly(EntryPoint = "JNI_OnLoad")]
        internal static int LoadLibrary(JavaVMRef vm, IntPtr unknown)
        {
            return 0x00010006; //JNI_VERSION_1_6
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_initialize")]
        public static JBoolean JniInitialize(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef jpath)
        {
            var path = GetString(jEnv, jpath);

            Ryujinx.Common.SystemInfo.SystemInfo.IsBionic = true;

            var init = Initialize(path);

            AudioDriver = new OboeHardwareDeviceDriver();

            _surfaceEvent = new ManualResetEvent(false);

            Logger.AddTarget(
                new AsyncLogTargetWrapper(
                new AndroidLogTarget("Ryujinx"),
                1000,
                AsyncLogTargetOverflowAction.Block
                ));

            return init;
        }

        private static string GetString(JEnvRef jEnv, JStringLocalRef jString)
        {
            var stringPtr = getStringPointer(jEnv, jString);

            var s = Marshal.PtrToStringAnsi(stringPtr);
            Marshal.FreeHGlobal(stringPtr);
            return s;
        }

        private static JStringLocalRef CreateString(string str, JEnvRef jEnv)
        {
            return str.AsSpan().WithSafeFixed(jEnv, CreateString);
        }


        private static JStringLocalRef CreateString(in IReadOnlyFixedContext<Char> ctx, JEnvRef jEnv)
        {
            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;

            IntPtr newStringPtr = jInterface.NewStringPointer;
            NewStringDelegate newString = newStringPtr.GetUnsafeDelegate<NewStringDelegate>();

            return newString(jEnv, ctx.Pointer, ctx.Values.Length);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceInitialize")]
        public static JBoolean JniInitializeDeviceNative(JEnvRef jEnv, JObjectLocalRef jObj, JBoolean isHostMapped)
        {
            return InitializeDevice(isHostMapped);
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

            GraphicsConfiguration graphicsConfiguration = new GraphicsConfiguration()
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
            Silk.NET.Core.Loader.SearchPathContainer.Platform = Silk.NET.Core.Loader.UnderlyingPlatform.Android;
            return InitializeGraphics(graphicsConfiguration);
        }

        private static CCharSequence GetCCharSequence(string s)
        {
            return (CCharSequence)Encoding.UTF8.GetBytes(s).AsSpan();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsSetSurface")]
        public unsafe static void JniSetSurface(JEnvRef jEnv, JObjectLocalRef jObj, JLong surfacePtr)
        {
            _surfacePtr = surfacePtr;

            _surfaceEvent.Set();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitializeRenderer")]
        public unsafe static JBoolean JniInitializeGraphicsRendererNative(JEnvRef jEnv, JObjectLocalRef jObj, JArrayLocalRef extensionsArray, JLong surfacePtr)
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

            List<string> extensions = new List<string>();

            var count = getArrayLength(jEnv, extensionsArray);

            for(int i = 0; i < count; i++)
            {
                var obj = getObjectArrayElement(jEnv, extensionsArray, i);
                var ext = obj.Transform<JObjectLocalRef,JStringLocalRef>();

                extensions.Add(GetString(jEnv, ext));
            }

            _surfaceEvent.Set();

            _surfacePtr = (long)surfacePtr;

            CreateSurface createSurfaceFunc = (IntPtr instance) =>
            {
                _surfaceEvent.WaitOne();
                _surfaceEvent.Reset();

                var api = Vk.GetApi();
                if (api.TryGetInstanceExtension(new Instance(instance), out KhrAndroidSurface surfaceExtension))
                {
                    var createInfo = new AndroidSurfaceCreateInfoKHR()
                    {
                        SType = StructureType.AndroidSurfaceCreateInfoKhr,
                        Window = (nint*)_surfacePtr
                    };

                    var result = surfaceExtension.CreateAndroidSurface(new Instance(instance), createInfo, null, out var surface);

                    return (nint)surface.Handle;
                }

                return IntPtr.Zero;
            };

            return InitializeGraphicsRenderer(GraphicsBackend.Vulkan, createSurfaceFunc, extensions.ToArray());
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSize")]
        public static void JniSetRendererSizeNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            Renderer?.Window?.SetSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererRunLoop")]
        public static void JniRunLoopNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            RunLoop();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceGetGameInfo")]
        public static JObjectLocalRef JniGetGameInfo(JEnvRef jEnv, JObjectLocalRef jObj, JInt fileDescriptor, JBoolean isXci)
        {
            using var stream = OpenFile(fileDescriptor);

            var info = GetGameInfo(stream, isXci) ?? new GameInfo();

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
            var newGlobalRef = newGlobalRefPtr.GetUnsafeDelegate <NewGlobalRefDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getMethod = getMethodPtr.GetUnsafeDelegate<GetMethodIdDelegate>();
            var newObject = newObjectPtr.GetUnsafeDelegate<NewObjectDelegate>();
            var setObjectField = setObjectFieldPtr.GetUnsafeDelegate<SetObjectFieldDelegate>();
            var setDoubleField = setDoubleFieldPtr.GetUnsafeDelegate<SetDoubleFieldDelegate>();

            var javaClass = findClass(jEnv, javaClassName);
            var newGlobal = newGlobalRef(jEnv, javaClass._value);
            var constructor = getMethod(jEnv, javaClass, GetCCharSequence("<init>"), GetCCharSequence("()V"));
            var newObj = newObject(jEnv, javaClass, constructor, 0);

            using var sha = SHA256.Create();

            var iconCacheByte = sha.ComputeHash(info.Icon ?? new byte[0]);
            var iconCache = BitConverter.ToString(iconCacheByte).Replace("-", "");

            var cacheDirectory = Path.Combine(AppDataManager.BaseDirPath, "iconCache");
            Directory.CreateDirectory(cacheDirectory);

            var cachePath = Path.Combine(cacheDirectory, iconCache);
            if (!File.Exists(cachePath))
            {
                File.WriteAllBytes(cachePath, info.Icon ?? new byte[0]);
            }

            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleName"), GetCCharSequence("Ljava/lang/String;")), CreateString(info.TitleName, jEnv)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("TitleId"), GetCCharSequence("Ljava/lang/String;")), CreateString(info.TitleId, jEnv)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Developer"), GetCCharSequence("Ljava/lang/String;")), CreateString(info.Developer, jEnv)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("Version"), GetCCharSequence("Ljava/lang/String;")), CreateString(info.Version, jEnv)._value);
            setObjectField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("IconCache"), GetCCharSequence("Ljava/lang/String;")), CreateString(iconCache, jEnv)._value);
            setDoubleField(jEnv, newObj, getFieldId(jEnv, javaClass, GetCCharSequence("FileSize"), GetCCharSequence("D")), info.FileSize);

            return newObj;
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
        public static void JniSetButtonPressed(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JStringLocalRef id)
        {
            SetButtonPressed((Ryujinx.Input.GamepadButtonInputId)(int)button, GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonReleased")]
        public static void JniSetButtonReleased(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JStringLocalRef id)
        {
            SetButtonReleased((Ryujinx.Input.GamepadButtonInputId)(int)button, GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetStickAxis")]
        public static void JniSetStickAxis(JEnvRef jEnv, JObjectLocalRef jObj, JInt stick, JFloat x, JFloat y, JStringLocalRef id)
        {
            SetStickAxis((Ryujinx.Input.StickInputId)(int)stick, new System.Numerics.Vector2(x, y), GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputConnectGamepad")]
        public static JStringLocalRef JniConnectGamepad(JEnvRef jEnv, JObjectLocalRef jObj, JInt index)
        {
            var id = ConnectGamepad(index);

            return (id ?? "").AsSpan().WithSafeFixed(jEnv, CreateString);
        }

        private static Stream OpenFile(int descriptor)
        {
            var safeHandle = new SafeFileHandle(descriptor, false);

            return new FileStream(safeHandle, FileAccess.Read);
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
            Silent = 0x08
        }
    }
}
