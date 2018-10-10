using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.SqlCompact
{
    public class DbCache
    {
        private Dictionary<long, Referee> m_Referees = new Dictionary<long, Referee>();
        private Dictionary<long, Dictionary<long, Player>> m_PlayersPerTeam = new Dictionary<long, Dictionary<long, Player>>();
        private Dictionary<long, Player> m_Players = new Dictionary<long, Player>();
        private Dictionary<long, Match> m_Matches = new Dictionary<long, Match>();
        private Dictionary<long, Team> m_Teams;

        private Dictionary<long, Referee> m_InternalReferees = new Dictionary<long, Referee>();
        private Dictionary<long, Player> m_InternalPlayers = new Dictionary<long, Player>();
        private Dictionary<long, Match> m_InternalMatches = new Dictionary<long, Match>();
        private Dictionary<long, Team> m_InternalTeams;

        private Dictionary<long, long> m_ExternalTeamIdToTeamId = new Dictionary<long, long>();
        private readonly object m_SyncRoot = new object();

        public DbCache()
        {
            ReloadCache();
        }

        public void EnsureTeamExist(Team team)
        {
            lock (m_SyncRoot)
            {
                if (!m_Teams.ContainsKey(team.ExternalId))
                {
                    long dbId = Adapter.AddTeam(team);
                    m_InternalTeams.Add(dbId, team);
                    m_Teams.Add(team.ExternalId, team);
                    m_ExternalTeamIdToTeamId.Add(team.ExternalId, dbId);
                }
            }
        }

        public void EnsureRefereeExists(Referee r)
        {
            lock(m_SyncRoot)
            {
                if (!m_Referees.ContainsKey(r.ExternalId))
                {
                    long dbId = Adapter.AddReferee(r);
                    m_Referees.Add(r.ExternalId, r);
                    m_InternalReferees.Add(dbId, r);
                }
            }
        }

        public void EnsurePlayersExistsForTeam(IEnumerable<Player> players, long externalTeamId)
        {
            lock(m_SyncRoot)
            {
                Dictionary<long, Player> playersForTeam = null;
                if (!m_PlayersPerTeam.TryGetValue(externalTeamId, out playersForTeam))
                {
                    playersForTeam = new Dictionary<long, Player>();
                    m_PlayersPerTeam.Add(externalTeamId, playersForTeam);
                }
                long dbTeamId = m_ExternalTeamIdToTeamId[externalTeamId];
                foreach(var player in players)
                {
                    if (!playersForTeam.ContainsKey(player.ExternalId))
                    {
                        long dbId = Adapter.AddPlayer(player, dbTeamId);
                        playersForTeam.Add(player.ExternalId, player);
                    }
                }
            }
        }

        public void EnsureMatchExists(Match match)
        {
            lock(m_SyncRoot)
            {
                if (!m_Matches.ContainsKey(match.ExternalId))
                {
                    long dbId = Adapter.AddMatch(match);
                    m_Matches.Add(match.ExternalId, match);
                    m_InternalMatches.Add(dbId, match);
                }
            }
        }

        private void ReloadCache()
        {
            m_ExternalTeamIdToTeamId = Adapter.GetTeamDictionary();
            m_Referees = Adapter.GetAllReferees();
            m_Teams = Adapter.GetAllTeams();
            m_Players = Adapter.GetAllPlayers();

            m_InternalReferees = Adapter.GetAllRefereesInternal();
            m_InternalPlayers = Adapter.GetAllPlayersInternal();
            m_InternalTeams = Adapter.GetAllTeamsInternal();


            m_Matches = Adapter.GetAllMatches(
                m_InternalReferees,
                m_InternalPlayers,
                m_InternalTeams);

            m_PlayersPerTeam = new Dictionary<long, Dictionary<long, Player>>();
            foreach(var team in m_Teams)
            {
                m_PlayersPerTeam.Add(team.Key, Adapter.GetAllPlayersForTeam(team.Key));
            }
            
        }
    }
}
