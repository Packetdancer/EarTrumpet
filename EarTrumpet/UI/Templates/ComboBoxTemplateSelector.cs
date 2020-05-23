using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EarTrumpet.UI.Templates
{
    class ComboBoxTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FaceItemTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            bool isFace = false;

            FrameworkElement fe = container as FrameworkElement;
            if (fe != null)
            {
                DependencyObject parent = fe.TemplatedParent;
                if (parent != null)
                {
                    ComboBox cbo = parent as ComboBox;
                    if (cbo != null)
                        isFace = true;
                }
            }

            if (isFace)
                return FaceItemTemplate;
            else
                return ItemTemplate;
        }
    }
}
