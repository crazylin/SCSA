using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SCSA.Models;

namespace SCSA.Services;

/// <summary>
///     负责应用级配置（appsettings.json）的读写。
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private const string CONFIG_FILE = "appsettings.json";
    private readonly string _configPath;

    public AppSettingsService()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception e)
        {
            // 读取失败时记录异常
            SCSA.Utils.Log.Error("Failed to load app settings", e);
        }

        return GetDefault();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception e)
        {
            // 写入失败时记录异常
            SCSA.Utils.Log.Error("Failed to save app settings", e);
        }
    }

    private static AppSettings GetDefault()
    {
        return new AppSettings
        {
            DataStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SCSA")
        };
    }
}