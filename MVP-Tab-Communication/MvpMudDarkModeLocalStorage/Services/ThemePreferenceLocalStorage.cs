using Microsoft.JSInterop;
using MvpMudDarkModeLocalStorage.Models;

namespace MvpMudDarkModeLocalStorage.Services;

/// <summary>
/// Persists theme preference in localStorage. Only usable after the first interactive render
/// (JS interop), which is why this approach causes a FOUC on Blazor Server.
/// </summary>
public sealed class ThemePreferenceLocalStorage(IJSRuntime js)
{
    public const string StorageKey = "mud-theme-preference";

    public async Task<ThemePreference> ReadAsync()
    {
        var raw = await js.InvokeAsync<string?>("mudThemeLocalStorage.get", StorageKey);
        return Parse(raw);
    }

    public Task WriteAsync(ThemePreference preference) =>
        js.InvokeVoidAsync("mudThemeLocalStorage.set", StorageKey, preference.ToString()).AsTask();

    public static ThemePreference Parse(string? raw) =>
        Enum.TryParse<ThemePreference>(raw, ignoreCase: true, out var value)
            ? value
            : ThemePreference.System;
}
