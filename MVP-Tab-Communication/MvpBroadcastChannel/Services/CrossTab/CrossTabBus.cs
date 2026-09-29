using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MvpBroadcastChannel.Models;

namespace MvpBroadcastChannel.Services.CrossTab;

public sealed class CrossTabBus : ICrossTabBus
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private IJSObjectReference? _channel;
    private DotNetObjectReference<CrossTabBus>? _selfRef;
    private bool _started;

    public CrossTabBus(IJSRuntime js)
    {
        _js = js;
    }

    public string TabId { get; private set; } = string.Empty;

    public event Func<CrossTabEnvelope, Task>? MessageReceived;

    public async Task EnsureStartedAsync()
    {
        if (_started)
        {
            return;
        }

        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./js/crossTabChannel.js");
        _channel = await _module.InvokeAsync<IJSObjectReference>("createChannel");
        TabId = await _channel.InvokeAsync<string>("getTabId");

        _selfRef = DotNetObjectReference.Create(this);
        await _channel.InvokeVoidAsync("subscribe", _selfRef);
        _started = true;
    }

    public async Task PublishAsync(CrossTabEnvelope message)
    {
        await EnsureStartedAsync();
        var enriched = message with { TabId = TabId };
        var json = JsonSerializer.Serialize(enriched, JsonOptions);
        await _channel!.InvokeVoidAsync("publish", json);
    }

    [JSInvokable]
    public async Task OnBrowserMessage(string json)
    {
        var envelope = JsonSerializer.Deserialize<CrossTabEnvelope>(json, JsonOptions);
        if (envelope is null || MessageReceived is null)
        {
            return;
        }

        await MessageReceived.Invoke(envelope);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_started && _module is null && _channel is null)
        {
            return;
        }

        try
        {
            if (_channel is not null)
            {
                await _channel.InvokeVoidAsync("dispose");
                await _channel.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone.
        }
        finally
        {
            _selfRef?.Dispose();
            _selfRef = null;
            _channel = null;
            _module = null;
            _started = false;
        }
    }
}
