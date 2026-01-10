using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProxyChecker.Dialogs.Models;

namespace ProxyChecker.Common;

[JsonSerializable(typeof(SettingModel))]
public partial class GlobalSettingContext : JsonSerializerContext
{
}

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

            var json = JsonSerializer.Serialize(Setting, GlobalSettingContext.Default.SettingModel);
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
                Setting = JsonSerializer.Deserialize(json, GlobalSettingContext.Default.SettingModel) ?? new SettingModel();
            }
        }
        catch
        {
            Setting = new SettingModel();
        }
    }
}
