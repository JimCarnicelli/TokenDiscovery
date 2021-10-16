using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {
    public class Entity {

        public Parser Parser;

        private static long NextId = 0;

        public long Id;

        /// <summary>
        /// Ultimately all significant entities need descriptive names like "Digits", "Word", or "Quoted text"
        /// </summary>
        public string Name;

        /// <summary>
        /// One or more literal characters that represent the entire rigid pattern for this entity part
        /// </summary>
        public string Literal;

        public EntityPart Head;

        public Entity(Parser parser, string literal = null) {
            Parser = parser;
            Id = NextId++;
            if (literal == null) {
                Head = new EntityPart(parser);
            } else {
                Literal = literal;
            }
        }

        public override string ToString() {
            if (Name != null) return Name;
            if (Literal != null) return "\"" + Literal + "\"";
            return "( " + Head.Describe(0) + " )";
        }

        public EntityMatch Match(string text, int startAt, EntityMatchChain matchChain) {
            if (startAt >= text.Length) return null;
            EntityMatch match = matchChain.HasEntityStartingAt(startAt, this);
            if (match != null) return match;
            if (Literal == null) {
                var innerMatch = Head.Match(text, startAt, matchChain);
                if (innerMatch == null) return null;
                match = new EntityMatch();
                match.Entity = this;
                match.StartAt = startAt;
                match.Length = innerMatch.Length;
            } else {
                if (startAt + Literal.Length > text.Length) return null;
                if (text.Substring(startAt, Literal.Length) != Literal) return null;
                match = new EntityMatch();
                match.Entity = this;
                match.StartAt = startAt;
                match.Length = Literal.Length;
            }
            if (match.Length > 0) matchChain.Add(match);
            return match;
        }

    }
}
