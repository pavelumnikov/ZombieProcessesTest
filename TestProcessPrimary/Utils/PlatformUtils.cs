using System;

namespace TestProcessPrimary.Utils
{
    static class PlatformUtils
    {
        public static bool IsWindows
        {
            get
            {
                var platformId = Environment.OSVersion.Platform;

                return platformId == PlatformID.Win32NT ||
                    platformId == PlatformID.Win32Windows ||
                    platformId == PlatformID.Win32S ||
                    platformId == PlatformID.WinCE;
            }
        }
    }
}
