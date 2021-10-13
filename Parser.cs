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

        public Entity NewRootEntity(string literal = null) {
            var entity = new Entity(this, literal);
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

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                var entity = NewRootEntity("" + ((char)i));
                switch (entity.Literal) {
                    case " ":
                        entity.Name = "Space";
                        break;
                    default:
                        entity.Name = "'" + entity.Literal + "'";
                        break;
                }
            }

            // Generate an entity for all letters with both upper and lower case versions of each
            for (char c = 'A'; c <= 'Z'; c++) {
                var entity = NewRootEntity();
                entity.Name = "'" + c + "' letter";
                entity.Head.AddAlternative(Entity("'" + c + "' character"));
                entity.Head.AddAlternative(Entity("'" + c.ToString().ToLower() + "' character"));
            }

        }

        public List<EntityMatch> Parse(string text, int startAt = 0, int depth = 0) {
            var matches = new List<EntityMatch>();
            foreach (var entity in Entities) {
                var match = entity.Match(text, startAt, depth);
                if (match != null) {
                    matches.Add(match);
                }
            }
            return matches;
        }

    }

}
