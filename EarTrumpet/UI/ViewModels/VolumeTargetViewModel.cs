using EarTrumpet.DataModel.Audio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace EarTrumpet.UI.ViewModels
{
    public class VolumeTargetViewModel : BindableBase
    {
        private readonly Keys _modifier;
        private Action<Keys, VolumeTargetMap.VolumeTarget> _save;
        private VolumeTargetMap.VolumeTarget _target;
        private ObservableCollection<VolumeTargetMap.VolumeTarget> _effectiveTargets;

        public string Label { get; set; }

        public VolumeTargetViewModel(Keys modifier, string label, VolumeTargetMap.VolumeTarget currentTarget, Action<Keys, VolumeTargetMap.VolumeTarget> save)
        {
            _modifier = modifier;
            _save = save;
            Label = label;
            SelectedTarget = currentTarget;
            VolumeTargetMap.SharedMap.AvailableTargets.CollectionChanged += this.Targets_CollectionChanged;
        }

        ~VolumeTargetViewModel()
        {
            _target.PropertyChanged -= this.Target_PropertyChanged;
            VolumeTargetMap.SharedMap.AvailableTargets.CollectionChanged -= this.Targets_CollectionChanged;
        }

        private void EnsureEffectiveTargets()
        {
            VolumeTargetMap targetMap = VolumeTargetMap.SharedMap;
            _effectiveTargets = new ObservableCollection<VolumeTargetMap.VolumeTarget>(targetMap.AvailableTargets);
            if (!_effectiveTargets.Contains(_target))
            {
                _effectiveTargets.Insert(0, _target);
            }
        }

        public VolumeTargetMap.VolumeTarget SelectedTarget {
            get => _target;
            set
            {
                VolumeTargetMap.VolumeTarget old = _target;

                _target = value;
                _save(_modifier, value);

                EnsureEffectiveTargets();

                if (old != null)
                {
                    old.PropertyChanged -= this.Target_PropertyChanged;
                }

                _target.PropertyChanged += this.Target_PropertyChanged;

                RaisePropertyChanged("SelectedTarget");
                RaisePropertyChanged("SelectedIndex");
            }
        }

        private void Target_PropertyChanged(object o, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("SelectedTarget");
        }

        private void Targets_CollectionChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            EnsureEffectiveTargets();
            RaisePropertyChanged("AvailableTargets");
        }

        public int SelectedIndex
        {
            get => _effectiveTargets.IndexOf(_target);
            set {
                if (value == -1)
                {
                    _target = null;
                    return;
                }

                SelectedTarget = _effectiveTargets.ElementAt(value);
                RaisePropertyChanged("SelectedIndex");
            }
        }

        public ObservableCollection<VolumeTargetMap.VolumeTarget> AvailableTargets
        {
            get => _effectiveTargets;
        }


    }
}
