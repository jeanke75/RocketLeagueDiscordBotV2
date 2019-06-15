using Discord;
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
    public class SettingsModule : ModuleBase<SocketCommandContext>
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
                await submitChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Allow, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny));

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



        [Command("setchannel", RunMode = RunMode.Async)]
        [Summary("Update the settings for the channel this is ran in.")]
        [Remarks("setchannel <1s/2s/3s> <ranked/unranked")]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task UpsertChannelAsync([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist, string ranked)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                bool isRanked;
                switch (ranked.ToLower())
                {
                    case "ranked":
                        isRanked = true;
                        break;
                    case "unranked":
                        isRanked = false;
                        break;
                    default:
                        await ReplyAsync($"The 2nd argument must be 'ranked' or 'unranked'!");
                        return;
                }

                // check if the channel is already in the database
                var queueChannel = await Database.GetQueueChannelAsync(Context.Guild.Id, Context.Channel.Id);

                // update if it exists, otherwise insert
                if (queueChannel != null)
                {
                    await Database.UpdateQueueChannelAsync(Context.Guild.Id, Context.Channel.Id, playlist, isRanked);
                    await ReplyAsync($"The channel settings have been updated.");
                }
                else
                {
                    await Database.InsertQueueChannelAsync(Context.Guild.Id, Context.Channel.Id, playlist, isRanked);
                    await ReplyAsync($"The channel settings have been added.");
                }
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