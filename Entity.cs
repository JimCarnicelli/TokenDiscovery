using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {
    public class Entity {

        public Parser Parser;

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

        public EntityMatch Match(string text, int startAt) {
            EntityMatch match;
            if (Literal == null) {
                match = Head.Match(text, startAt);
                if (match == null) return null;
                match.Entity = this;
            } else {
                if (startAt + Literal.Length > text.Length) return null;
                if (text.Substring(startAt, Literal.Length) != Literal) return null;
                match = new EntityMatch();
                match.Entity = this;
                match.StartAt = startAt;
                match.Length = Literal.Length;
            }
            return match;
        }

    }
}
