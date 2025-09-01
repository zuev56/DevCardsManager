using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DevCardsManager.Extensions;
using DevCardsManager.Models;
using DevCardsManager.ViewModels;
using Microsoft.Extensions.Configuration;

namespace DevCardsManager.Services;

public sealed class SettingsManager
{
    private Settings _settingsAfterSave;
    private readonly Logger _logger;

    public event Action<string>? ParameterChanged;

    public SettingsManager(Logger logger)
    {
        _logger = logger;

        Settings = ReadSettingsFile();
        _settingsAfterSave = ReadSettingsFile();
    }

    public Settings Settings { get; private set; }

    private static Settings ReadSettingsFile()
        => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Settings.FileName, optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build()
            .Get<Settings>()!;

    internal void SaveSettings(Settings? settings = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Settings = settings ?? Settings;
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), Settings.FileName);
            var settingsJson = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(settingsPath, settingsJson);

            RaiseParameterChanged();
            _settingsAfterSave = Settings.Duplicate()!;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
        finally
        {
            _logger.LogPerformance(stopwatch.Elapsed);
        }
    }

    private void RaiseParameterChanged()
    {
        var changedParameters = new List<string>();

        var properties = typeof(Settings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string));

        foreach (var property in properties)
        {
            var value1 = property.GetValue(Settings);
            var value2 = property.GetValue(_settingsAfterSave);

            if (!AreEqual(value1, value2))
                changedParameters.Add(property.Name);
        }

        foreach (var parameterName in changedParameters)
            ParameterChanged?.Invoke(parameterName);

        return;

        static bool AreEqual(object? value1, object? value2)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;

            return value1.Equals(value2);
        }
    }
}