using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DevCardsManager.ViewModels;

namespace DevCardsManager;

internal static class Mapper
{
    public static List<ParameterViewModel> ToParameters(this Settings settings)
    {
        var parameters = new List<ParameterViewModel>();

        foreach (var propertyInfo in settings.GetType().GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null))
        {
            var propertyValue = propertyInfo.GetValue(settings);
            var displayName = propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? propertyInfo.Name;

            ParameterViewModel parameter = propertyValue switch
            {
                int value => new IntegerParameterViewModel(propertyInfo.Name, displayName, value),
                string value => new StringParameterViewModel(propertyInfo.Name, displayName, value),
                bool value => new BooleanParameterViewModel(propertyInfo.Name, displayName, value),
                _ => throw new ArgumentOutOfRangeException()
            };

            parameters.Add(parameter);
        }

        return parameters;
    }

    public static Settings ToSettings(this IList<ParameterViewModel> parameters, List<string> pinnedCards)
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
            SaveCardChangesOnReturn = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.SaveCardChangesOnReturn))).Value,
            DetailedLogging = ((BooleanParameterViewModel)parameters.Single(p => p.PropertyName == nameof(Settings.DetailedLogging))).Value,
            PinnedCards = pinnedCards
        };

        return settings;
    }
}