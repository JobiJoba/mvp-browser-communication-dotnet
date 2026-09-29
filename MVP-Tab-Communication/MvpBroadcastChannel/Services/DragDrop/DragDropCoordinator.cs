using MvpBroadcastChannel.Models;
using MvpBroadcastChannel.Services.CrossTab;

namespace MvpBroadcastChannel.Services.DragDrop;

public sealed record ActiveDrag(string SessionId, string SourceTabId, DragPayload Payload);

public sealed class DragDropCoordinator : IAsyncDisposable
{
    private const int SchemaVersion = 1;

    private readonly ICrossTabBus _bus;
    private bool _listening;
    private string? _originSessionId;
    private bool _originCompleted;

    public DragDropCoordinator(ICrossTabBus bus)
    {
        _bus = bus;
    }

    public string TabId => _bus.TabId;

    public ActiveDrag? RemoteDrag { get; private set; }

    public ActiveDrag? LocalDrag { get; private set; }

    public bool HasRemoteDrag => RemoteDrag is not null;

    public Func<Func<Task>, Task>? Dispatcher { get; set; }

    public event Func<Task>? Changed;

    /// <summary>Raised on every tab that receives a DropAccepted (move shared item into zone).</summary>
    public event Func<string /*itemId*/, string /*zoneId*/, Task>? ItemMoved;

    public async Task StartAsync()
    {
        await _bus.EnsureStartedAsync();
        if (_listening)
        {
            return;
        }

        _bus.MessageReceived += OnBusMessageAsync;
        _listening = true;
    }

    public async Task BeginLocalDragAsync(BoardItem item)
    {
        await StartAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        var payload = new DragPayload(item.Id, item.Title, item.Color, item.ZoneId);
        LocalDrag = new ActiveDrag(sessionId, TabId, payload);
        _originSessionId = sessionId;
        _originCompleted = false;

        await _bus.PublishAsync(new CrossTabEnvelope(
            SchemaVersion,
            CrossTabMessageType.DragStarted,
            sessionId,
            TabId,
            TargetZoneId: null,
            payload));

        await NotifyChangedAsync();
    }

    public async Task EndLocalDragAsync()
    {
        if (_originSessionId is null)
        {
            return;
        }

        var sessionId = _originSessionId;
        LocalDrag = null;

        // Allow a remote DropAccepted to arrive before treating this as a cancel.
        await Task.Delay(150);

        if (_originCompleted || _originSessionId != sessionId)
        {
            await NotifyChangedAsync();
            return;
        }

        _originSessionId = null;
        await _bus.PublishAsync(new CrossTabEnvelope(
            SchemaVersion,
            CrossTabMessageType.DragCancelled,
            sessionId,
            TabId,
            TargetZoneId: null,
            Payload: null));

        await NotifyChangedAsync();
    }

    public async Task AcceptRemoteDropAsync(string targetZoneId)
    {
        if (RemoteDrag is null)
        {
            return;
        }

        var drag = RemoteDrag;
        RemoteDrag = null;

        await _bus.PublishAsync(new CrossTabEnvelope(
            SchemaVersion,
            CrossTabMessageType.DropAccepted,
            drag.SessionId,
            TabId,
            targetZoneId,
            drag.Payload));

        await NotifyChangedAsync();
    }

    public async Task AcceptLocalDropAsync(string targetZoneId)
    {
        if (LocalDrag is null)
        {
            return;
        }

        _originCompleted = true;
        var drag = LocalDrag;
        LocalDrag = null;
        _originSessionId = null;

        await _bus.PublishAsync(new CrossTabEnvelope(
            SchemaVersion,
            CrossTabMessageType.DropAccepted,
            drag.SessionId,
            TabId,
            targetZoneId,
            drag.Payload));

        await NotifyChangedAsync();
    }

    private async Task OnBusMessageAsync(CrossTabEnvelope envelope)
    {
        await DispatchAsync(async () =>
        {
            switch (envelope.Type)
            {
                case CrossTabMessageType.DragStarted when envelope.Payload is not null:
                    RemoteDrag = new ActiveDrag(envelope.SessionId, envelope.TabId, envelope.Payload);
                    await NotifyChangedAsync();
                    break;

                case CrossTabMessageType.DragCancelled:
                    if (RemoteDrag?.SessionId == envelope.SessionId)
                    {
                        RemoteDrag = null;
                        await NotifyChangedAsync();
                    }
                    break;

                case CrossTabMessageType.DropAccepted:
                    if (RemoteDrag?.SessionId == envelope.SessionId)
                    {
                        RemoteDrag = null;
                    }

                    if (_originSessionId == envelope.SessionId)
                    {
                        _originCompleted = true;
                        LocalDrag = null;
                        _originSessionId = null;
                    }

                    if (envelope.Payload is not null
                        && envelope.TargetZoneId is not null
                        && ItemMoved is not null)
                    {
                        await ItemMoved.Invoke(envelope.Payload.ItemId, envelope.TargetZoneId);
                    }

                    await NotifyChangedAsync();
                    break;
            }
        });
    }

    private Task DispatchAsync(Func<Task> work) =>
        Dispatcher is not null ? Dispatcher(work) : work();

    private async Task NotifyChangedAsync()
    {
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_listening)
        {
            _bus.MessageReceived -= OnBusMessageAsync;
            _listening = false;
        }

        return ValueTask.CompletedTask;
    }
}
