using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ryujinx.Common
{
    // DO NOT EDIT, filled by CI
    public static class ReleaseInformation
    {
        private const string FlatHubChannelOwner = "flathub";

        private const string BuildVersion = "%%RYUJINX_BUILD_VERSION%%";
        private const string BuildGitHash = "%%RYUJINX_BUILD_GIT_HASH%%";
        private const string ReleaseChannelName = "%%RYUJINX_TARGET_RELEASE_CHANNEL_NAME%%";
        private const string ConfigFileName = "%%RYUJINX_CONFIG_FILE_NAME%%";

        public const string ReleaseChannelOwner = "%%RYUJINX_TARGET_RELEASE_CHANNEL_OWNER%%";
        public const string ReleaseChannelRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_REPO%%";

        public static string ConfigName => !ConfigFileName.StartsWith("%%") ? ConfigFileName : "Config.json";

        public static bool IsValid =>
            !BuildGitHash.StartsWith("%%") &&
            !ReleaseChannelName.StartsWith("%%") &&
            !ReleaseChannelOwner.StartsWith("%%") &&
            !ReleaseChannelRepo.StartsWith("%%") &&
            !ConfigFileName.StartsWith("%%");

        public static bool IsFlatHubBuild => IsValid && ReleaseChannelOwner.Equals(FlatHubChannelOwner);

        public static string Version => SystemInfo.SystemInfo.IsBionic ? "Android_1.0" : IsValid ? BuildVersion : !RuntimeFeature.IsDynamicCodeCompiled ? "libryujinx" : Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    }
}
