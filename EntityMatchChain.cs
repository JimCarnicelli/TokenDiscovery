using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery_V1 {
    public class EntityMatchChain {

        public int Length;
        public string Text;
        public Dictionary<long, EntityMatch>[] Starts;
        public Dictionary<long, EntityMatch>[] Ends;

        public Dictionary<long, EntityMatch> this[int index] {
            get {
                return Starts[index];
            }
        }

        public EntityMatchChain(string text) {
            Text = text;
            Length = Text.Length;
            Starts = new Dictionary<long, EntityMatch>[Length];
            Ends = new Dictionary<long, EntityMatch>[Length];
            Starts[0] = new Dictionary<long, EntityMatch>();
            Ends[0] = new Dictionary<long, EntityMatch>();
        }

        public EntityMatch HasEntityStartingAt(int index, Entity entity) {
            if (index < 0) return null;
            if (index >= Starts.Length) return null;
            if (Starts[index] == null) return null;
            if (Starts[index].TryGetValue(entity.Id, out EntityMatch match)) return match;
            return null;
        }

        public EntityMatch HasEntityEndingAt(int index, Entity entity) {
            if (index < 0) return null;
            if (index >= Starts.Length) return null;
            if (Ends[index].TryGetValue(entity.Id, out EntityMatch match)) return match;
            return null;
        }

        public void Add(EntityMatch match) {
            if (Starts[match.StartAt] == null) {
                Starts[match.StartAt] = new Dictionary<long, EntityMatch>();
            }
            if (match.StartAt + 1 < Starts.Length && Starts[match.StartAt + 1] == null) {
                Starts[match.StartAt + 1] = new Dictionary<long, EntityMatch>();
            }
            Starts[match.StartAt][match.Entity.Id] = match;
            //Console.WriteLine(match.StartAt + " > Adding " + match.Entity);
            if (Ends[match.StartAt + match.Length - 1] == null) {
                Ends[match.StartAt + match.Length - 1] = new Dictionary<long, EntityMatch>();
            }
            Ends[match.StartAt + match.Length - 1][match.Entity.Id] = match;
        }

        public string Describe() {
            string description = "";
            for (int i = 0; i < Length; i++) {
                description += i + ": " + Text.Substring(i, 1) + " ---------\n";
                if (Starts[i] == null) continue;
                foreach (var match in Starts[i].Values) {
                    description += "  " + match.Entity + "\n";
                    description += "    " + Text.Substring(match.StartAt, match.Length).Replace(" ", "_") + "\n";
                }
            }
            return description;
        }

    }
}
