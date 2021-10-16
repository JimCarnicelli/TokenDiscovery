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

        /// <summary>
        /// Like a regular expression, describes the rules a string of other entities must follow to be considered a match
        /// </summary>
        public List<EntityPart> Alternatives;

        public bool LookBehind = false;

        public bool LookAhead = false;

        public bool Not = false;

        public int MinQuantity = 1;

        public int MaxQuantity = 1;

        public bool Greedy = true;

        public EntityPart Next;

        public EntityPart(Parser parser) {
            Parser = parser;
        }

        public string Describe(int depth) {
            string text = "";

            if (LookAhead) text += ">";
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

            if (Not) {
                text += "!";
            } else if (MinQuantity == 1 && MaxQuantity == 1) {
                // Nothing to add here
            } else if (MaxQuantity == int.MaxValue) {
                text += "{" + MinQuantity + "+}";
            } else {
                text += "{" + MinQuantity + "-" + MaxQuantity + "}";
            }

            if (LookBehind) text += "<";

            if (Next != null) {
                text += " + " + Next.Describe(depth + 1);
            }

            return text;
        }

        public EntityPart NewNextPart(Entity entity = null) {
            var nextPart = new EntityPart(Parser);
            Next = nextPart;
            nextPart.Entity = entity;
            return nextPart;
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

        public EntityMatch Match(string text, int startAt, EntityMatchChain matchChain) {
            var matches = new List<EntityMatch>();

            int totalLength = 0;
            int runningStartAt = startAt;
            while (matches.Count < MaxQuantity) {
                EntityMatch match = null;

                if (Entity != null) {
                    if (LookBehind) {
                        match = matchChain.HasEntityEndingAt(runningStartAt - 1, Entity);
                    } else {
                        match = Entity.Match(text, runningStartAt, matchChain);
                        if (match != null && LookAhead) {
                            match.Length = 0;
                        }
                    }
                } else {
                    // See if at least one of the alternatives matches
                    foreach (var Alt in Alternatives) {
                        match = Alt.Match(text, runningStartAt, matchChain);
                        if (match != null) break;
                    }
                }

                if (Not) {
                    if (match != null) return null;
                    match = new EntityMatch();
                    match.StartAt = runningStartAt;
                    match.Length = 0;
                    match.Entity = null;
                } else {
                    if (match == null) break;
                }

                matches.Add(match);
                totalLength += match.Length;
                runningStartAt += match.Length;
            }

            if (matches.Count < MinQuantity) return null;

            EntityMatch combinedMatch;
            if (matches.Count == 1) {
                combinedMatch = matches[0];
            } else if (matches.Count == 0) {
                combinedMatch = new EntityMatch();
                combinedMatch.StartAt = startAt;
                combinedMatch.Length = 0;
                combinedMatch.Count = 0;
            } else {
                combinedMatch = new EntityMatch();
                combinedMatch.StartAt = startAt;
                combinedMatch.Length = totalLength;
                combinedMatch.Count = matches.Count;
                combinedMatch.SubMatches = matches;
            }

            // Is there a next part that must also match?
            if (Next != null) {
                var nextMatch = Next.Match(text, startAt + combinedMatch.Length, matchChain);
                if (nextMatch == null) return null;
                combinedMatch.Length += nextMatch.Length;
            }

            return combinedMatch;
        }

    }

}
