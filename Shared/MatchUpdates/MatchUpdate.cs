using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.MatchUpdates
{
    [Serializable]
    public enum MatchUpdateType
    {
        Goal,
        YellowCard,
        RedCard
    }
    [Serializable]
    public abstract class MatchUpdate
    {
        public abstract MatchUpdateType Type { get; }
        public bool HomeTeam { get; set; }
        public int Minute { get; set; }
        public Player Player { get; set; }

        public static MatchUpdate GetUpdate(bool home, int minute, Player player, MatchUpdateType type)
        {
            MatchUpdate mu = null;
            if (type == MatchUpdateType.Goal)
            {
                mu = new GoalUpdate();
            }
            else if (type == MatchUpdateType.RedCard)
            {
                mu = new RedCardUpdate();
            }
            else if (type == MatchUpdateType.YellowCard)
            {
                mu = new YellowCardUpdate();
            }
            mu.Minute = minute;
            mu.HomeTeam = home;
            mu.Player = player;
            return mu;
        }
    }
}
