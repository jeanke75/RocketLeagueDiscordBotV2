using Discord.WebSocket;
using RLBot.Data.Models;
using RLBot.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace RLBot.Data
{
    public static class Database
    {
        public static SqlConnection GetSqlConnection()
        {
            Uri uri = new Uri(ConfigurationManager.AppSettings["SQLSERVER_URI"]);
            string connectionString = new SqlConnectionStringBuilder
            {
                DataSource = uri.Host,
                InitialCatalog = uri.AbsolutePath.Trim('/'),
                UserID = uri.UserInfo.Split(':').First(),
                Password = uri.UserInfo.Split(':').Last(),
                MultipleActiveResultSets = true
            }.ConnectionString;

            return new SqlConnection(connectionString);
        }

        #region UserInfo
        public static async Task<UserInfo> GetUserInfoAsync(ulong guildId, ulong userId)
        {
            UserInfo result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                    cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                    cmd.CommandText = "SELECT * FROM UserInfo WHERE GuildID = @GuildID AND UserID = @UserID";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            result = new UserInfo()
                            {
                                GuildID = guildId,
                                UserID = userId,
                                JoinDate = (DateTime)reader["JoinDate"],
                                Elo1s = (short)reader["Elo1s"],
                                Elo2s = (short)reader["Elo2s"],
                                Elo3s = (short)reader["Elo3s"]
                            };
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }

        public static async Task InsertUserInfoAsync(ulong guildId, ulong userId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                            cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                            cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                            cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                            cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                            cmd.CommandText = "INSERT INTO UserInfo(GuildID, UserID, JoinDate, Elo1s, Elo2s, Elo3s) VALUES(@GuildID, @UserID, GetDate(), @Elo1s, @Elo2s, @Elo3s)";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task UpdateUserInfoAsync(ulong guildId, ulong userId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        await UpdateUserInfoAsync(conn, tr, guildId, userId, elo1s, elo2s, elo3s);
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        private static async Task UpdateUserInfoAsync(SqlConnection conn, SqlTransaction tr, ulong guildId, ulong userId, short elo1s, short elo2s, short elo3s)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Elo1s", DbType.Int16).Value = elo1s;
                cmd.Parameters.AddWithValue("@Elo2s", DbType.Int16).Value = elo2s;
                cmd.Parameters.AddWithValue("@Elo3s", DbType.Int16).Value = elo3s;
                cmd.CommandText = "UPDATE UserInfo set Elo1s = @Elo1s, Elo2s = @Elo2s, Elo3s = @Elo3s WHERE GuildID = @GuildID AND UserID = @UserID";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task UpdateUserInfoAsync(SqlConnection conn, SqlTransaction tr, ulong guildId, ulong userId, RLPlaylist playlist, short elo)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Elo", DbType.Int16).Value = elo;
                switch (playlist)
                {
                    case RLPlaylist.Duel:
                        cmd.CommandText = "UPDATE UserInfo set Elo1s = @Elo WHERE GuildID = @GuildID AND UserID = @UserID";
                        break;
                    case RLPlaylist.Doubles:
                        cmd.CommandText = "UPDATE UserInfo set Elo2s = @Elo WHERE GuildID = @GuildID AND UserID = @UserID";
                        break;
                    case RLPlaylist.Standard:
                        cmd.CommandText = "UPDATE UserInfo set Elo3s = @Elo WHERE GuildID = @GuildID AND UserID = @UserID";
                        break;
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }
        #endregion

        #region Server settings
        #region General
        public static async Task<Settings> GetSettings(ulong guildId)
        {
            Settings result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                    cmd.CommandText = "SELECT RoleID, SubmitChannelID FROM Settings WHERE GuildID = @GuildID;";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            result = new Settings()
                            {
                                RoleID = (ulong)(decimal)reader["RoleID"],
                                SubmitChannelID = (ulong)(decimal)reader["SubmitChannelID"]
                            };
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }

        public static async Task InsertSettingsAsync(ulong guildId, ulong roleId, ulong submitChannelId)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                            cmd.Parameters.AddWithValue("@RoleID", DbType.Decimal).Value = (decimal)roleId;
                            cmd.Parameters.AddWithValue("@SubmitChannelID", DbType.Decimal).Value = (decimal)submitChannelId;
                            cmd.CommandText = "INSERT INTO Settings(GuildID, RoleID, SubmitChannelID) VALUES(@GuildID, @RoleID, @SubmitChannelID);";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }
        #endregion

        #region QueueChannel
        public static async Task<QueueChannel> GetQueueChannelAsync(ulong guildId, ulong channelId)
        {
            QueueChannel result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                result = await GetQueueChannelAsync(conn, null, guildId, channelId);
            }
            return result;
        }

        private static async Task<QueueChannel> GetQueueChannelAsync(SqlConnection conn, SqlTransaction tr, ulong guildId, ulong channelId)
        {
            QueueChannel result = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                if (tr != null)
                    cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                cmd.Parameters.AddWithValue("@ChannelID", DbType.Decimal).Value = (decimal)channelId;
                cmd.CommandText = "SELECT * FROM QueueChannel WHERE GuildID = @GuildID AND ChannelID = @ChannelID;";

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        result = new QueueChannel()
                        {
                            Playlist = (RLPlaylist)(byte)reader["Playlist"],
                            Ranked = (bool)reader["Ranked"],
                            RequiredElo = (reader["RequiredElo"] as int?).GetValueOrDefault()
                        };
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public static async Task InsertQueueChannelAsync(ulong guildId, ulong channelId, RLPlaylist playlist, bool ranked, int? requiredElo)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                            cmd.Parameters.AddWithValue("@ChannelID", DbType.Decimal).Value = (decimal)channelId;
                            cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                            cmd.Parameters.AddWithValue("@Ranked", DbType.Boolean).Value = ranked;
                            if (ranked)
                            {
                                cmd.Parameters.AddWithValue("@RequiredElo", DbType.Int32).Value = requiredElo;
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@RequiredElo", DBNull.Value);
                            }
                            cmd.CommandText = "INSERT INTO QueueChannel(GuildID, ChannelID, Playlist, Ranked, RequiredElo) VALUES(@GuildID, @ChannelID, @Playlist, @Ranked, @RequiredElo);";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task UpdateQueueChannelAsync(ulong guildId, ulong channelId, RLPlaylist playlist, bool ranked, int? requiredElo)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                            cmd.Parameters.AddWithValue("@ChannelID", DbType.Decimal).Value = (decimal)channelId;
                            cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                            cmd.Parameters.AddWithValue("@Ranked", DbType.Boolean).Value = ranked;
                            if (ranked)
                            {
                                cmd.Parameters.AddWithValue("@RequiredElo", DbType.Int32).Value = requiredElo;
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@RequiredElo", DBNull.Value);
                            }
                            cmd.CommandText = "UPDATE QueueChannel SET Playlist = @Playlist, Ranked = @Ranked, RequiredElo = @RequiredElo;";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task DeleteQueueChannelAsync(ulong guildId, ulong channelId)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                            cmd.Parameters.AddWithValue("@ChannelID", DbType.Decimal).Value = (decimal)channelId;
                            cmd.CommandText = "DELETE FROM QueueChannel WHERE GuildID = @GuildID AND ChannelID = @ChannelID;";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Queues
        public static async Task<Queue> GetQueueAsync(long queueId)
        {
            Queue result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                result = await GetQueueAsync(conn, null, queueId);
            }
            return result;
        }

        private static async Task<Queue> GetQueueAsync(SqlConnection conn, SqlTransaction tr, long queueId)
        {
            Queue result = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                if (tr != null)
                    cmd.Transaction = tr;
                
                cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                cmd.CommandText = "SELECT * FROM Queue WHERE QueueID = @QueueID;";

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        result = new Queue()
                        {
                            QueueID = (long)reader["QueueID"],
                            ScoreTeamA = (byte)reader["ScoreTeamA"],
                            ScoreTeamB = (byte)reader["ScoreTeamB"],
                            Playlist = (RLPlaylist)(byte)reader["Playlist"],
                            Created = (DateTime)reader["Created"]
                        };
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public static async Task<long> InsertQueueAsync(ulong guildId, RLPlaylist type, List<SocketUser> team_a, List<SocketUser> team_b)
        {
            long queueId = -1;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)type;
                            cmd.CommandText = "INSERT INTO Queue(ScoreTeamA, ScoreTeamB, Created, Playlist) OUTPUT INSERTED.QueueID VALUES(0, 0, GETDATE(), @Playlist);";

                            var res = await cmd.ExecuteScalarAsync();
                            queueId = (long)res;
                        }

                        var tasks = new Task[team_a.Count + team_b.Count];
                        int i = 0;
                        foreach (SocketUser user in team_a)
                        {
                            tasks[i] = InsertQueuePlayerAsync(conn, tr, queueId, guildId, user.Id, 0);
                            i++;
                        }
                        foreach (SocketUser user in team_b)
                        {
                            tasks[i] = InsertQueuePlayerAsync(conn, tr, queueId, guildId, user.Id, 1);
                            i++;
                        }

                        await Task.WhenAll(tasks);

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
            return queueId;
        }

        private static async Task UpdateQueueAsync(SqlConnection conn, SqlTransaction tr, long queueId, byte scoreTeamA, byte scoreTeamB)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                cmd.Parameters.AddWithValue("@ScoreTeamA", DbType.Byte).Value = scoreTeamA;
                cmd.Parameters.AddWithValue("@ScoreTeamB", DbType.Byte).Value = scoreTeamB;
                cmd.CommandText = "UPDATE Queue SET ScoreTeamA = @ScoreTeamA, ScoreTeamB = @ScoreTeamB WHERE QueueID = @QueueID;";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task<List<QueuePlayer>> GetQueuePlayersAsync(long queueId)
        {
            List<QueuePlayer> result = new List<QueuePlayer>();
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                    cmd.CommandText = "SELECT qp.UserID, qp.Team, Elo = case when q.Playlist = 1 then ui.Elo1s when q.Playlist = 2 then ui.Elo2s else ui.Elo3s end FROM QueuePlayer qp INNER JOIN Queue q ON q.QueueID = qp.QueueID INNER JOIN UserInfo ui ON ui.UserID = qp.UserID WHERE qp.QueueID = @QueueID;";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new QueuePlayer()
                            {
                                UserId = (ulong)(decimal)reader["UserID"],
                                Team = (byte)reader["Team"],
                                Elo = (short)reader["Elo"]
                            });
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }

        private static async Task InsertQueuePlayerAsync(SqlConnection conn, SqlTransaction tr, long queueId, ulong guildId, ulong userId, byte team)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tr;

                cmd.Parameters.AddWithValue("@QueueID", DbType.Int64).Value = queueId;
                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                cmd.Parameters.AddWithValue("@Team", DbType.Byte).Value = team;
                cmd.CommandText = "INSERT INTO QueuePlayer(QueueId, GuildID, UserID, Team) VALUES(@QueueId, @GuildID, @UserID, @Team);";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task SubstituteQueuePlayerAsync(long queueId, ulong subPlayer, ulong currentPlayer)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;

                            cmd.Parameters.AddWithValue("@QueueID", DbType.Decimal).Value = (decimal)queueId;
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)subPlayer;
                            cmd.Parameters.AddWithValue("@CurrentUserID", DbType.Decimal).Value = (decimal)currentPlayer;
                            cmd.CommandText = "UPDATE QueuePlayer SET UserID = @NewUserID WHERE QueueID = @QueueID AND UserID = @CurrentUserID;";

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task SetQueueResultAsync(ulong guildId, long queueId, byte scoreTeamA, byte scoreTeamB, RLPlaylist playlist, List<QueuePlayer> players)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        // check if the queue exists and if the score hasn't been submitted yet
                        var queue = await GetQueueAsync(conn, tr, queueId);
                        if (queue == null)
                            throw new Exception($"Didn't find queue {queueId}!");
                        
                        if (queue.ScoreTeamA != 0 && queue.ScoreTeamB != 0)
                            throw new Exception($"The score for queue {queueId} has already been submitted!");
                        
                        // update the queue score
                        await UpdateQueueAsync(conn, tr, queueId, scoreTeamA, scoreTeamB);

                        // update player elos
                        foreach (QueuePlayer player in players)
                        {
                            await UpdateUserInfoAsync(conn, tr, guildId, player.UserId, playlist, player.Elo);
                        }

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                }
            }
        }
        #endregion

        #region Leaderboard
        public static async Task<Leaderboard> GetLeaderboardUserStatsAsync(ulong guildId, ulong userId, RLPlaylist playlist, bool monthly, SqlConnection conn = null)
        {
            Leaderboard rec = null;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                cmd.Parameters.AddWithValue("@UserID", DbType.Decimal).Value = (decimal)userId;
                if (monthly)
                    cmd.CommandText = "select * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.GuildID = @GuildID AND qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24))  AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y WHERE y.UserID = @UserID";
                else
                    cmd.CommandText = "select * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.GuildID = @GuildID AND qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y WHERE y.UserID = @UserID";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        rec = new Leaderboard()
                        {
                            UserID = userId,
                            Rank = (long)reader["Rank"], 
                            Wins = (int)reader["Wins"],
                            TotalGames = (int)reader["TotalGames"]
                        };
                    }
                    reader.Close();
                }
            }
            return rec;
        }

        public static async Task<List<Leaderboard>> GetLeaderboardTop5Async(ulong guildId, RLPlaylist playlist, bool monthly, SqlConnection conn)
        {
            List<Leaderboard> records = new List<Leaderboard>();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@Playlist", DbType.Byte).Value = (byte)playlist;
                cmd.Parameters.AddWithValue("@GuildID", DbType.Decimal).Value = (decimal)guildId;
                if (monthly)
                    cmd.CommandText = "select TOP 5 * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.GuildID = @GuildID AND qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24))  AND q.Created >= CAST(DATEADD(dd, -DAY(GETDATE()) + 1, GETDATE()) AS DATE) AND q.Created < CAST(DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0) AS DATE) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y order by y.Rank";
                else
                    cmd.CommandText = "select TOP 5 * from (select row_number() OVER (ORDER BY x.Wins DESC, x.TotalGames ASC) as Rank, x.UserID, x.Wins, x.TotalGames from (SELECT qp.UserID, ISNULL(SUM(CASE WHEN ((qp.Team = 0 AND q.ScoreTeamA > q.ScoreTeamB) OR (qp.Team = 1 AND q.ScoreTeamA < q.ScoreTeamB)) THEN 1 END), 0) as Wins, COUNT(1) as TotalGames FROM Queue q INNER JOIN QueuePlayer qp ON qp.GuildID = @GuildID AND qp.QueueID = q.QueueID WHERE ((q.ScoreTeamA > 0 OR q.ScoreTeamB > 0) OR (DATEDIFF(hour, q.Created, GetDate()) > 24)) AND q.Playlist = @Playlist GROUP BY qp.UserID) x ) y order by y.Rank";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {
                        Leaderboard rec = new Leaderboard()
                        {
                            UserID = (ulong)(decimal)reader["UserID"],
                            Wins = (int)reader["Wins"],
                            TotalGames = (int)reader["TotalGames"]
                        };
                        records.Add(rec);
                    }
                    reader.Close();
                }
            }
            return records;
        }
        #endregion

        #region SQL
        public static async Task RunSQLAsync(string command)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tr;

                        cmd.CommandText = command;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    tr.Commit();
                }
            }
        }

        public static async Task<DataTable> DatabaseTablesAsync()
        {
            DataTable schemaDataTable = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    schemaDataTable = conn.GetSchema("Tables");
                }
                finally
                {
                    conn.Close();
                }
            }
            return schemaDataTable;
        }
        #endregion
    }
}