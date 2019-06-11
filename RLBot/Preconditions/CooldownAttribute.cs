using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;

namespace RLBot.Preconditions
{
    public class CooldownAttribute : PreconditionAttribute
    {
        public TimeSpan CooldownTime { get; }
        private ConcurrentDictionary<ulong, DateTimeOffset> CoolingDown { get; set; }

        protected Timer CleanupTimer { get; set; }

        public int CleanupTimeout { get; set; } = 300; //every 5 minutes

        private object cleanupLock = new object();

        /// <summary>
        /// Set up a cooldown (per-user) for the command with the given time
        /// </summary>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        /// <param name="seconds"></param>
        public CooldownAttribute(int hours, int minutes, int seconds)
        {
            CooldownTime = new TimeSpan(hours, minutes, seconds);
            CoolingDown = new ConcurrentDictionary<ulong, DateTimeOffset>();

            //optional extra todo: start timer for regular cleanup
            var timer = new Timer();
            CleanupTimer = new Timer()
            {
                AutoReset = true,
                Interval = TimeSpan.FromSeconds(CleanupTimeout).TotalMilliseconds,
                Enabled = true
            };
            CleanupTimer.Elapsed += CleanupTimer_Elapsed;
            CleanupTimer.Start();
        }

        private void CleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (cleanupLock) //lock to prevent this from accumulating (if the timeout elapses before we're done)
            {
                foreach (var kvp in CoolingDown)
                {
                    if (kvp.Value < DateTimeOffset.Now)
                    {
                        DateTimeOffset throwaway;
                        if (!CoolingDown.TryRemove(kvp.Key, out throwaway))
                        {
                            //log a threading error?
                            RLBot.Log(new LogMessage(LogSeverity.Critical, "CleanupTimer_Elapsed", "Cleanup timer threading error."));
                        }
                    }
                }
            }
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (CoolingDown.ContainsKey(context.User.Id))
            {
                DateTimeOffset endTime;
                if (CoolingDown.TryGetValue(context.User.Id, out endTime))
                {
                    if (endTime > DateTimeOffset.Now)
                    {
                        string name = (context as IGuildUser)?.Nickname ?? context.User.Username;

                        int diff = (int)Math.Ceiling((endTime - DateTimeOffset.Now).TotalSeconds);
                        int seconds = diff % 60;
                        diff -= seconds;
                        int minutes = diff % 3600;
                        diff -= minutes;
                        int hours = diff % 86400;
                        int days = diff - hours;

                        string time = "";
                        if (days > 0) time = $"{days / 86400}days ";
                        if (hours > 0) time = time + $"{hours / 3600}hours ";
                        if (minutes > 0) time = time + $"{minutes / 60}minutes ";
                        if (seconds > 0) time = time + $"{seconds}seconds ";

                        return PreconditionResult.FromError($"{name}, `{time}`before you can use this command again.");
                    }
                }
                else
                {
                    //something really glitchy happened, timer interfered?
                    //for now we'll ignore this possibility except for logging it
                    await RLBot.Log(new LogMessage(LogSeverity.Critical, "CheckPermissions", "glitchy cooldown error occurred."));
                }
            }

            CoolingDown.AddOrUpdate(context.User.Id, DateTimeOffset.Now + CooldownTime, (key, val) => DateTimeOffset.Now + CooldownTime);
            return PreconditionResult.FromSuccess();
        }
    }
}