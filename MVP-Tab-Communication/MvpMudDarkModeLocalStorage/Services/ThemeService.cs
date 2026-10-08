using MvpMudDarkModeLocalStorage.Models;

namespace MvpMudDarkModeLocalStorage.Services;

public sealed class ThemeService(ThemePreferenceLocalStorage storage)
{
    private ThemePreference _preference = ThemePreference.System;
    private bool _isDarkMode;
    private bool _hydrated;

    public ThemePreference Preference => _preference;

    public bool IsDarkMode => _isDarkMode;

    public bool ObserveSystemDarkModeChange => _preference == ThemePreference.System;

    /// <summary>
    /// True after localStorage has been read on the client. Until then the UI shows the default (light).
    /// </summary>
    public bool IsHydrated => _hydrated;

    public event Action? Changed;

    public async Task HydrateFromLocalStorageAsync(Func<Task<bool>> getSystemDarkModeAsync)
    {
        if (_hydrated)
        {
            return;
        }

        _preference = await storage.ReadAsync();
        await ApplyResolvedDarkModeAsync(getSystemDarkModeAsync);
        _hydrated = true;
        Changed?.Invoke();
    }

    public async Task SetPreferenceAsync(ThemePreference preference, Func<Task<bool>>? getSystemDarkModeAsync = null)
    {
        _preference = preference;
        await ApplyResolvedDarkModeAsync(getSystemDarkModeAsync);
        await storage.WriteAsync(preference);
        Changed?.Invoke();
    }

    public void ApplySystemDarkMode(bool systemIsDark)
    {
        if (_preference != ThemePreference.System)
        {
            return;
        }

        if (_isDarkMode == systemIsDark)
        {
            return;
        }

        _isDarkMode = systemIsDark;
        Changed?.Invoke();
    }

    private async Task ApplyResolvedDarkModeAsync(Func<Task<bool>>? getSystemDarkModeAsync)
    {
        _isDarkMode = _preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            ThemePreference.System when getSystemDarkModeAsync is not null => await getSystemDarkModeAsync(),
            _ => _isDarkMode
        };
    }
}
