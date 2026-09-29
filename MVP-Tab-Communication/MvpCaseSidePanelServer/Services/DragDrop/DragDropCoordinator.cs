using MvpCaseSidePanelServer.Models;
using MvpCaseSidePanelServer.Services.DragSession;

namespace MvpCaseSidePanelServer.Services.DragDrop;

public sealed record ActiveDrag(string SessionId, string SourceCircuitId, CaseLinkPayload Payload);

public sealed class DragDropCoordinator : IAsyncDisposable
{
    private readonly IDragSessionHub _hub;
    private IDisposable? _subscription;
    private string? _originSessionId;
    private bool _originCompleted;

    public DragDropCoordinator(IDragSessionHub hub)
    {
        _hub = hub;
        CircuitId = Guid.NewGuid().ToString("N");
    }

    public string CircuitId { get; }

    public ActiveDrag? RemoteDrag { get; private set; }

    public ActiveDrag? LocalDrag { get; private set; }

    public bool HasRemoteDrag => RemoteDrag is not null;

    public Func<Func<Task>, Task>? Dispatcher { get; set; }

    public event Func<Task>? Changed;

    public event Func<string /*caseId*/, Task>? LinkRemovedRemotely;

    public void Start()
    {
        _subscription ??= _hub.Subscribe(CircuitId, OnHubEventAsync);
    }

    public async Task BeginLocalDragAsync(CaseLink link)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var payload = new CaseLinkPayload(link.CaseId, link.DisplayName);
        LocalDrag = new ActiveDrag(sessionId, CircuitId, payload);
        _originSessionId = sessionId;
        _originCompleted = false;
        await _hub.BeginDragAsync(CircuitId, sessionId, payload);
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

        await Task.Delay(150);

        if (_originCompleted || _originSessionId != sessionId)
        {
            await NotifyChangedAsync();
            return;
        }

        _originSessionId = null;
        await _hub.CancelDragAsync(CircuitId, sessionId);
        await NotifyChangedAsync();
    }

    public async Task AcceptRemoteDropAsync()
    {
        if (RemoteDrag is null)
        {
            return;
        }

        var drag = RemoteDrag;
        RemoteDrag = null;
        await _hub.AcceptDropAsync(CircuitId, drag.SessionId, drag.Payload);
        await NotifyChangedAsync();
    }

    public async Task AcceptLocalDropAsync()
    {
        if (LocalDrag is null)
        {
            return;
        }

        _originCompleted = true;
        var drag = LocalDrag;
        LocalDrag = null;
        _originSessionId = null;
        await _hub.AcceptDropAsync(CircuitId, drag.SessionId, drag.Payload);
        await NotifyChangedAsync();
    }

    private async Task OnHubEventAsync(DragSessionEvent evt)
    {
        await DispatchAsync(async () =>
        {
            switch (evt.Kind)
            {
                case DragEventKind.Started when evt.SourceCircuitId != CircuitId && evt.Payload is not null:
                    RemoteDrag = new ActiveDrag(evt.SessionId, evt.SourceCircuitId, evt.Payload);
                    await NotifyChangedAsync();
                    break;

                case DragEventKind.Cancelled:
                    if (RemoteDrag?.SessionId == evt.SessionId)
                    {
                        RemoteDrag = null;
                        await NotifyChangedAsync();
                    }
                    break;

                case DragEventKind.DropAccepted:
                    if (RemoteDrag?.SessionId == evt.SessionId)
                    {
                        RemoteDrag = null;
                    }

                    if (_originSessionId == evt.SessionId)
                    {
                        _originCompleted = true;
                        LocalDrag = null;
                        _originSessionId = null;
                    }

                    if (evt.SourceCircuitId == CircuitId
                        && evt.TargetCircuitId != CircuitId
                        && evt.Payload is not null
                        && LinkRemovedRemotely is not null)
                    {
                        await LinkRemovedRemotely.Invoke(evt.Payload.CaseId);
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
        _subscription?.Dispose();
        _subscription = null;
        return ValueTask.CompletedTask;
    }
}
