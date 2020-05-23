using EarTrumpet.DataModel.WindowsAudio;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EarTrumpet.DataModel.Audio
{
    public interface IAudioDeviceSession : INamedStreamWithVolumeControl
    {
        IEnumerable<IAudioDeviceSessionChannel> Channels { get; }
        IAudioDevice Parent { get; }
        string ExeName { get; }
        uint BackgroundColor { get; }
        bool IsSystemSoundsSession { get; }
        int ProcessId { get; }
        string AppId { get; }
        SessionState State { get; }
        ObservableCollection<IAudioDeviceSession> Children { get; }
    }
}