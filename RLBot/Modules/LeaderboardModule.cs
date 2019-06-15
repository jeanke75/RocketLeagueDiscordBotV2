using Discord;
using Discord.Commands;
using RLBot.Data;
using RLBot.Data.Models;
using RLBot.Models;
using RLBot.TypeReaders;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RLBot.Modules
{
    [Name("Leaderboard")]
    [Summary("Everything involving the leaderboard")]
    public class LeaderboardModule : ModuleBase<SocketCommandContext>
    {
        [Command("stats", RunMode = RunMode.Async)]
        [Summary("Returns leaderboard info about the current user, or the user parameter, if one passed.")]
        [Remarks("stats <playlist> <optional user>")]
        public async Task StatsAsync([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist, IUser user = null)
        {
            await Context.Channel.TriggerTypingAsync();
            var userInfo = user ?? Context.Message.Author;

            Leaderboard recordTotal = null;
            Leaderboard recordMonthly = null; 
            
            using (SqlConnection conn = Database.GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    var queueTotalTask = Database.GetLeaderboardUserStatsAsync(Context.Guild.Id, userInfo.Id, playlist, false, conn);
                    var queueMonthlyTask = Database.GetLeaderboardUserStatsAsync(Context.Guild.Id, userInfo.Id, playlist, true, conn);

                    await Task.WhenAll(queueTotalTask, queueMonthlyTask);
                    
                    recordTotal = await queueTotalTask;
                    recordMonthly = await queueMonthlyTask;
                }
                catch (Exception ex)
                {
                    await ReplyAsync(ex.Message);
                    throw ex;
                }
                finally
                {
                    conn.Close();
                }
            }

            string monthlyStats = (recordMonthly != null ? $"#{recordMonthly.Rank}\nWins: {recordMonthly.Wins}\nGames Played: {recordMonthly.TotalGames}" : "Unranked");
            string totalStats = (recordTotal != null ? $"#{recordTotal.Rank}\nWins: {recordTotal.Wins}\nGames Played: {recordTotal.TotalGames}" : "Unranked");

            await ReplyAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .WithTitle($":trophy: {playlist} Leaderboard - {userInfo} :trophy:")
                .AddField("Monthly", monthlyStats, true)
                .AddField("All-Time", totalStats, true)
                .Build());
        }

        [Command("top5", RunMode = RunMode.Async)]
        [Summary("Returns the 5 players with the most wins for both Monthly and All-Time.")]
        [Remarks("top5 <playlist>")]
        public async Task Top5Async([OverrideTypeReader(typeof(RLPlaylistTypeReader))] RLPlaylist playlist)
        {
            await Context.Channel.TriggerTypingAsync();
            Leaderboard[] monthlyTop5 = null;
            Leaderboard[] allTimeTop5 = null;
            using (SqlConnection conn = Database.GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    var monthlyTask = Database.GetLeaderboardTop5Async(Context.Guild.Id, playlist, true, conn);
                    var allTimeTask = Database.GetLeaderboardTop5Async(Context.Guild.Id, playlist, false, conn);
                    await Task.WhenAll(monthlyTask, allTimeTask);
                    monthlyTop5 = (await monthlyTask).ToArray();
                    allTimeTop5 = (await allTimeTask).ToArray();
                }
                catch (Exception ex)
                {
                    await ReplyAsync(ex.Message);
                    throw ex;
                }
                finally
                {
                    conn.Close();
                }
            }

            var builder = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithTitle($":trophy: {playlist} Leaderboard :trophy:");

            EmbedWithTop5(builder, "Monthly", monthlyTop5);
            EmbedWithTop5(builder, "All-Time", allTimeTop5);

            if (builder.Fields.Count > 0)
                await ReplyAsync("", false, builder.Build());
            else
                await ReplyAsync("No leaderboard data found.");
        }

        private void EmbedWithTop5(EmbedBuilder builder, string title, Leaderboard[] top5)
        {
            if (top5 == null || top5.Length == 0) return;

            int records = top5.Length <= 5 ? top5.Length : 5;
            string s = "";
            for (int i = 0; i < records; i++)
            {
                string icon = "";
                if (i == 0)
                    icon = ":first_place:";
                else if (i == 1)
                    icon = ":second_place:";
                else if (i == 2)
                    icon = ":third_place:";
                else
                    icon = ":eight_pointed_black_star:";
                
                float perc = (float)Math.Round(top5[i].Wins * 100.0f / top5[i].TotalGames, 2);

                // in case the user isn't in any of the servers the bot is in anymore show the user id
                var user = Context.Client.GetUser(top5[i].UserID);
                string username = (user != null ? user.ToString() : $"<{top5[i].UserID}>");
                s += $"{icon} {username} - {top5[i].Wins}wins ({perc}%)\n";
            }

            builder.AddField(title, s, true);
        }
    }
}