using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using EarTrumpet.DataModel.Audio;

namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetVolumeTargetSettingsPageViewModel : SettingsPageViewModel
    {
        private readonly VolumeTargetMap _targets;
        private readonly AppSettings _settings;
        private ObservableCollection<VolumeTargetMap.VolumeTarget> _availableTargets;
        private Action<Keys, VolumeTargetMap.VolumeTarget> _save;

        public ObservableCollection<VolumeTargetViewModel> AvailableModifierBindings { get; set; }
        
        public EarTrumpetVolumeTargetSettingsPageViewModel(AppSettings settings, VolumeTargetMap targetMap) : base(null)
        {
            _targets = targetMap;
            _settings = settings;
            _availableTargets = new ObservableCollection<VolumeTargetMap.VolumeTarget>(_targets.AvailableTargets);

            Glyph = "\xE15D";
            Title = Properties.Resources.VolumeTargetMenuText;

            _save = (modifiers, target) => { if (target != null) { _settings.SetVolumeTargetForModifiers(modifiers, target); } };

            AvailableModifierBindings = new ObservableCollection<VolumeTargetViewModel>();

            AvailableModifierBindings.Add(CreateTargetViewModel(Keys.None, Properties.Resources.SettingsVolumeTargetNone));
            AvailableModifierBindings.Add(CreateTargetViewModel(Keys.Control, Properties.Resources.SettingsVolumeTargetControl));
            AvailableModifierBindings.Add(CreateTargetViewModel(Keys.Shift, Properties.Resources.SettingsVolumeTargetShift));
            AvailableModifierBindings.Add(CreateTargetViewModel(Keys.Alt, Properties.Resources.SettingsVolumeTargetAlt));
        }

        private VolumeTargetViewModel CreateTargetViewModel(Keys modifiers, string label)
        {
            VolumeTargetMap.VolumeTarget target = _targets.TargetForModifiers(modifiers);
            if (target == null)
            {
                target = _availableTargets.ElementAt(0);
            }

            return new VolumeTargetViewModel(modifiers, label, target, _save);
        }


    }
}
