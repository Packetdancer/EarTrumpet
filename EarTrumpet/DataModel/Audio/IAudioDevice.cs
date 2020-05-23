using System;
using System.Collections.ObjectModel;

namespace EarTrumpet.DataModel.Audio
{
    public interface IAudioDevice : INamedStreamWithVolumeControl
    {
        IAudioDeviceManager Parent { get; }
        ObservableCollection<IAudioDeviceSession> Groups { get; }
        void AddFilter(Func<ObservableCollection<IAudioDeviceSession>, ObservableCollection<IAudioDeviceSession>> filter);
    }
}