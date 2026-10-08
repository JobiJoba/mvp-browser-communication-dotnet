using MvpMudDarkModeCookie.Models;

namespace MvpMudDarkModeCookie.Services;

public sealed class ThemeService(ThemePreferenceCookie cookieStore)
{
    private ThemePreference _preference;
    private bool _isDarkMode;
    private bool _initialized;

    public ThemePreference Preference => _preference;

    public bool IsDarkMode => _isDarkMode;

    public bool ObserveSystemDarkModeChange => _preference == ThemePreference.System;

    public event Action? Changed;

    /// <summary>
    /// Loads preference from the request cookie. Safe during prerender / OnInitialized.
    /// For System mode, IsDarkMode stays at the last known / default until the layout
    /// resolves the OS preference (or an early head script already set dark).
    /// </summary>
    public void InitializeFromCookie()
    {
        if (_initialized)
        {
            return;
        }

        _preference = cookieStore.Read();
        _isDarkMode = _preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            // System: optimistic default; layout will refine via GetSystemPreference once interactive.
            _ => false
        };
        _initialized = true;
    }

    public async Task SetPreferenceAsync(ThemePreference preference, bool? systemIsDark = null)
    {
        _preference = preference;
        ApplyResolvedDarkMode(systemIsDark);
        await cookieStore.WriteAsync(preference);
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

    public void SetIsDarkMode(bool isDarkMode)
    {
        if (_isDarkMode == isDarkMode)
        {
            return;
        }

        _isDarkMode = isDarkMode;
        Changed?.Invoke();
    }

    private void ApplyResolvedDarkMode(bool? systemIsDark)
    {
        _isDarkMode = _preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            ThemePreference.System => systemIsDark ?? _isDarkMode,
            _ => false
        };
    }
}
