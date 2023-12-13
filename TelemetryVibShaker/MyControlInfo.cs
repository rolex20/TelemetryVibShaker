using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    internal class MyControlInfo
    {
        public string Name;
        public Type Type; // string or int to store in Settings
        public object Value;  // .Text or .Value or .SelectedIndex
        private Type originalControlType;
        private Control control;


        
        public void AssignValue(object value)
        {
            if (control is TextBox)
            {
                (control as TextBox).Text = (string)value;
            }
            else if (control is CheckBox)
            {
                (control as CheckBox).Checked = (bool)value;
            }
            else if (control is ComboBox)
            {
                (control as ComboBox).SelectedIndex = (int)value;
            }
            else if (control is TrackBar)
            {
                (control as TrackBar).Value = (int)value;
            }
            else if (control is NumericUpDown)
            {
                (control as NumericUpDown).Value = (int)value;
            } else
            {
                throw new NotImplementedException("You need to add control [" + Name + "] to MyControlInfo::AssignValue()");
            }
            // You can add more cases for other types of controls

        }

        public MyControlInfo(Control ctrl)
        {
            string sType = "";
            int iType = 0;
            bool bType = false;
            decimal dType = 0;

            control = ctrl;

            Name = control.Name;
            originalControlType = control.GetType();
            Value = null;
            Type = null;

            if (control is TextBox)
            {
                Value = (control as TextBox).Text;
                Type = sType.GetType();                
            }
            else if (control is CheckBox)
            {
                Value = (control as CheckBox).Checked;
                Type = bType.GetType();
            }
            else if (control is ComboBox)
            {
                Value = (control as ComboBox).SelectedIndex;
                Type = iType.GetType();
            }
            else if (control is TrackBar)
            {
                Value = (control as TrackBar).Value;
                Type = iType.GetType();
            }
            else if (control is NumericUpDown)
            {
                Value = (int)(control as NumericUpDown).Value;
                Type = iType.GetType();
            }
            // You can add more cases for other types of controls

        }
    }
}
