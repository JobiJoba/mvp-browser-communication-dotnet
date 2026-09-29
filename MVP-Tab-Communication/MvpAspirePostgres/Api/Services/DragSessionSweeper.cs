namespace MvpAspirePostgres.Api.Services;

/// <summary>
/// Periodically removes expired drag_sessions and broadcasts Cancelled so remote
/// UIs clear a ghost "Remote drag active" after the source circuit dies mid-drag.
/// </summary>
public sealed class DragSessionSweeper(DragSessionService drag, ILogger<DragSessionSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
                var cleared = await drag.SweepExpiredAsync(stoppingToken).ConfigureAwait(false);
                if (cleared > 0)
                {
                    logger.LogInformation("Swept {Count} expired drag session(s).", cleared);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Drag session sweep failed.");
            }
        }
    }
}
