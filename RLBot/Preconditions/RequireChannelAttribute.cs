using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace RLBot.Preconditions
{
    /// <summary> Sets what channel the command or any command
    /// in this module can be used in. </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequireChannelAttribute : PreconditionAttribute
    {
        private readonly HashSet<ulong> _channelIds = new HashSet<ulong>();
        private readonly HashSet<string> _channelNames = new HashSet<string>();

        /// <summary> e.g. [RequireChannel(123456789012345678, 01234567890123456789)]</summary>
        /// <param name="channelIds">The params array of channelID's the command can be used in.</param>
        public RequireChannelAttribute(params ulong[] channelIds)
        {
            foreach (ulong channelId in channelIds)
                _channelIds.Add(channelId);
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (_channelIds.Contains(context.Channel.Id) || _channelNames.Contains(context.Channel.Name.ToLower()))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError(""));
        }
    }
}