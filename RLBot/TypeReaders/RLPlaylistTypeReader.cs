using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RLBot.Models;

namespace RLBot.TypeReaders
{
    public class RLPlaylistTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "1s":
                case "duel":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLPlaylist.Duel));
                case "2s":
                case "doubles":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLPlaylist.Doubles));
                case "3s":
                case "standard":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RLPlaylist.Standard));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Not a valid Rocket League playlist. {string.Join(", ", Enum.GetValues(typeof(RLPlaylist)).Cast<RLPlaylist>())}"));
        }
    }
}