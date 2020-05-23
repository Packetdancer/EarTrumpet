using EarTrumpet.Extensions;
using EarTrumpet.Interop;
using EarTrumpet.Interop.Helpers;
using System;
using System.Diagnostics;

namespace EarTrumpet.DataModel.AppInformation.Internal
{
    class SystemSoundsAppInfo : IAppInfo
    {
        public event Action<IAppInfo> Stopped { add { } remove { } }
        public uint BackgroundColor => 0x000000;
        public string ExeName => "*SystemSounds";
        public string DisplayName => null;
        public string PackageInstallPath => "System.SystemSoundsSession";
        public bool IsDesktopApp => true;
        public string SmallLogoPath { get; set; }

        public SystemSoundsAppInfo()
        {
            SmallLogoPath = Environment.ExpandEnvironmentVariables(User32Helper.Is64BitOperatingSystem() ? 
                @"%windir%\sysnative\audiosrv.dll,203" : @"%windir%\system32\audiosrv.dll,203");
        }

    }
}
