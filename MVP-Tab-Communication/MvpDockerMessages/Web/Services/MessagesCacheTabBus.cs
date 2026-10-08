using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MvpDockerMessages.Web.Models;

namespace MvpDockerMessages.Web.Services;

/// <summary>
/// Same-browser BroadcastChannel bridge so a detail tab can patch the overview tab's cache
/// after Save (new Blazor circuit ≠ shared scoped DI).
/// </summary>
public sealed class MessagesCacheTabBus : IAsyncDisposable
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
    private DotNetObjectReference<MessagesCacheTabBus>? _selfRef;
    private bool _started;

    public MessagesCacheTabBus(IJSRuntime js) => _js = js;

    public event Func<MessageDto, Task>? MessageUpdated;

    public async Task EnsureStartedAsync()
    {
        if (_started)
        {
            return;
        }

        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./js/messagesCacheChannel.js");
        _channel = await _module.InvokeAsync<IJSObjectReference>("createChannel");
        _selfRef = DotNetObjectReference.Create(this);
        await _channel.InvokeVoidAsync("subscribe", _selfRef);
        _started = true;
    }

    public async Task PublishUpdatedAsync(MessageDto message)
    {
        await EnsureStartedAsync();
        var json = JsonSerializer.Serialize(
            new MessagesCacheEnvelope("messageUpdated", message),
            JsonOptions);
        await _channel!.InvokeVoidAsync("publish", json);
    }

    public async Task CloseThisTabAsync()
    {
        await EnsureStartedAsync();
        await _channel!.InvokeVoidAsync("closeTab");
    }

    [JSInvokable]
    public async Task OnBrowserMessage(string json)
    {
        var envelope = JsonSerializer.Deserialize<MessagesCacheEnvelope>(json, JsonOptions);
        if (envelope is null || envelope.Type != "messageUpdated" || envelope.Message is null)
        {
            return;
        }

        if (MessageUpdated is not null)
        {
            await MessageUpdated.Invoke(envelope.Message);
        }
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

    private sealed record MessagesCacheEnvelope(string Type, MessageDto? Message);
}
