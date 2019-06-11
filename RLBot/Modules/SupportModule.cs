using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RLBot.Modules
{
    [Name("Support")]
    [Summary("Bot support")]
    public class SupportModule : ModuleBase<SocketCommandContext>
    {
        [Command("suggestion", RunMode = RunMode.Async)]
        [Summary("Send in suggestions for new features or improvements for the bot.")]
        [Remarks("suggestion <your suggestion>")]
        public async Task SuggestionAsync([Remainder] string suggestion)
        {
            var channel = Context.Client.GetChannel(393390897573789699) as IMessageChannel;
            if (channel == null)
            {
                var application = await Context.Client.GetApplicationInfoAsync();
                await ReplyAsync($"Can't find the suggestion channel, please contact {application.Owner}.");
                return;
            }

            var msg = await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .WithTitle("Suggestion")
                .WithDescription(suggestion.Trim())
                .WithFooter($"Submitted by {Context.Message.Author}")
                .Build());
            
            var e1Task = msg.AddReactionAsync(new Emoji("✅"));
            var e2Task = msg.AddReactionAsync(new Emoji("❌"));
            await Task.WhenAll(e1Task, e2Task); 

            await ReplyAsync("Succesfully submitted the suggestion.");
        }

        [Command("bugreport", RunMode = RunMode.Async)]
        [Summary("Send in a detailed bugreport for the bot. Try to describe how to reproduce it if possible.")]
        [Remarks("bugreport <your bugreport>")]
        public async Task BugReportAsync([Remainder] string bug)
        {
            if (bug == "") return;

            var channel = Context.Client.GetChannel(387560503641374730) as IMessageChannel;
            if (channel == null)
            {
                var application = await Context.Client.GetApplicationInfoAsync();
                await ReplyAsync($"Can't find the bug report channel, please contact {application.Owner}.");
                return;
            }

            await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithColor(RLBot.EMBED_COLOR)
                .WithAuthor(Context.Message.Author)
                .WithDescription(bug.Trim())
                .WithCurrentTimestamp()
                .Build());

            await ReplyAsync("Succesfully submitted the bugreport.");
        }
    }
}