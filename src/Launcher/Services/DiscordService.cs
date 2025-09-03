using System;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Services;

public static class DiscordService
{
    private static readonly object _lock = new object();
    private static Discord.Discord? _discord;
    private static CancellationTokenSource _cts = new();
    private static Task? _updateTask;

    // OSFR Launcher application owned by OSFR team
    private const long ClientId = 1223728876199608410;

    public static void Start()
    {
        if (!IsDiscordAvailable())
        {
            Console.WriteLine("Discord is not available. Service will not start.");
            return;
        }

        if (_cts.IsCancellationRequested)
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        try
        {
            lock (_lock)
            {
                _discord = new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Discord: {ex}");
            return; // Exit if initialization fails
        }

        // Start the update loop only if not already running
        if (_updateTask == null || _updateTask.IsCompleted)
        {
            _updateTask = Task.Factory.StartNew(UpdateAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    public static void Stop()
    {
        _cts.Cancel();

        lock (_lock)
        {
            _discord?.Dispose();
            _discord = null;
        }

        // Optionally wait for the update task to complete
        if (_updateTask != null)
        {
            try
            {
                _updateTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Console.WriteLine($"Error stopping Discord update task: {e}");
                }
            }
        }
    }

    private static bool IsDiscordAvailable()
    {
        try
        {
            // Attempt to create a Discord client as a test
            using var testDiscord = new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private static async Task UpdateAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_discord == null)
                    {
                        if (!IsDiscordAvailable())
                        {
                            Console.WriteLine("Discord not available during update loop.");
                            break; // Exit if Discord is unavailable
                        }

                        try
                        {
                            _discord = new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating Discord instance: {ex}");
                            break;
                        }
                    }

                    _discord?.RunCallbacks();
                }

                await Task.Delay(1000 / 60, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateAsync: {ex}");
        }
        finally
        {
            Stop();
        }
    }

    public static void UpdateActivity(string details, string state)
    {
        Discord.ActivityManager? activityManager;

        lock (_lock)
        {
            if (_discord == null)
            {
                Console.WriteLine("Cannot update activity: Discord is not initialized.");
                return;
            }

            activityManager = _discord?.GetActivityManager();
        }

        if (activityManager is null)
            return;

        var activity = new Discord.Activity
        {
            State = state,
            Details = details,
            Type = Discord.ActivityType.Playing,
            Timestamps =
        {
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        }
        };

        try
        {
            activityManager.UpdateActivity(activity, _ => { });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update activity: {ex}");
        }

        activityManager.UpdateActivity(activity, _ =>
            {

            });
    }
}