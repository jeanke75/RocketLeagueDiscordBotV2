using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace RLBot.Modules
{
    [Name("Info")]
    [Summary("General information")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("invite", RunMode = RunMode.Async)]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        [Remarks("invite")]
        public async Task Invite()
        {
            var application = await Context.Client.GetApplicationInfoAsync();

            var generalPermissions = GuildPermission.ManageRoles | GuildPermission.ManageChannels | GuildPermission.CreateInstantInvite | GuildPermission.ChangeNickname | GuildPermission.ViewChannel;
            var textPermissions = GuildPermission.SendMessages | GuildPermission.ManageMessages | GuildPermission.EmbedLinks | GuildPermission.AttachFiles | GuildPermission.ReadMessageHistory | GuildPermission.MentionEveryone | GuildPermission.UseExternalEmojis | GuildPermission.AddReactions;
            var voicePermissions = GuildPermission.MoveMembers;
            var requiredPermissions = (int)(generalPermissions | textPermissions | voicePermissions);

            await ReplyAsync($"<https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions={requiredPermissions}>");
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Ping to see the latency")]
        [Remarks("ping")]
        public async Task PingAsync()
            => await ReplyAsync($"Pong! - {Context.Client.Latency}ms");

        [Command("botinfo", RunMode = RunMode.Async)]
        [Summary("General info about the bot")]
        [Remarks("botinfo")]
        public async Task InfoAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            string latestChanges = null;
            
            var _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            using (var response = await _http.GetAsync("https://api.github.com/repos/jeanke75/RocketLeagueDiscordBotV2/commits"))
            {
                if (!response.IsSuccessStatusCode)
                    latestChanges = "Error Recieving Latest Changes :x:";
                else
                {
                    dynamic Info = JArray.Parse(await response.Content.ReadAsStringAsync());

                    latestChanges =
                    $"[{((string)Info[0].sha).Substring(0, 5)}]({Info[0].html_url}) {Info[0].commit.message}\n" +
                    $"[{((string)Info[1].sha).Substring(0, 5)}]({Info[1].html_url}) {Info[1].commit.message}\n" +
                    $"[{((string)Info[2].sha).Substring(0, 5)}]({Info[2].html_url}) {Info[2].commit.message}";
                }

                response.Dispose();
            }
            
            await ReplyAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .AddField("Info",
                    $"**Author:** `{application.Owner}` [ID: {application.Owner.Id}]\n" +
                    $"**Library:** Discord.Net - Version: {DiscordConfig.Version}\n" +
                    $"**Total Guilds:** {Context.Client.Guilds.Count()}\n" +
                    $"**Total Channels:** {Context.Client.Guilds.Sum(g => g.Channels.Count())}\n" +
                    $"**Total Users:** {Context.Client.Guilds.Sum(g => g.MemberCount - g.Users.Where(b => b.IsBot).Count())}")
                .AddField("Process Info",
                    $"**Runtime:** {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                    $"**Heap Size:** {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}MB\n" +
                    $"**Uptime**: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"d\d\ h\h\ m\m\ s\s")}")
                .AddField("Latest Changes on Github", latestChanges)
                .Build());
        }

        [Command("userinfo", RunMode = RunMode.Async)]
        [Alias("user", "whois")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Remarks("userinfo (<user>)")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info for")] SocketUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;
            await ReplyAsync(userInfo.ToString());
        }
    }
}