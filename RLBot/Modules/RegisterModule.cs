using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RLBot.Data;
using System;
using System.Threading.Tasks;

namespace RLBot.Modules
{
    [Name("Register")]
    [Summary("Register yourself to start playing")]
    public class RegisterModule : ModuleBase<SocketCommandContext>
    {
        [Command("register", RunMode = RunMode.Async)]
        [Summary("Register a matchmaking account.")]
        [Remarks("register")]
        [RequireBotPermission(GuildPermission.EmbedLinks | GuildPermission.ManageRoles)]
        public async Task LinkAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            var user = Context.Message.Author as SocketGuildUser;
            try
            {
                // check if discord id is already in the database
                var userinfo = await Database.GetUserInfoAsync(Context.Guild.Id, user.Id);
                if (userinfo != null)
                {
                    await ReplyAsync($"{user.Mention}, you've already been registered.");
                    return;
                }

                // try to add the user to the database
                await Database.InsertUserInfoAsync(Context.Guild.Id, user.Id, 900, 900, 900);

                // give the role to the user
                var role = Context.Guild.GetRole(568858227480199188);

                await user.AddRoleAsync(role);

                await ReplyAsync($"{user.Mention}, you've been succesfully registered!");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"{user.Mention}, " + ex.Message);
            }
        }
    }
}