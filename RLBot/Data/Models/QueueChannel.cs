using RLBot.Models;

namespace RLBot.Data.Models
{
    public class QueueChannel
    {
        public RLPlaylist Playlist { get; set; }
        public bool Ranked { get; set; }

        public int? RequiredElo { get; set; }
    }
}