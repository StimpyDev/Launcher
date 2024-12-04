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
        catch (Discord.ResultException ex)
        {
            // If discord isn't installed don't create background thread.
            if (ex.Result == Discord.Result.NotInstalled)
                return;
        }

        Task.Factory.StartNew(UpdateAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public static void Stop()
    {
        _discord?.Dispose();
        _discord = null;

        _cts.Cancel();
    }

    private static async Task UpdateAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                lock (_lock)
                {
                    _discord ??= new Discord.Discord(ClientId, (ulong)Discord.CreateFlags.NoRequireDiscord);

                    _discord.RunCallbacks();
                }
            }
            catch (Discord.ResultException ex)
            {
                // If Discord isn't running wait longer.
                if (ex.Result == Discord.Result.NotRunning)
                    await Task.Delay(TimeSpan.FromMinutes(1));
            }

            await Task.Delay(1000 / 60);
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