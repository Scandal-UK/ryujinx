using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibRyujinx.Shared.Audio.Oboe
{
    internal static partial class OboeInterop
    {
        private const string InteropLib = "libryujinxjni";

        [LibraryImport(InteropLib, EntryPoint = "create_session")]
        public static partial IntPtr CreateSession(int sample_format,
                         uint sample_rate,
                         uint channel_count);


        [LibraryImport(InteropLib, EntryPoint = "start_session")]
        public static partial void StartSession(IntPtr session);

        [LibraryImport(InteropLib, EntryPoint = "stop_session")]
        public static partial void StopSession(IntPtr session);

        [LibraryImport(InteropLib, EntryPoint = "close_session")]
        public static partial void CloseSession(IntPtr session);

        [LibraryImport(InteropLib, EntryPoint = "set_session_volume")]
        public static partial void SetSessionVolume(IntPtr session, float volume);

        [LibraryImport(InteropLib, EntryPoint = "get_session_volume")]
        public static partial float GetSessionVolume(IntPtr session);

        [LibraryImport(InteropLib, EntryPoint = "is_playing")]
        public static partial int IsPlaying(IntPtr session);

        [LibraryImport(InteropLib, EntryPoint = "write_to_session")]
        public static partial void WriteToSession(IntPtr session, ulong data, ulong samples);
    }
}
