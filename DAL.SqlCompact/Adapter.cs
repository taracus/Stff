using DAL.SqlCompact.StffContextTableAdapters;
using Shared;
using Shared.MatchUpdates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.SqlCompact
{
    internal class Adapter
    {
        public static Dictionary<long, Referee> GetAllReferees()
        {
            Dictionary<long, Referee> referees = new Dictionary<long, Referee>();
            using (var context = new StffContext())
            {
                foreach(var referee in context.Referee)
                {
                    Referee r = new Referee();
                    r.ExternalId = referee.ExternalId;
                    r.FirstName = referee.FirstName;
                    r.LastName = referee.LastName;
                    r.Location = referee.Region;
                    referees.Add(r.ExternalId, r);
                }
            }
            return referees;
        }

        public static Dictionary<long, Team> GetAllTeams()
        {
            Dictionary<long, Team> teams = new Dictionary<long, Team>();
            using (var context = new StffContext())
            {
                foreach (var team in context.Team)
                {
                    Team t = new Team();
                    t.AwayColors = team.AwayColors;
                    t.Colors = team.Colors;
                    t.ExternalId = team.ExternalId;
                    t.Name = team.TeamName;
                    teams.Add(t.ExternalId, t);
                }
            }
            return teams;
        }

        public static Dictionary<long, Player> GetAllPlayersForTeam(long externalTeamId)
        {
            Dictionary<long, Player> players = new Dictionary<long, Player>();
            using (var context = new StffContext())
            {
                long teamId = context.Team.Where(team => team.ExternalId == externalTeamId).Select(t => t.Id).Single();
                foreach(var player in context.Player.Where(p => p.TeamId == teamId))
                {
                    Player p = new Player();
                    p.ExternalId = player.ExternalId;
                    p.FirstName = player.FirstName;
                    p.LastName = player.SecondName;
                    players.Add(p.ExternalId, p);
                }
            }
            return players;
        }

        public static Dictionary<long, Player> GetAllPlayers()
        {
            Dictionary<long, Player> players = new Dictionary<long, Player>();
            using (var context = new StffContext())
            {
                foreach (var player in context.Player)
                {
                    Player p = new Player();
                    p.ExternalId = player.ExternalId;
                    p.FirstName = player.FirstName;
                    p.LastName = player.SecondName;
                    players.Add(p.ExternalId, p);
                }
            }
            return players;
        }

        public static Dictionary<long, Referee> GetAllRefereesInternal()
        {
            Dictionary<long, Referee> referees = new Dictionary<long, Referee>();
            using (var context = new StffContext())
            {
                foreach (var referee in context.Referee)
                {
                    Referee r = new Referee();
                    r.ExternalId = referee.ExternalId;
                    r.FirstName = referee.FirstName;
                    r.LastName = referee.LastName;
                    r.Location = referee.Region;
                    referees.Add(referee.Id, r);
                }
            }
            return referees;
        }

        public static Dictionary<long, Team> GetAllTeamsInternal()
        {
            Dictionary<long, Team> teams = new Dictionary<long, Team>();
            using (var context = new StffContext())
            {
                foreach (var team in context.Team)
                {
                    Team t = new Team();
                    t.AwayColors = team.AwayColors;
                    t.Colors = team.Colors;
                    t.ExternalId = team.ExternalId;
                    t.Name = team.TeamName;
                    teams.Add(team.Id, t);
                }
            }
            return teams;
        }

        public static Dictionary<long, long> GetTeamDictionary()
        {
            Dictionary<long, long> d = new Dictionary<long, long>();
            using (var context = new StffContext())
            {
                foreach (var team in context.Team)
                {
                    d.Add(team.ExternalId, team.Id);
                }
            }
            return d;
        }
        public static Dictionary<long, Player> GetAllPlayersInternal()
        {
            Dictionary<long, Player> players = new Dictionary<long, Player>();
            using (var context = new StffContext())
            {
                foreach (var player in context.Player)
                {
                    Player p = new Player();
                    p.ExternalId = player.ExternalId;
                    p.FirstName = player.FirstName;
                    p.LastName = player.SecondName;
                    players.Add(player.Id, p);
                }
            }
            return players;
        }

        public static Dictionary<long, Match> GetAllMatches(
            Dictionary<long, Referee> refCache,
            Dictionary<long, Player> playerCache,
            Dictionary<long, Team> teamCache)
        {
            Dictionary<long, Match> matches = new Dictionary<long, Match>();
            using (var context = new StffContext())
            {
                foreach(var match in context.Match.ToList())
                {
                    var updates = context.MatchUpdate.Where(mu => mu.MatchId == match.Id);
                    Match m = new Match();
                    m.AssRef1 = refCache[match.AssRefereeId1];
                    m.AssRef2 = refCache[match.AssRefereeId2];
                    m.AwayTeam = teamCache[match.AwayTeamid].ExternalId;
                    m.AwayTeamPlayers = new List<Player>();
                    foreach(var player in context.Player.Where(p => p.TeamId == match.AwayTeamid))
                    {
                        m.AwayTeamPlayers.Add(playerCache[player.Id]);
                    }
                    m.Completed = match.Completed;
                    m.ExternalId = match.ExternalMatchId;
                    m.GameWeek = match.GameWeek;
                    m.HomeTeam = teamCache[match.HomeTeamId].ExternalId;
                    m.HomeTeamPlayers = new List<Player>();
                    foreach (var player in context.Player.Where(p => p.TeamId == match.HomeTeamId))
                    {
                        m.HomeTeamPlayers.Add(playerCache[player.Id]);
                    }
                    m.MatchUpdates = new List<MatchUpdate>();
                    foreach(var mu in context.MatchUpdate.Where(update => update.MatchId == match.Id))
                    {
                        m.MatchUpdates.Add(MatchUpdate.GetUpdate(mu.HomeTeam, mu.Minute, playerCache[mu.PlayerId], (MatchUpdateType)Enum.Parse(typeof(MatchUpdateType), mu.Type)));
                    }

                    m.Ref = refCache[match.RefereeId];
                    m.StartTime = m.StartTime;
                    matches.Add(m.ExternalId, m);
                }
            }
            return matches;
        }

        public static long AddTeam(Team team)
        {
            using (var context = new StffContext())
            {
                var newRow = context.Team.AddTeamRow(team.Name, team.Colors, team.AwayColors, team.ExternalId);
                //context.Team.Rows.Add(newRow);
                context.AcceptChanges();

                TeamTableAdapter adapter = new TeamTableAdapter();
                adapter.Update(context);
                context.AcceptChanges();
                return newRow.Id;
            }
        }

        public static long AddReferee(Referee referee)
        {
            using (var context = new StffContext())
            {
                var insertedRow = context.Referee.AddRefereeRow(referee.FirstName, referee.LastName, referee.Location, referee.ExternalId);
                insertedRow.SetAdded();
                insertedRow.AcceptChanges();
                context.AcceptChanges();
                return insertedRow.Id;
            }
        }

        public static long AddPlayer(Player player, long teamId)
        {
            using (var context = new StffContext())
            {
                var teamRow = context.Team.Where(t => t.Id == teamId).Single();
                var insertedRow = context.Player.AddPlayerRow(teamRow, player.FirstName, player.LastName, player.ExternalId);
                insertedRow.AcceptChanges();
                context.AcceptChanges();
                return insertedRow.Id;
            }
        }

        public static long AddMatch(Match match)
        {
            using (var context = new StffContext())
            {
                var homeTeamRow = GetTeamRow(match.HomeTeam, context);
                var awayTeamRow = GetTeamRow(match.AwayTeam, context);

                var referee = GetRefereeRow(match.Ref.ExternalId, context);
                var assRef1 = GetRefereeRow(match.Ref.ExternalId, context);
                var assRef2 = GetRefereeRow(match.Ref.ExternalId, context);
                var insertedRow = context.Match.AddMatchRow(
                    match.StartTime,
                    match.GameWeek,
                    homeTeamRow,
                    awayTeamRow,
                    referee,
                    assRef1,
                    assRef2,
                    match.Completed,
                    match.ExternalId);
                insertedRow.AcceptChanges();
                context.AcceptChanges();
                foreach(var update in match.MatchUpdates)
                {
                    AddMatchUpdate(update, insertedRow, context);
                }
                return insertedRow.Id;
            }
        }

        private static long AddMatchUpdate(MatchUpdate update, StffContext.MatchRow match, StffContext context)
        {
            var playerRow = context.Player.Where(p => p.ExternalId == update.Player.ExternalId).Single();

            var insertedRow = context.MatchUpdate.AddMatchUpdateRow(update.Type.ToString(), update.Minute, playerRow, match, update.HomeTeam);
            insertedRow.AcceptChanges();
            context.AcceptChanges();
            return insertedRow.Id;
        }

        private static StffContext.TeamRow GetTeamRow(long externalTeamId, StffContext context)
        {
            return context.Team.Where(t => t.ExternalId == externalTeamId).Single();
        }

        private static StffContext.PlayerRow GetPlayerRow(long externalTeamId, StffContext context)
        {
            return context.Player.Where(p => p.ExternalId == externalTeamId).Single();
        }

        private static StffContext.RefereeRow GetRefereeRow(long externalRefId, StffContext context)
        {
            return context.Referee.Where(r => r.ExternalId == externalRefId).Single();
        }
    }
}
