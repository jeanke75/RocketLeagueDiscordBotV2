using System.Collections.Generic;
using System.Linq;
using RLBot.Exceptions;
using RLBot.Models;

namespace RLBot.Data
{
    public static class Global
    {
        private static Dictionary<ulong, RLRank> _rankedChannelRequirement = new Dictionary<ulong, RLRank>();
        private static Dictionary<RLPlaylist, List<RLRank>> _playlistRanks = new Dictionary<RLPlaylist, List<RLRank>>();

        static Global()
        {
            // init ranks (roleId, requiredElo)
            List<RLRank> duelRanks = new List<RLRank>();
            var duelW = new RLRank(407225064602468353, 0);
            var duelX = new RLRank(407225059963568139, 916);
            var duelY = new RLRank(407225057677410304, 1156);
            var duelZ = new RLRank(407225054590664729, 1309);
            var duelA = new RLRank(407225051968962590, 1401);
            duelRanks.Add(duelW);
            duelRanks.Add(duelX);
            duelRanks.Add(duelY);
            duelRanks.Add(duelZ);
            duelRanks.Add(duelA);

            List<RLRank> doublesRanks = new List<RLRank>();
            var doublesW = new RLRank(407224786956189696, 0);
            var doublesX = new RLRank(407224673001275392, 936);
            var doublesY = new RLRank(407224549801984001, 1196);
            var doublesZ = new RLRank(407224239079424031, 1396);
            var doublesA = new RLRank(407224039363444736, 1551);
            doublesRanks.Add(doublesW);
            doublesRanks.Add(doublesX);
            doublesRanks.Add(doublesY);
            doublesRanks.Add(doublesZ);
            doublesRanks.Add(doublesA);

            List<RLRank> standardRanks = new List<RLRank>();
            var standardW = new RLRank(373857511146782721, 0);
            var standardX = new RLRank(375020227328475138, 936);
            var standardY = new RLRank(375035896753553409, 1196);
            var standardZ = new RLRank(373857123110617099, 1396);
            var standardA = new RLRank(386211454040276992, 1551);
            standardRanks.Add(standardW);
            standardRanks.Add(standardX);
            standardRanks.Add(standardY);
            standardRanks.Add(standardZ);
            standardRanks.Add(standardA);

            _playlistRanks.Add(RLPlaylist.Duel, duelRanks);
            _playlistRanks.Add(RLPlaylist.Doubles, doublesRanks);
            _playlistRanks.Add(RLPlaylist.Standard, standardRanks);


            // init ranked channels (channelId, requiredRank)
            _rankedChannelRequirement.Add(393692333469597696, duelA);
            _rankedChannelRequirement.Add(393693176772296704, duelZ);
            _rankedChannelRequirement.Add(393693726565728257, duelY);
            _rankedChannelRequirement.Add(393693993050832896, duelX);
            _rankedChannelRequirement.Add(393694545688133634, duelW);

            _rankedChannelRequirement.Add(393695480946622465, doublesA);
            _rankedChannelRequirement.Add(393695741941514240, doublesZ);
            _rankedChannelRequirement.Add(393695923026526208, doublesY);
            _rankedChannelRequirement.Add(393696051607109632, doublesX);
            _rankedChannelRequirement.Add(393696168300773386, doublesW);

            _rankedChannelRequirement.Add(386213046672162838, standardA);
            _rankedChannelRequirement.Add(375039923603898369, standardZ);
            _rankedChannelRequirement.Add(385070323449462785, standardY);
            _rankedChannelRequirement.Add(385420072996438021, standardX);
            _rankedChannelRequirement.Add(385393503833948160, standardW);
        }

        public static RLRank GetRank(RLPlaylist playlist, short elo)
        {
            _playlistRanks.TryGetValue(playlist, out List<RLRank> ranks);
            if (ranks == null)
                throw new RLException("Invalid playlist");
            
            return ranks.Where(x => x.RequiredElo <= elo).LastOrDefault();
        }

        public static List<RLRank> GetRanks(RLPlaylist playlist)
        {
            _playlistRanks.TryGetValue(playlist, out List<RLRank> ranks);
            return ranks;
        }
        
        public static RLRank GetChannelRankRequirement(ulong channelId)
        {
            _rankedChannelRequirement.TryGetValue(channelId, out RLRank rank);
            return rank;
        }
    }
}