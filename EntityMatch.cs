using System.Collections.Generic;

namespace TokenDiscovery {

    public class EntityMatch {
        public int StartAt;
        public int Length;
        public Entity Entity;
        public int Count = 1;
        public List<EntityMatch> SubMatches;
    }

}
