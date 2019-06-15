using Discord.Commands;
using RLBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RLBot.TypeReaders
{
    public class RLPlaylistTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (TryParsePlaylist(input, out RLPlaylist? playlist))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(playlist.Value));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, InvalidPlaylistMessage()));
        }

        public static bool TryParsePlaylist(string text, out RLPlaylist? playlist)
        {
            switch (text.ToLower())
            {
                case "1s":
                case "duel":
                    playlist = RLPlaylist.Duel;
                    return true;
                case "2s":
                case "doubles":
                    playlist = RLPlaylist.Doubles;
                    return true;
                case "3s":
                case "standard":
                    playlist = RLPlaylist.Standard;
                    return true;
                default:
                    playlist = null;
                    return false;
            }
        }

        public static string InvalidPlaylistMessage()
        {
            return $"Not a valid Rocket League playlist. {string.Join(", ", Enum.GetValues(typeof(RLPlaylist)).Cast<RLPlaylist>())}";
        }
    }
}