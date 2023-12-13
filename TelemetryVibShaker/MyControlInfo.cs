using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryVibShaker
{
    internal class MyControlInfo
    {
        public string ControlName;
        public Type ControlType; // string or int
        public object ControlValue;  // .Text or .Value or .SelectedIndex
        public string PropertyTemplate;
        
        public MyControlInfo(Control control)
        {
            string sType = "";
            int iType = 0;
            bool bType = false;

            ControlName = control.Name;
            ControlValue = null;
            ControlType = null;


            if (control is TextBox)
            {
                ControlValue = (control as TextBox).Text;
                ControlType = sType.GetType();
                PropertyTemplate = "genericString";
            }
            else if (control is CheckBox)
            {
                ControlValue = (control as CheckBox).Checked;
                ControlType = bType.GetType();
                PropertyTemplate = "genericBool";
            }
            else if (control is ComboBox)
            {
                ControlValue = (control as ComboBox).SelectedIndex;
                ControlType = iType.GetType();
                PropertyTemplate = "genericInt";
            }
            else if (control is TrackBar)
            {
                ControlValue = (control as TrackBar).Value;
                ControlType = iType.GetType();
                PropertyTemplate = "genericInt";
            }
            else if (control is NumericUpDown)
            {
                ControlValue = (control as NumericUpDown).Value;
                ControlType = iType.GetType();
                PropertyTemplate = "genericInt";
            }
            // You can add more cases for other types of controls

        }
    }
}
