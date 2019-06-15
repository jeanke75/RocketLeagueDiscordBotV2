using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace RLBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequireAdministratorOrBotOwnerAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guildAdminAttr = new RequireUserPermissionAttribute(GuildPermission.Administrator);
            var guildAdminResult = await guildAdminAttr.CheckPermissionsAsync(context, command, services);

            if (guildAdminResult.IsSuccess) return PreconditionResult.FromSuccess();

            var botOwnerAttr = new RequireOwnerAttribute();
            var botOwnerResult = await botOwnerAttr.CheckPermissionsAsync(context, command, services);

            if (botOwnerResult.IsSuccess) return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError(ErrorMessage ?? $"User requires administrator permission or has to be the bot owner.");
        }
    }
}