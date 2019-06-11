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
                var channelType = await Database.GetChannelType(Context.Guild.Id, Context.Channel.Id);

                // update if it exists, otherwise insert
                if (channelType != null)
                {
                    await Database.UpdateChannelTypeAsync(Context.Guild.Id, Context.Channel.Id, playlist, isRanked);
                    await ReplyAsync($"The channel settings have been updated.");
                }
                else
                {
                    await Database.InsertChannelTypeAsync(Context.Guild.Id, Context.Channel.Id, playlist, isRanked);
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
                var channelType = await Database.GetChannelType(Context.Guild.Id, Context.Channel.Id);

                // remove if it exists
                if (channelType != null)
                {
                    await Database.DeleteChannelTypeAsync(Context.Guild.Id, Context.Channel.Id);
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