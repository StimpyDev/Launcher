using System;
using Discord;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Services;

public static class DiscordService
{
    private static readonly Lock _lock = new();
    private static Discord.Discord? _discord;
    private static CancellationTokenSource _cts = new();

    // OSFR Launcher application owned by OSFR team
    private const long ClientId = 1223728876199608410;

    public static void Start()
    {
        if (_cts.IsCancellationRequested)
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        try
        {
            lock (_lock)
            {
                _discord = new Discord.Discord(ClientId, (ulong)CreateFlags.NoRequireDiscord);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Discord: {ex}");
            Stop();
            return; // Exit if initialization fails
        }

        Task.Factory.StartNew(UpdateAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public static void Stop()
    {
        _cts.Cancel();

        _discord?.Dispose();
        _discord = null;
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
                        try
                        {
                            _discord = new Discord.Discord(ClientId, (ulong)CreateFlags.NoRequireDiscord);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating Discord instance: {ex}");
                            Stop();
                            break;
                        }
                    }

                    _discord.RunCallbacks();
                }

                await Task.Delay(1000 / 60, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateAsync: {ex}");
            Stop();
        }
    }

    public static void UpdateActivity(string details, string state)
    {
        ActivityManager? activityManager;

        lock (_lock)
        {
            if (_discord == null)
            {
                Console.WriteLine("Cannot update activity: Discord is not initialized.");
                return;
            }

            activityManager = _discord.GetActivityManager();
        }

        if (activityManager is null)
            return;

        var activity = new Activity
        {
            State = state,
            Details = details,
            Type = ActivityType.Playing,
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
    }
}