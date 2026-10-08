using Microsoft.JSInterop;
using MvpMudDarkModeCookie.Models;

namespace MvpMudDarkModeCookie.Services;

/// <summary>
/// Reads the theme preference from the incoming request cookie (available during prerender)
/// and writes it back via JS so the next navigation paints correctly without FOUC.
/// </summary>
public sealed class ThemePreferenceCookie(IHttpContextAccessor httpContextAccessor, IJSRuntime js)
{
    public const string CookieName = "mud-theme-preference";

    public ThemePreference Read()
    {
        var raw = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        return Parse(raw);
    }

    public Task WriteAsync(ThemePreference preference) =>
        js.InvokeVoidAsync("mudThemeCookie.set", CookieName, preference.ToString()).AsTask();

    public static ThemePreference Parse(string? raw) =>
        Enum.TryParse<ThemePreference>(raw, ignoreCase: true, out var value)
            ? value
            : ThemePreference.System;
}
