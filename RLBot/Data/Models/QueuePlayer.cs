namespace RLBot.Data.Models
{
    public class QueuePlayer
    {
        public ulong UserId { get; set; }
        public byte Team { get; set; }
        public short Elo { get; set; }
    }
}