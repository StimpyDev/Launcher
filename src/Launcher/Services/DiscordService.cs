using System;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Services;

public static class DiscordService
{
    private static Lock _lock = new();
    private static Discord.Discord? _discord;
    private static CancellationTokenSource _cts = new();

    // OSFR Launcher application owned by OSFR team
    private const long ClientId = 1223728876199608410;

    public static void Start()
    {
        if (_cts.IsCancellationRequested)
            _cts = new CancellationTokenSource();

        try
        {
            lock (_lock)
            {
                _discord = new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);
            }
        }
        finally
        {
            Stop();
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
                    _discord ??= new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);

                    _discord.RunCallbacks();
                }

                await Task.Delay(1000 / 60);
            }
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

        activityManager.UpdateActivity(activity, result =>
        {
        });
    }
}