﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SimConnectExporter.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.11.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("192.168.1.5")]
        public string txtDestinationHostname {
            get {
                return ((string)(this["txtDestinationHostname"]));
            }
            set {
                this["txtDestinationHostname"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("54671")]
        public string txtDestinationPort {
            get {
                return ((string)(this["txtDestinationPort"]));
            }
            set {
                this["txtDestinationPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int XCoordinate {
            get {
                return ((int)(this["XCoordinate"]));
            }
            set {
                this["XCoordinate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int YCoordinate {
            get {
                return ((int)(this["YCoordinate"]));
            }
            set {
                this["YCoordinate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool chkUseEfficiencyCoresOnly {
            get {
                return ((bool)(this["chkUseEfficiencyCoresOnly"]));
            }
            set {
                this["chkUseEfficiencyCoresOnly"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool chkUseBackgroundProcessing {
            get {
                return ((bool)(this["chkUseBackgroundProcessing"]));
            }
            set {
                this["chkUseBackgroundProcessing"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int cmbPriorityClass {
            get {
                return ((int)(this["cmbPriorityClass"]));
            }
            set {
                this["cmbPriorityClass"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool chkChangeToMonitor {
            get {
                return ((bool)(this["chkChangeToMonitor"]));
            }
            set {
                this["chkChangeToMonitor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool chkShowStatistics {
            get {
                return ((bool)(this["chkShowStatistics"]));
            }
            set {
                this["chkShowStatistics"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool chkTrackGForces {
            get {
                return ((bool)(this["chkTrackGForces"]));
            }
            set {
                this["chkTrackGForces"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("300")]
        public int nudFrequency {
            get {
                return ((int)(this["nudFrequency"]));
            }
            set {
                this["nudFrequency"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int cmbSimConnectPeriod {
            get {
                return ((int)(this["cmbSimConnectPeriod"]));
            }
            set {
                this["cmbSimConnectPeriod"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool chkAutoMinimize {
            get {
                return ((bool)(this["chkAutoMinimize"]));
            }
            set {
                this["chkAutoMinimize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool chkReassignIdealProcessor {
            get {
                return ((bool)(this["chkReassignIdealProcessor"]));
            }
            set {
                this["chkReassignIdealProcessor"] = value;
            }
        }
    }
}
