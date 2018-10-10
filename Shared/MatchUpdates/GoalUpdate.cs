﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.MatchUpdates
{
    [Serializable]
    public class GoalUpdate : MatchUpdate
    {
        public override MatchUpdateType Type => MatchUpdateType.Goal;
    }
}
