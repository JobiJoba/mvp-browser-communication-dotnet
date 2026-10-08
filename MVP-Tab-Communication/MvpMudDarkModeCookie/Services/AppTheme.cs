using MudBlazor;
using MudBlazor.Utilities;

namespace MvpMudDarkModeCookie.Services;

public static class AppTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = new MudColor("#1565C0"),
            Secondary = new MudColor("#00838F"),
            AppbarBackground = new MudColor("#1565C0"),
            Background = new MudColor("#F5F7FA"),
            Surface = new MudColor("#FFFFFF"),
            DrawerBackground = new MudColor("#FFFFFF"),
            TextPrimary = new MudColor("#1A1A1A"),
            TextSecondary = new MudColor("#5C5C5C")
        },
        PaletteDark = new PaletteDark
        {
            Primary = new MudColor("#90CAF9"),
            Secondary = new MudColor("#80DEEA"),
            AppbarBackground = new MudColor("#1E1E1E"),
            Background = new MudColor("#121212"),
            Surface = new MudColor("#1E1E1E"),
            DrawerBackground = new MudColor("#1E1E1E"),
            TextPrimary = new MudColor("#E0E0E0"),
            TextSecondary = new MudColor("#B0B0B0")
        }
    };
}
