using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DevCardsManager.Models;
using DevCardsManager.Ui.Controls;

namespace DevCardsManager.Services;

internal static class Mapper
{
    public static List<ParameterViewModel> ToParameters(this Settings settings, Logger logger)
    {
        var parameters = new List<ParameterViewModel>();

        foreach (var propertyInfo in settings.GetType().GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null))
        {
            var propertyValue = propertyInfo.GetValue(settings);
            var displayName = propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? propertyInfo.Name;

            if (propertyValue == null)
                logger.LogError($"Attempt to assign null to the '{propertyInfo.Name}' property");

            ParameterViewModel parameter = propertyValue switch
            {
                int value => new IntegerParameterViewModel(propertyInfo.Name, displayName, value),
                double value => new DoubleParameterViewModel(propertyInfo.Name, displayName, value),
                string value => new StringParameterViewModel(propertyInfo.Name, displayName, value),
                bool value => new BooleanParameterViewModel(propertyInfo.Name, displayName, value),
                _ => throw new ArgumentOutOfRangeException(nameof(propertyInfo.Name), propertyValue, "Invalid property value")
            };

            parameters.Add(parameter);
        }

        return parameters;
    }

    public static Settings ToSettings(this IList<ParameterViewModel> parameters, List<string> pinnedCards, List<LogItemPattern> patternsToRemove, List<LogItemPattern> patternsToLeave)
    {
        // TODO: надо сделать так, чтобы не приходилось прописывать каждое новое свойство
        var settings = new Settings
        {
            AllCardsPath = ((StringParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.AllCardsPath))).Value,
            InsertedCardPath = ((StringParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.InsertedCardPath))).Value,
            ReplaceCardDelayMs = ((IntegerParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.ReplaceCardDelayMs))).Value,
            InsertCardOnTimeMs = ((IntegerParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.InsertCardOnTimeMs))).Value,
            SortAscending = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.SortAscending))).Value,
            UseDarkTheme = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.UseDarkTheme))).Value,
            KeepOnTop = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.KeepOnTop))).Value,
            KeepOnTopTransparency = ((IntegerParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.KeepOnTopTransparency))).Value,
            SaveCardChangesOnReturn = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.SaveCardChangesOnReturn))).Value,
            DetailedLogging = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.DetailedLogging))).Value,
            PinnedCards = pinnedCards,
            LogRowPatternsToRemove = patternsToRemove,
            LogRowPatternsToLeave = patternsToLeave
        };

        return settings;
    }
}