using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {

    /// <summary>
    /// A single class defined by a chain of entities such as a word, number, quoted text, etc
    /// </summary>
    public class Entity {

        public Parser Parser;

        public bool IsRoot = false;

        /// <summary>
        /// Ultimately all significant entities need descriptive names like "Digits", "Word", or "Quoted text"
        /// </summary>
        public string Name;

        public Entity NextEntity;

        /// <summary>
        /// Like a regular expression, describes the rules a string of other entities must follow to be considered a match
        /// </summary>
        public List<Entity> Alternatives;

        /// <summary>
        /// One or more literal characters that represent the entire rigid pattern for this entity
        /// </summary>
        public string Literal;

        public override string ToString() {
            string text = Name;
            // TODO: Deal with any other case
            if (text == null) text = "(" + Describe() + ")";
            return text;
        }

        public Entity(Parser parser) {
            Parser = parser;
        }

        public string Describe() {
            string text = "";
            if (Literal != null) {
                text += "\"" + Literal.Replace("\"", "\"\"") + "\"";
            } else if (Alternatives != null) {
                if (Alternatives.Count > 1 && !IsRoot) text += "( ";
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += " | ";
                    var alt = Alternatives[i];
                    if (alt.Name != null) {
                        text += alt.Name;
                    } else {
                        text += alt.Describe();
                    }
                }
                if (Alternatives.Count > 1 && !IsRoot) text += " )";
            }
            if (NextEntity != null) {
                text += " + " + NextEntity.Describe();
            }
            return text;
        }

        public Entity NewNextEntity(Entity entity = null) {
            var nextEntity = new Entity(Parser);
            NextEntity = nextEntity;
            if (entity != null) nextEntity.AddAlternative(entity);
            return nextEntity;
        }

        public Entity NewAlternative() {
            var entity = new Entity(Parser);
            if (Alternatives == null) Alternatives = new List<Entity>();
            Alternatives.Add(entity);
            return entity;
        }

        public Entity AddAlternative(Entity entity) {
            if (Alternatives == null) Alternatives = new List<Entity>();
            Alternatives.Add(entity);
            return entity;
        }

        public EntityMatch Match(string text, int startAt, int depth) {
            EntityMatch entityMatch = null;
            if (Literal != null) {
                if (startAt + Literal.Length >= text.Length) return null;  // Couldn't possibly match
                var subText = text.Substring(startAt, Literal.Length);
                if (subText == Literal) {
                    entityMatch = new EntityMatch();
                    entityMatch.StartAt = startAt;
                    entityMatch.Length = Literal.Length;
                    entityMatch.Entity = this;
                }
            }
            if (entityMatch != null) {
                Console.WriteLine(text.Substring(startAt, entityMatch.Length) + " -> " + entityMatch.Entity.Name);
                Parser.Parse(text, startAt + entityMatch.Length, depth + 1);
            }
            return entityMatch;
        }

    }

}
