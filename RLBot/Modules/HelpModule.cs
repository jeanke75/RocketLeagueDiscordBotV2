using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using RLBot.Preconditions;

namespace RLBot.Modules
{
    [Name("Help")]
    [Group("help")]
    [Hidden]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService commands;

        public HelpModule(CommandService commands)
        {
            this.commands = commands;
        }

        [Name("help")]
        [Command(RunMode = RunMode.Async)]
        [Summary("List all the modules in the bot")]
        public async Task HelpAsync()
        {
            string mList = "";
            foreach (var module in commands.Modules.Where(m => m.Attributes.FirstOrDefault(a => a.GetType() == typeof(HiddenAttribute)) == null))
                mList += string.Concat($"**► Name: {module.Name}** ││", $"{module.Summary}", "\n\n");

            var embed = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(mList)
                        .WithFooter($"For a module's commands - Use {RLBot.COMMAND_PREFIX}help module <module name>")
                        .Build();
            try
            {
                await Context.Message.Author.SendMessageAsync("", false, embed);
            }
            catch (HttpException ex)
            when (ex.DiscordCode == 50007)
            {
                // send message normally if dm's are blocked by receiver
                await ReplyAsync("", false, embed);
            }
        }

        [Command("module", RunMode = RunMode.Async)]
        [Summary("List all the commands in a module")]
        [Remarks("help module <module name>")]
        public async Task HelpModuleAsync(string moduleName)
        {
            string cList = "";
            var module = commands.Modules.FirstOrDefault(x => x.Name.ToLower() == moduleName.ToLower());

            if (module == null)
            {
                string msg = $"`{moduleName}` is an invalid module! :mag:";
                try
                {
                    await Context.Message.Author.SendMessageAsync(msg);
                }
                catch (HttpException ex)
                when (ex.DiscordCode == 50007)
                {
                    await Context.Channel.SendMessageAsync(msg);
                }
                return;
            }

            foreach (var command in module.Commands)
                cList += string.Concat($"**► Name: {command.Name}**\n", $"• Summary: {command.Summary}\n\n");

            var embed = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(cList)
                        .WithFooter($"For a command's details - Use {RLBot.COMMAND_PREFIX}help command <command name>")
                        .Build();
            try
            {
                await Context.Message.Author.SendMessageAsync("", false, embed);
            }
            catch (HttpException ex)
            when (ex.DiscordCode == 50007)
            {
                // send message normally if dm's are blocked by receiver
                await ReplyAsync("", false, embed);
            }
        }

        [Command("command", RunMode = RunMode.Async)]
        [Summary("List a command's details")]
        [Remarks("help command <command name>")]
        public async Task HelpCommandAsync(string c)
        {
            var result = commands.Search(Context, c);
            string cDetails = "";

            if (result.Commands == null)
            {
                var msg = $"`{c}` is an invalid command! :mag:";
                try
                {
                    await Context.Message.Author.SendMessageAsync(msg);
                }
                catch (HttpException ex)
                when (ex.DiscordCode == 50007)
                {
                    await ReplyAsync(msg);
                }
                return;
            }

            foreach (var cmd in result.Commands)
            {
                var aliases = string.Join(", ", cmd.Command.Aliases.ToArray());
                cDetails += $"**► Name: {cmd.Command.Name}**\n• Aliases: {aliases}\n• Summary: {cmd.Command.Summary}\n• Usage: {RLBot.COMMAND_PREFIX + cmd.Command.Remarks}";
            }

            var embed = new EmbedBuilder()
                        .WithColor(RLBot.EMBED_COLOR)
                        .WithDescription(cDetails)
                        .Build();
            
            try
            {
                await Context.Message.Author.SendMessageAsync("", false, embed);
            }
            catch (HttpException ex)
            when (ex.DiscordCode == 50007)
            {
                // send message normally if dm's are blocked by receiver
                await ReplyAsync("", false, embed);
            }
        }
    }
}