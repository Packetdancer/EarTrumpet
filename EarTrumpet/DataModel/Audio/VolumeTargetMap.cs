using EarTrumpet.Properties;
using EarTrumpet.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Keys = System.Windows.Forms.Keys;

namespace EarTrumpet.DataModel.Audio
{
    public class VolumeTargetMap : BindableBase
    {
        public class VolumeTarget : BindableBase, IEquatable<VolumeTarget>, UI.Helpers.IAppIconSource
        {
            private INamedStreamWithVolumeControl _stream;
            public INamedStreamWithVolumeControl Stream
            {
                get
                {
                    if (_stream == null)
                    {
                        RefreshStream();
                    }

                    return _stream;
                }
            }
            public INamedStreamWithVolumeControl RealStream { get => _stream; }

            public void RefreshStream(bool forced = false, INamedStreamWithVolumeControl newStream = null)
            {
                if (Id == null) return;

                if (_stream != null && !forced) return;

                IStreamWithVolumeControl old = _stream;
                _stream = newStream != null ? newStream : SharedManager.GetStreamById(Id);

                if (old == _stream) return;

                if (old != null)
                {
                    INotifyPropertyChanged notifiable = (INotifyPropertyChanged)old;
                    notifiable.PropertyChanged -= this.Stream_PropertyChanged;
                }

                if (_stream != null)
                {
                    INotifyPropertyChanged notifiable = (INotifyPropertyChanged)_stream;
                    notifiable.PropertyChanged += this.Stream_PropertyChanged;
                    _streamName = _stream.DisplayName;
                    if (_stream is IAudioDeviceSession)
                    {
                        IAudioDeviceSession session = (IAudioDeviceSession)_stream;

                        if (session.Parent != null)
                        {
                            _parent = session.Parent;
                            ParentName = session.Parent.DisplayName;
                        }
                    }
                    IconPath = _stream.IconPath;
                    IsDesktopApp = _stream.IsDesktopApp;
                }

                RaisePropertyChanged("Stream");
                RaisePropertyChanged("StreamName");
                RaisePropertyChanged("Disabled");
                RaisePropertyChanged("QualifiedStreamName");
            }

            private string _streamName;
            public string StreamName {
                get => (RealStream != null ? RealStreamName : QualifiedStreamName) + (Disabled ? " " + Resources.VolumeTargetDisabledText : "");
                set => _streamName = value;
            }

            public string RealStreamName
            {
                get => _streamName;
            }

            public string QualifiedStreamName { get => RealStreamName + (ParentName != null ? " (" + ParentName + ")" : "") + (Disabled ? " " + Resources.VolumeTargetDisabledText : ""); }

            public override string ToString() { return QualifiedStreamName; }

            private string _id;
            public string Id
            {
                get => _id;
                set
                {
                    if (value != null && value.Contains("|1%b"))
                    {
                        value = value.Substring(0, value.IndexOf("|1%b"));
                    }
                    _id = value;
                }
            }

            public override int GetHashCode()
            {
                if (Id == null)
                {
                    return -1;
                }

                return Id.GetHashCode();
            }

            private IAudioDevice _parent = null;

            public string ParentName { get; set; }


            private string _iconPath = "%windir%\\system32\\shell32.dll,200";
            public string IconPath
            {
                get
                {
                    if (Id == null)
                    {
                        return "%windir%\\system32\\mmres.dll,3004";
                    }

                    return Stream != null ? Stream.IconPath : _iconPath;
                }
                set => _iconPath = value;
            }

            private bool _desktopApp = true;
            public bool IsDesktopApp { get => Stream != null ? Stream.IsDesktopApp : _desktopApp; set => _desktopApp = value; }

            public string IconText { get => StreamName != null ? StreamName.Substring(0, 1).ToUpper() : null; }

            public bool Disabled { get
                {
                    if (Id == null) return false;

                    if (_stream == null) return true;

                    return false;
                }
            }

            public bool IsChild { get; set; }

            public int Indent { get
                {
                    return ((ParentName != null) && (_stream != null)) ? 22 : 0;
                }
            }

            public bool Equals(VolumeTarget other)
            {
                return this.Id == other.Id;
            }

            private void Stream_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "State")
                {
                    IAudioDeviceSession session = (IAudioDeviceSession)sender;

                    if (session.State == SessionState.Invalid || session.State == SessionState.Expired)
                    {
                        session.PropertyChanged -= this.Stream_PropertyChanged;
                        _stream = null;
                    }

                    if (_stream != null)
                    {
                        IconPath = _stream.IconPath;
                        IsDesktopApp = _stream.IsDesktopApp;
                    }

                    RaisePropertyChanged("Stream");
                    RaisePropertyChanged("StreamName");
                    RaisePropertyChanged("Disabled");
                    RaisePropertyChanged("QualifiedStreamName");
                }

                if (e.PropertyName == "DisplayName")
                {
                    _streamName = _stream.DisplayName;
                    RaisePropertyChanged("StreamName");
                    RaisePropertyChanged("QualifiedStreamName");
                }
            }
            private bool _bound = false;

            private void AddBindings()
            {
                if (_bound) return;

                if (_stream != null)
                {
                    INotifyPropertyChanged notifiable = (INotifyPropertyChanged)_stream;
                    _stream.PropertyChanged += this.Stream_PropertyChanged;
                }

                _bound = true;
            }

            public void RemoveBindings()
            {
                if (!_bound) return;

                if (_stream != null)
                {
                    INotifyPropertyChanged notifiable = (INotifyPropertyChanged)_stream;
                    notifiable.PropertyChanged -= this.Stream_PropertyChanged;
                }

                _bound = false;

            }

            public VolumeTarget(INamedStreamWithVolumeControl stream)
            {
                _stream = stream;
                if (_stream is IAudioDeviceSession)
                {
                    IAudioDeviceSession session = (IAudioDeviceSession)_stream;
                    _parent = session.Parent;
                    if (_parent != null)
                    {
                        ParentName = _parent.DisplayName;
                    }
                }
                AddBindings();
            }

            ~VolumeTarget()
            {
                RemoveBindings();
            }

            public static VolumeTarget None = new VolumeTarget(null)
            {
                StreamName = EarTrumpet.Properties.Resources.VolumeTargetNoneText,
                Id = null
            };

        }

        [Serializable]
        public struct StoredMapping
        {
            public Keys Modifiers;
            public string StreamName;
            public string ParentName;
            public string QualifiedId;
            public string IconPath;
            public bool IsDesktopApp;
        }

        private Dictionary<Keys, VolumeTarget> _targets;
        private static IAudioDeviceManager _deviceManager;
        public static VolumeTargetMap _sharedMap = null;
        public static VolumeTargetMap SharedMap { get => _sharedMap; }

        private Dictionary<string, VolumeTarget> _cached = new Dictionary<string, VolumeTarget>();

        private VolumeTarget CreateVolumeTarget(string streamId, INamedStreamWithVolumeControl stream)
        {
            if (streamId == null) return VolumeTarget.None;

            if (streamId.Contains("|1%b"))
            {
                streamId = streamId.Substring(0, streamId.IndexOf("|1%b"));
            }

            VolumeTarget result;
            _cached.TryGetValue(streamId, out result);

            if (result == null)
            {
                result = new VolumeTarget(stream)
                {
                    Id = streamId
                };
                _cached.Add(streamId, result);
            }

            return result;
        }

        protected static IAudioDeviceManager SharedManager { get => _deviceManager; }

        public VolumeTargetMap(IAudioDeviceManager manager)
            : this(manager, null)
        {
        }

        public VolumeTargetMap(IAudioDeviceManager manager, List<StoredMapping> stored)
        {
            _deviceManager = manager;
            if (stored != null)
                RestoreSettingsRepresentation(stored);
            else
                _targets = new Dictionary<Keys, VolumeTarget>();

            if (_sharedMap == null)
            {
                _sharedMap = this;
            }

            _deviceManager.Devices.CollectionChanged += this.DeviceManager_CollectionChanged;
            foreach (IAudioDevice device in _deviceManager.Devices)
            {
                device.Groups.CollectionChanged += this.Device_CollectionChanged;
            }
        }

        ~VolumeTargetMap()
        {
            foreach (IAudioDevice device in _deviceManager.Devices)
            {
                device.Groups.CollectionChanged -= this.Device_CollectionChanged;
            }
            _deviceManager.Devices.CollectionChanged -= this.DeviceManager_CollectionChanged;
        }

        private void DeviceManager_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) 
            {
                foreach (IAudioDevice device in e.OldItems)
                {
                    device.Groups.CollectionChanged -= this.Device_CollectionChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (IAudioDevice device in e.NewItems)
                {
                    device.Groups.CollectionChanged += this.Device_CollectionChanged;
                }
            }
            BuildAvailableTargets();
        }

        private void Device_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            BuildAvailableTargets();            
        }

        public bool HasTargetForModifiers(Keys modifiers)
        {
            return _targets.ContainsKey(modifiers);
        }

        public IStreamWithVolumeControl StreamForModifiers(Keys modifiers)
        {
            IStreamWithVolumeControl result = null;

            VolumeTarget target = TargetForModifiers(modifiers);

            if (target != null)
            {
                result = target.Stream;
            }

            return result;
        }

        public VolumeTarget TargetForModifiers(Keys modifiers)
        {
            VolumeTarget target;
            _targets.TryGetValue(modifiers, out target);
            return target;
        }


        public void SetTargetForModifiers(Keys modifiers, VolumeTarget target)
        {
            if (_targets.ContainsKey(modifiers))
            {
                _targets.Remove(modifiers);
            }

            if ((target == null) || (target == VolumeTarget.None)) return;

            _targets.Add(modifiers, target);
        }

        public List<StoredMapping> GetSettingsRepresentation()
        {
            List<StoredMapping> stored = new List<StoredMapping>();

            foreach (KeyValuePair<Keys, VolumeTarget> record in _targets)
            {
                if (record.Value != null)
                {
                    StoredMapping mapping = new StoredMapping();
                    mapping.Modifiers = record.Key;
                    mapping.StreamName = record.Value.RealStreamName;
                    mapping.QualifiedId = record.Value.Id;
                    mapping.ParentName = record.Value.ParentName;
                    mapping.IconPath = record.Value.IconPath;
                    mapping.IsDesktopApp = record.Value.IsDesktopApp;
                    stored.Add(mapping);
                }
            }

            return stored;
        }

        public void RestoreSettingsRepresentation(List<StoredMapping> stored)
        {
            Dictionary<Keys, VolumeTarget> resolved = new Dictionary<Keys, VolumeTarget>();

            if (stored != null)
            {
                foreach (StoredMapping record in stored)
                {
                    if (record.QualifiedId != null)
                    {
                        VolumeTarget target = CreateVolumeTarget(record.QualifiedId, _deviceManager.GetStreamById(record.QualifiedId));
                        target.Id = record.QualifiedId;
                        target.StreamName = record.StreamName;
                        target.ParentName = record.ParentName;
                        target.IconPath = record.IconPath;
                        target.IsDesktopApp = record.IsDesktopApp;

                        resolved.Add(record.Modifiers, target);
                    }
                }
            }

            _targets = resolved;
        }

        private ObservableCollection<VolumeTarget> _availableTargets = new ObservableCollection<VolumeTarget>();
        public ObservableCollection<VolumeTarget> AvailableTargets { 
            get
            {
                if (_availableTargets == null)
                {
                    BuildAvailableTargets();
                }

                return _availableTargets;
            }
        }

        private void AppendSession(IAudioDeviceSession session, List<VolumeTarget> collectedTargets, bool descend)
        {
            VolumeTarget target = CreateVolumeTarget(session.Id, session);
            target.StreamName = session.DisplayName;

            collectedTargets.Add(target);

            if (descend && session.Children != null)
            {
                foreach (IAudioDeviceSession child in session.Children)
                {
                    AppendSession(child, collectedTargets, descend);
                }
            }
        }

        private void BuildAvailableTargets()
        {
            List<VolumeTarget> result = new List<VolumeTarget>();

            result.Add(VolumeTarget.None);

            foreach(IAudioDevice targetDevice in _deviceManager.Devices)
            {
                VolumeTarget target = CreateVolumeTarget(targetDevice.Id, targetDevice);
                target.StreamName = targetDevice.DisplayName;
                result.Add(target);

                // Disabling the ability to target individual groups within a device right now,
                // because while it works perfectly *during* a run we have a lot of trouble bringing it back afterwards.
                if (targetDevice.Groups != null)
                {
                    foreach (IAudioDeviceSession session in targetDevice.Groups)
                    {
                        AppendSession(session, result, false);
                    }
                }
            }

            List<VolumeTarget> changed = new List<VolumeTarget>();

            foreach (KeyValuePair<string,VolumeTarget> check in _cached)
            {
                VolumeTarget checkMe = check.Value;
                if (!result.Contains(checkMe))
                {
                    changed.Add(checkMe);
                }
            }

            foreach (VolumeTarget checkMe in result)
            {
                if (!_availableTargets.Contains(checkMe))
                {
                    changed.Add(checkMe);
                }
            }

            foreach (VolumeTarget checkMe in changed)
            {
                checkMe.RefreshStream(forced: true);
            }

            _availableTargets.Clear();
            foreach (VolumeTarget checkMe in result)
            {
                _availableTargets.Add(checkMe);
            }

            RaisePropertyChanged("AvailableTargets");
        }

        public Keys[] RegisteredModifiers
        {
            get => _targets.Keys.ToArray<Keys>();
        }
    }
}
