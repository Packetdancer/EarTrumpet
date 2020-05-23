using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarTrumpet.DataModel.Audio
{
    public interface INamedStreamWithVolumeControl : IStreamWithVolumeControl
    {
        string DisplayName { get; }
        string IconPath { get; }
        bool IsDesktopApp { get; }
        bool IsChild { get;  }

    }
}
