using System;
using System.IO;
using System.Text.Json;
using ProxyChecker.Dialogs.Models;

namespace ProxyChecker.Common;

public class GlobalSetting
{
    private static readonly string SettingPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProxyChecker",
        "settings.json");

    public static GlobalSetting Instance { get; } = new();

    public SettingModel Setting { get; private set; } = new();

    private GlobalSetting()
    {
        Load();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(Setting, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingPath, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingPath))
            {
                var json = File.ReadAllText(SettingPath);
                Setting = JsonSerializer.Deserialize<SettingModel>(json) ?? new SettingModel();
            }
        }
        catch
        {
            Setting = new SettingModel();
        }
    }
}
