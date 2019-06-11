using System;
using System.Collections.Concurrent;
using Discord.WebSocket;

namespace RLBot.Models
{
    public class RLQueue
    {
        public DateTime Created { get; private set; }
        public RLPlaylist Playlist { get; private set; }
        public bool IsLeaderboardQueue { get; private set; }
        public SocketGuildChannel Channel { get; private set; }
        public ConcurrentDictionary<ulong, SocketUser> Users { get; private set; }

        private RLQueue(RLPlaylist playlist, SocketGuildChannel channel, bool isLeaderboardQueue)
        {
            Created = DateTime.Now;
            Playlist = playlist;
            IsLeaderboardQueue = isLeaderboardQueue;
            Channel = channel;
            
            Users = new ConcurrentDictionary<ulong, SocketUser>();
        }

        public static RLQueue DuelQueue(SocketGuildChannel channel, bool isLeaderboardQueue)
        {
            return new RLQueue(RLPlaylist.Duel, channel, isLeaderboardQueue);
        }

        public static RLQueue DoublesQueue(SocketGuildChannel channel, bool isLeaderboardQueue)
        {
            return new RLQueue(RLPlaylist.Doubles, channel, isLeaderboardQueue);
        }

        public static RLQueue StandardQueue(SocketGuildChannel channel, bool isLeaderboardQueue)
        {
            return new RLQueue(RLPlaylist.Standard, channel, isLeaderboardQueue);
        }

        public int GetSize()
        {
            switch (Playlist)
            {
                case RLPlaylist.Duel:
                    return 2;
                case RLPlaylist.Doubles:
                    return 4;
                case RLPlaylist.Standard:
                    return 6;
                default:
                    return int.MaxValue;
            }
        }
    }
}