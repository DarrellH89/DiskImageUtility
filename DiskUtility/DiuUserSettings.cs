using System;
using System.IO;
using System.Configuration;
using System.Drawing;

public class DiuUserSettings : ApplicationSettingsBase
{
    [UserScopedSetting()]
    [DefaultSettingValue("")]
    public String SelectedPath
    {
        get
        {
            return ((String)this["SelectedPath"]);
        }
        set
        {
            this["SelectedPath"] = (String)value;
        }
    }
    [UserScopedSetting()]
    [DefaultSettingValue(" ")]
    public String options
    {
        get
        {
            return ((String)this["options"]);
        }
        set
        {
            this["options"] = (String)value;
        }
    }
    [UserScopedSetting()]
    [DefaultSettingValue("")]
    public String addFilesLoc
    {
        get
        {
            return ((String)this["addFilesLoc"]);
        }
        set
        {
            this["addFilesLoc"] = (String)value;
        }
    }
}

