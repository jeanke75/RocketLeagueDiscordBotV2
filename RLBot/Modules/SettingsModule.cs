using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RLBot.Data;
using RLBot.Models;
using RLBot.TypeReaders;
using System;
using System.Threading.Tasks;

namespace RLBot.Modules
{
    [Name("Settings")]
    [Summary("Change settings for the bot")]
    [RequireOwner]
    //[RequireUserPermission(GuildPermission.Administrator)]
    public class SettingsModule : InteractiveBase<SocketCommandContext>
    {
        [Command("install", RunMode = RunMode.Async)]
        [Summary("Creates a role and a channel for submitting the scores.")]
        [Remarks("install")]
        [RequireBotPermission(GuildPermission.SendMessages | GuildPermission.ManageRoles | GuildPermission.ManageChannels)]
        public async Task InstallAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                if (await Database.GetSettings(Context.Guild.Id) != null)
                {
                    await ReplyAsync("Everything required for the bot to function has already been created.");
                    return;
                }
                
                // Add the role
                var role = await Context.Guild.CreateRoleAsync("RLBot", new GuildPermissions(false, false, false, false, false, false, true, false, false, true, false, false, true, true, true, false, true, true, true, false, false, false, true, false, false, false, false, false, false), Color.Orange, false);

                // Add the submitchannel
                var submitChannel = await Context.Guild.CreateTextChannelAsync("submit-scores", x =>
                {
                    x.SlowModeInterval = 5;
                    x.Topic = $"Use '{RLBot.COMMAND_PREFIX}qresult <queue ID> <score team A> <score team B>' to submit a match result. No chatting allowed!";
                });

                // Set the bot's permissions
                await submitChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, new OverwritePermissions(PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny));

                // Give permission to people with the role
                await submitChannel.AddPermissionOverwriteAsync(role, new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny));

                // Remove all permissions for everyone else
                await submitChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny));

                // Save the id's in the database
                await Database.InsertSettingsAsync(Context.Guild.Id, role.Id, submitChannel.Id);

                await ReplyAsync($"The bot has been succesfully installed.");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Installation of the bot failed: " + ex.Message);
            }
        }

        [Command("createchannel", RunMode = RunMode.Async)]
        [Summary("Create a queue channel.")]
        [Remarks("createchannel <1s/2s/3s>")]
        [RequireBotPermission(GuildPermission.SendMessages | GuildPermission.ManageChannels)]
        public async Task CreateQueueChannelAsync([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var settings = await Database.GetSettings(Context.Guild.Id);
                if (settings == null)
                {
                    await ReplyAsync($"No settings found for this channel, add them by running the command \"{RLBot.COMMAND_PREFIX}setchannel\".");
                    return;
                }

                await ReplyAsync("Ranked? (y/n)");
                var rankedResponse = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (rankedResponse == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }

                bool ranked = rankedResponse.Content.ToLower() == "y";

                int? requiredElo = null;
                if (ranked)
                {
                    await ReplyAsync("What is the minimum required elo for this channel? (Tip: Make sure you have a channel that has 0 as the minimum)");
                    var eloResponse = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (eloResponse == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    if (int.TryParse(eloResponse.Content, out int elo))
                    {
                        if (elo < 0)
                        {
                            await ReplyAsync("The minimum elo can't be lower than 0.");
                            return;
                        }

                        requiredElo = elo;
                    }
                    else
                    {
                        await ReplyAsync("You must provide a number.");
                        return;
                    }
                }

                var queueChannel = await Context.Guild.CreateTextChannelAsync("submit-scores", x =>
                {
                    x.SlowModeInterval = 5;
                    x.Topic = $"Commands: '{RLBot.COMMAND_PREFIX}qo(pen)', '{RLBot.COMMAND_PREFIX}qj(oin)', '{RLBot.COMMAND_PREFIX}ql(eave)', '{RLBot.COMMAND_PREFIX}qs(tatus)', '{RLBot.COMMAND_PREFIX}qsub', '{RLBot.COMMAND_PREFIX}qp(ick)', '{RLBot.COMMAND_PREFIX}qr(eset)'";
                });
                await Database.InsertQueueChannelAsync(Context.Guild.Id, queueChannel.Id, playlist, ranked, requiredElo);

                // Set the bot's permissions
                await queueChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, new OverwritePermissions(PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny));

                // Give permission to people with the role
                await queueChannel.AddPermissionOverwriteAsync(Context.Guild.GetRole(settings.RoleID), new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny));

                // Remove all permissions for everyone else
                await queueChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny));


                await ReplyAsync($"The queue channel has been created.");

                // check if the channel is already in the database
                //var queueChannel = await Database.GetQueueChannelAsync(Context.Guild.Id, Context.Channel.Id);

                // update if it exists, otherwise insert
                /*if (queueChannel != null)
                {
                    await Database.UpdateQueueChannelAsync(Context.Guild.Id, Context.Channel.Id, playlist, ranked);
                    await ReplyAsync($"The channel settings have been updated.");
                }
                else
                {
                    await Database.InsertQueueChannelAsync(Context.Guild.Id, Context.Channel.Id, playlist, ranked);
                    await ReplyAsync($"The channel settings have been added.");
                }*/
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Updating the channel settings failed: " + ex.Message);
            }
        }

        [Command("deletechannel", RunMode = RunMode.Async)]
        [Summary("Delete the settings for channel this is ran in.")]
        [Remarks("deletechannel")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task DeleteSettingsAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                // check if the channel is in the database
                var queueChannel = await Database.GetQueueChannelAsync(Context.Guild.Id, Context.Channel.Id);

                // remove if it exists
                if (queueChannel != null)
                {
                    await Database.DeleteQueueChannelAsync(Context.Guild.Id, Context.Channel.Id);
                    await ReplyAsync($"The channel settings have been deleted.");
                }
                else
                {
                    await ReplyAsync($"No channel settings were found.");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Deleting the channel settings failed: " + ex.Message);
            }
        }
    }
}