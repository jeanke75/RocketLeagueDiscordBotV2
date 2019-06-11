namespace RLBot.Models
{
    public class RLRank
    {
        public ulong RoleID { get; }
        public short RequiredElo { get; }

        public RLRank(ulong id, short requiredElo)
        {
            RoleID = id;
            RequiredElo = requiredElo;
        }
    }
}