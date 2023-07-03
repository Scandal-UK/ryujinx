using System;

namespace Ryujinx.Common.SystemInfo
{
    public class SystemInfo
    {
        public static bool IsBionic { get; set; }

        public static bool IsAndroid()
        {
            return OperatingSystem.IsAndroid() || IsBionic;
        }
    }
}
