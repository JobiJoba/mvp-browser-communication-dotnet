using System.Text.Json;
using Microsoft.JSInterop;

namespace MvpCaseSidePanelBroadcast.Services.SidePanel;

/// <summary>
/// Persists this window's side-panel list in sessionStorage so refresh keeps cases.
/// sessionStorage is per-tab, so two open windows stay independent.
/// </summary>
public sealed class SidePanelSessionStore : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private SidePanelState? _panel;
    private bool _initialized;

    public SidePanelSessionStore(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Restore any saved snapshot into <paramref name="panel"/>, then auto-save on changes.
    /// Safe to call once after the first interactive render (needs JSInterop).
    /// Skips restore if the panel already has links (e.g. user generated before JS was ready).
    /// </summary>
    public async Task InitializeAsync(SidePanelState panel)
    {
        if (_initialized)
        {
            return;
        }

        _panel = panel;
        var snapshot = await LoadAsync();
        if (snapshot is not null && panel.Links.Count == 0)
        {
            panel.Restore(snapshot);
        }

        panel.Changed += OnPanelChanged;
        _initialized = true;

        // Persist current state even if restore was skipped or Changed already fired.
        await SaveAsync(panel.ToSnapshot());
    }

    public async Task<SidePanelSnapshot?> LoadAsync()
    {
        await EnsureModuleAsync();
        var json = await _module!.InvokeAsync<string?>("loadSnapshot");
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SidePanelSnapshot>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(SidePanelSnapshot snapshot)
    {
        await EnsureModuleAsync();
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await _module!.InvokeVoidAsync("saveSnapshot", json);
    }

    private async Task EnsureModuleAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/sidePanelSession.js");
    }

    private void OnPanelChanged()
    {
        if (_panel is null)
        {
            return;
        }

        _ = SaveAsync(_panel.ToSnapshot());
    }

    public async ValueTask DisposeAsync()
    {
        if (_panel is not null)
        {
            _panel.Changed -= OnPanelChanged;
            _panel = null;
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            finally
            {
                _module = null;
            }
        }

        _initialized = false;
    }
}
