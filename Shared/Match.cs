using Shared.MatchUpdates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    [Serializable]
    public class Match
    {
        public long ExternalId { get; set; }
        public DateTime StartTime { get; set; }
        public int GameWeek { get; set; }
        public string Division { get; set; }

        public long HomeTeam { get; set; }
        public long AwayTeam { get; set; }

        public List<Player> HomeTeamPlayers { get; set; }
        public List<Player> AwayTeamPlayers { get; set; }

        public Referee Ref { get; set; }
        public Referee AssRef1 { get; set; }
        public Referee AssRef2 { get; set; }

        public List<MatchUpdate> MatchUpdates { get; set; }

        public bool Completed { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public int HomeScore
        {
            get
            {
                return MatchUpdates.Count(mu => mu.Type == MatchUpdateType.Goal && mu.HomeTeam);
            }
        }

        public int AwayScore
        {
            get
            {
                return MatchUpdates.Count(mu => mu.Type == MatchUpdateType.Goal && !mu.HomeTeam);
            }
        }
    }
}
