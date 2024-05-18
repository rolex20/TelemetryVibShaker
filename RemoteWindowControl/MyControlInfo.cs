

using System.Windows.Forms;
using System;

namespace RemoteWindowControl
{
    internal class MyControlInfo
    {
        public string Name;
        public Type _Type; // string or int to store in Settings
        public object Value;  // .Text or .Value or .SelectedIndex
        private Type originalControlType;
        private Control control;



        public void AssignValue(object value)
        {
#pragma warning disable CS8602
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
                int v = (int)value;
                int c = (int)(control as ComboBox).Items.Count;

                if (v < c) (control as ComboBox).SelectedIndex = v;
            }
            else if (control is TrackBar)
            {
                (control as TrackBar).Value = (int)value;
            }
            else if (control is NumericUpDown)
            {
                (control as NumericUpDown).Value = (int)value;
            }
            else
            {
                throw new NotImplementedException("You need to add control [" + Name + "] to MyControlInfo::AssignValue()");
            }
            // You can add more cases for other types of controls
#pragma warning restore CS8602
        }

        public MyControlInfo(Control ctrl)
        {
            string sType = "";
            int iType = 0;
            bool bType = false;

            control = ctrl;

            Name = control.Name;
            originalControlType = control.GetType();
            Value = null;
            _Type = null;

#pragma warning disable CS8602

            if (control is TextBox)
            {
                Value = (control as TextBox).Text;
                _Type = sType.GetType();
            }
            else if (control is CheckBox)
            {
                Value = (control as CheckBox).Checked;
                _Type = bType.GetType();
            }
            else if (control is ComboBox)
            {
                Value = (control as ComboBox).SelectedIndex;
                _Type = iType.GetType();
            }
            else if (control is TrackBar)
            {
                Value = (control as TrackBar).Value;
                _Type = iType.GetType();
            }
            else if (control is NumericUpDown)
            {
                Value = (int)(control as NumericUpDown).Value;
                _Type = iType.GetType();
            }
            // You can add more cases for other types of controls
#pragma warning restore CS8602
        }
    }
}
