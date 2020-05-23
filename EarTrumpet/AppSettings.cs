using Bugsnag.Payload;
using EarTrumpet.DataModel.Audio;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.Extensions;
using EarTrumpet.Interop.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Keys = System.Windows.Forms.Keys;

namespace EarTrumpet
{
    public class AppSettings
    {
        public event EventHandler<bool> UseLegacyIconChanged;
        public event Action FlyoutHotkeyTyped;
        public event Action MixerHotkeyTyped;
        public event Action SettingsHotkeyTyped;

        private ISettingsBag _settings = StorageFactory.GetSettings();

        private VolumeTargetMap _volumeTargets = null;

        public VolumeTargetMap VolumeTargetMap
        {
            get => _volumeTargets;
        }

        public void RegisterHotkeys()
        {
            HotkeyManager.Current.Register(FlyoutHotkey);
            HotkeyManager.Current.Register(MixerHotkey);
            HotkeyManager.Current.Register(SettingsHotkey);

            HotkeyManager.Current.KeyPressed += (hotkey) =>
            {
                if (hotkey.Equals(FlyoutHotkey))
                {
                    Trace.WriteLine("AppSettings FlyoutHotkeyTyped");
                    FlyoutHotkeyTyped?.Invoke();
                }
                else if (hotkey.Equals(SettingsHotkey))
                {
                    Trace.WriteLine("AppSettings SettingsHotkeyTyped");
                    SettingsHotkeyTyped?.Invoke();
                }
                else if (hotkey.Equals(MixerHotkey))
                {
                    Trace.WriteLine("AppSettings MixerHotkeyTyped");
                    MixerHotkeyTyped?.Invoke();
                }
            };
        }

        public HotkeyData FlyoutHotkey
        {
            get => _settings.Get("Hotkey", new HotkeyData { });
            set
            {
                HotkeyManager.Current.Unregister(FlyoutHotkey);
                _settings.Set("Hotkey", value);
                HotkeyManager.Current.Register(FlyoutHotkey);
            }
        }

        public HotkeyData MixerHotkey
        {
            get => _settings.Get("MixerHotkey", new HotkeyData { });
            set
            {
                HotkeyManager.Current.Unregister(MixerHotkey);
                _settings.Set("MixerHotkey", value);
                HotkeyManager.Current.Register(MixerHotkey);
            }
        }

        public HotkeyData SettingsHotkey
        {
            get => _settings.Get("SettingsHotkey", new HotkeyData { });
            set
            {
                HotkeyManager.Current.Unregister(SettingsHotkey);
                _settings.Set("SettingsHotkey", value);
                HotkeyManager.Current.Register(SettingsHotkey);
            }
        }

        public void InitializeVolumeTargetMapping(IAudioDeviceManager manager)
        {
            _volumeTargets = new VolumeTargetMap(manager);
            LoadVolumeTargets();

            foreach(Keys modifiers in _volumeTargets.RegisteredModifiers)
            {
                HotkeyData volumeUp = new HotkeyData(Keys.VolumeUp, modifiers);
                HotkeyData volumeDown = new HotkeyData(Keys.VolumeDown, modifiers);
                HotkeyData volumeMute = new HotkeyData(Keys.VolumeMute, modifiers);

                HotkeyManager.Current.Register(volumeUp);
                HotkeyManager.Current.Register(volumeDown);
                HotkeyManager.Current.Register(volumeMute);
            }

            HotkeyManager.Current.KeyPressed += (hotkey) =>
            {
                IStreamWithVolumeControl target = _volumeTargets.StreamForModifiers(hotkey.Modifiers);
                if (target != null)
                {
                    float volume = target.Volume;

                    if (hotkey.Key == Keys.VolumeUp)
                    {
                        volume += 0.05f;
                    }
                    else if (hotkey.Key == Keys.VolumeDown)
                    {
                        volume -= 0.05f;
                    }
                    else
                    {
                        target.IsMuted = !target.IsMuted;
                        return;
                    }

                    target.Volume = volume.Bound(0.0f, 1.0f);
                }
            };
        }

        public IStreamWithVolumeControl GetVolumeTargetForModifiers(Keys modifiers)
        {
            return _volumeTargets.StreamForModifiers(modifiers);
        }

        private void SaveVolumeTargets()
        {
            _settings.Set("VolumeTargets", _volumeTargets.GetSettingsRepresentation());
        }

        private void LoadVolumeTargets()
        {
            try
            {
                List<VolumeTargetMap.StoredMapping> stored = _settings.Get("VolumeTargets", new List<VolumeTargetMap.StoredMapping>());
                _volumeTargets.RestoreSettingsRepresentation(stored);
            }
            catch (System.Exception)
            {
                _volumeTargets.RestoreSettingsRepresentation(null);
            }
        }

        public void SetVolumeTargetForModifiers(Keys modifiers, VolumeTargetMap.VolumeTarget target)
        {
            HotkeyData volumeUp = new HotkeyData(Keys.VolumeUp, modifiers);
            HotkeyData volumeDown = new HotkeyData(Keys.VolumeDown, modifiers);
            HotkeyData volumeMute = new HotkeyData(Keys.VolumeMute, modifiers);

            HotkeyManager.Current.Unregister(volumeUp);
            HotkeyManager.Current.Unregister(volumeDown);
            HotkeyManager.Current.Unregister(volumeMute);

            _volumeTargets.SetTargetForModifiers(modifiers, target);

            if (target != null && target != VolumeTargetMap.VolumeTarget.None)
            {
                HotkeyManager.Current.Register(volumeUp);
                HotkeyManager.Current.Register(volumeDown);
                HotkeyManager.Current.Register(volumeMute);
            }

            SaveVolumeTargets();
        }

        public bool UseLegacyIcon
        {
            get
            {
                // Note: Legacy compat, we used to write string bools.
                var ret = _settings.Get("UseLegacyIcon", "False");
                bool.TryParse(ret, out bool isUseLegacyIcon);
                return isUseLegacyIcon;
            }
            set
            {
                _settings.Set("UseLegacyIcon", value.ToString());
                UseLegacyIconChanged?.Invoke(null, UseLegacyIcon);
            }
        }

        public bool HasShownFirstRun
        {
            get => _settings.HasKey("hasShownFirstRun");
            set => _settings.Set("hasShownFirstRun", value);
        }

        public bool IsTelemetryEnabled
        {
            get
            {
                return _settings.Get("IsTelemetryEnabled", IsTelemetryEnabledByDefault());
            }
            set => _settings.Set("IsTelemetryEnabled", value);
        }

        private bool IsTelemetryEnabledByDefault()
        {
            // Discussion on what to include:
            // https://gist.github.com/henrik/1688572
            var europeanUnionRegions = new string[]
            {
                // EU 28
                "AT", // Austria
                "BE", // Belgium
                "BG", // Bulgaria
                "HR", // Croatia
                "CY", // Cyprus
                "CZ", // Czech Republic
                "DK", // Denmark
                "EE", // Estonia
                "FI", // Finland
                "FR", // France
                "DE", // Germany
                "GR", // Greece
                "HU", // Hungary
                "IE", // Ireland, Republic of (EIRE)
                "IT", // Italy
                "LV", // Latvia
                "LT", // Lithuania
                "LU", // Luxembourg
                "MT", // Malta
                "NL", // Netherlands
                "PL", // Poland
                "PT", // Portugal
                "RO", // Romania
                "SK", // Slovakia
                "SI", // Slovenia
                "ES", // Spain
                "SE", // Sweden
                "GB", // United Kingdom (Great Britain)

                // Outermost Regions (OMR)
                "GF", // French Guiana
                "GP", // Guadeloupe
                "MQ", // Martinique
                "ME", // Montenegro
                "YT", // Mayotte
                "RE", // Réunion
                "MF", // Saint Martin

                // Special Cases: Part of EU
                "GI", // Gibraltar
                "AX", // Åland Islands

                // Overseas Countries and Territories (OCT)
                "PM", // Saint Pierre and Miquelon
                "GL", // Greenland
                "BL", // Saint Bartelemey
                "SX", // Sint Maarten
                "AW", // Aruba
                "CW", // Curacao
                "WF", // Wallis and Futuna
                "PF", // French Polynesia
                "NC", // New Caledonia
                "TF", // French Southern Territories
                "AI", // Anguilla
                "BM", // Bermuda
                "IO", // British Indian Ocean Territory
                "VG", // Virgin Islands, British
                "KY", // Cayman Islands
                "FK", // Falkland Islands (Malvinas)
                "MS", // Montserrat
                "PN", // Pitcairn
                "SH", // Saint Helena
                "GS", // South Georgia and the South Sandwich Islands
                "TC", // Turks and Caicos Islands

                // Microstates
                "AD", // Andorra
                "LI", // Liechtenstein
                "MC", // Monaco
                "SM", // San Marino
                "VA", // Vatican City

                // Other
                "JE", // Jersey
                "GG", // Guernsey
            };
            var region = new Windows.Globalization.GeographicRegion();
            return !europeanUnionRegions.Contains(region.CodeTwoLetter);
        }
    }
}