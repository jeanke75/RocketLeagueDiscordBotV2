using System;
using RLBot.Models;

namespace RLBot.Data.Models
{
    public class Queue
    {
        public long QueueID { get; set; }
        public byte ScoreTeamA { get; set; }
        public byte ScoreTeamB { get; set; }
        public RLPlaylist Playlist { get; set; }
        public DateTime Created { get; set; }
    }
}