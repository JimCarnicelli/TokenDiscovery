using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {

    /// <summary>
    /// A single class defined by a chain of entities such as a word, number, quoted text, etc
    /// </summary>
    public class EntityPart {

        public Parser Parser;

        public Entity Entity;

        public EntityPart Next;

        /// <summary>
        /// Like a regular expression, describes the rules a string of other entities must follow to be considered a match
        /// </summary>
        public List<EntityPart> Alternatives;

        public int MinQuantity = 1;

        public int MaxQuantity = 1;

        public bool Greedy = true;

        public EntityPart(Parser parser) {
            Parser = parser;
        }

        public string Describe(int depth) {
            string text = "";
            if (Entity != null) {
                text += Entity.ToString();
            } else if (Alternatives != null) {
                if (Alternatives.Count > 1 && depth > 0) text += "( ";
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += " | ";
                    var alt = Alternatives[i];
                    if (alt.Entity != null && alt.Entity.Name != null) {
                        text += alt.Entity.Name;
                    } else {
                        text += alt.Describe(depth + 1);
                    }
                }
                if (Alternatives.Count > 1 && depth > 0) text += " )";
            }
            if (Next != null) {
                text += " + " + Next.Describe(depth + 1);
            }
            if (MinQuantity == 1 && MaxQuantity == 1) {
                // Nothing to add here
            } else if (MaxQuantity == int.MaxValue) {
                text += " {" + MinQuantity + "+}";
            } else {
                text += " {" + MinQuantity + "-" + MaxQuantity + "}";
            }
            return text;
        }

        public EntityPart NewNextEntity(EntityPart part = null) {
            var nextEntity = new EntityPart(Parser);
            Next = nextEntity;
            if (part != null) nextEntity.AddAlternative(part);
            return nextEntity;
        }

        public EntityPart NewNextEntity(Entity entity = null) {
            var part = NewNextEntity(new EntityPart(Parser));
            part.Entity = entity;
            return part;
        }

        public EntityPart NewAlternative() {
            var part = new EntityPart(Parser);
            if (Alternatives == null) Alternatives = new List<EntityPart>();
            Alternatives.Add(part);
            return part;
        }

        public EntityPart AddAlternative(EntityPart part) {
            if (Alternatives == null) Alternatives = new List<EntityPart>();
            Alternatives.Add(part);
            return part;
        }

        public EntityPart AddAlternative(Entity entity) {
            var part = AddAlternative(new EntityPart(Parser));
            part.Entity = entity;
            return part;
        }

        public EntityMatch Match(string text, int startAt, int depth) {
            EntityMatch entityMatch = null;
            /*
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
                Console.WriteLine(text.Substring(startAt, entityMatch.Length) + " -> " + entityMatch.Entity.ToString());
                Parser.Parse(text, startAt + entityMatch.Length, depth + 1);
            }
            */
            return entityMatch;
        }

    }

}
