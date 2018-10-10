using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    [Serializable]
    public class Team
    {
        public string Name { get; set; }
        public string Colors { get; set; }
        public string AwayColors { get; set; }
        public long ExternalId { get; set; }
    }
}
