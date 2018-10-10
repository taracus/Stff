using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    [Serializable]
    public class Player
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long ExternalId { get; set; }
    }
}
