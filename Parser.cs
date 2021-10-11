using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {
    
    /// <summary>
    /// The top level that learns how to parse and does it
    /// </summary>
    public class Parser {

        public List<Entity> Entities = new();

        public Entity NewRootEntity() {
            var entity = new Entity(this);
            entity.IsRoot = true;
            Entities.Add(entity);
            return entity;
        }

        public Entity Entity(string name) {
            foreach (var entity in Entities) {
                if (entity.Name == name) return entity;
            }
            return null;
        }

        public Parser() {

            for (int i = 32; i < 127; i++) {
                var entity = NewRootEntity();
                entity.Literal = "" + ((char)i);
                switch (entity.Literal) {
                    case " ":
                        entity.Name = "Space character";
                        break;
                    default:
                        entity.Name = "'" + entity.Literal + "' character";
                        break;
                }
            }

            for (char c = 'A'; c <= 'Z'; c++) {
                var entity = NewRootEntity();
                entity.Name = "'" + c + "' letter";
                entity.AddAlternative(Entity("'" + c + "' character"));
                entity.AddAlternative(Entity("'" + c.ToString().ToLower() + "' character"));
            }

        }

        public List<EntityMatch> Parse(string text, int startAt = 0, int depth = 0) {
            var matches = new List<EntityMatch>();
            foreach (var entity in Entities) {
                var match = entity.Match(text, startAt, depth);
                matches.Add(match);
            }
            return matches;
        }

    }

}
