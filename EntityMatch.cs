using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {

    public class EntityMatch {
        public int StartAt;
        public int Length;
        public Entity Entity;
        public List<EntityMatch> NextMatches = new();
    }

}
