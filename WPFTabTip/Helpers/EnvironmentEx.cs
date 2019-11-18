using Microsoft.Win32;
using WPFTabTipMixedHardware.Models;

namespace WPFTabTipMixedHardware.Helpers
{
    internal static class EnvironmentEx
    {
        private static OSVersion OSVersion = OSVersion.Undefined;

        internal static OSVersion GetOSVersion()
        {
            if (OSVersion != OSVersion.Undefined)
                return OSVersion;

            string OSName = GetOSName();

            if (OSName.Contains("7"))
                OSVersion = OSVersion.Win7;
            else if (OSName.Contains("8"))
                OSVersion = OSVersion.Win8Or81;
            else if (OSName.Contains("10"))
                OSVersion = OSVersion.Win10;

            return OSVersion;
        }

        private static string GetOSName()
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (rk == null) return "";
            return (string)rk.GetValue("ProductName");
        }
    }
}
