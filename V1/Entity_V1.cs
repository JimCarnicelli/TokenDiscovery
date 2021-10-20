using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery_V1 {

    public enum EntityType {
        Trivial,
        Baseline,
        Derived,
        Experimental
    }

    public class Entity {

        public Parser Parser;

        private static long NextId = 0;

        public EntityType Type = EntityType.Experimental;

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

        public int SurveyScore = 0;
        public int SurveyCounts = 0;
        public int SurveyLength = 0;
        public int SurveyLongest = 0;
        public Dictionary<int, bool> SurveyCoverage = new();
        public Dictionary<string, int> SurveyExamples;

        public Entity(Parser parser, EntityType type, string literal = null) {
            Parser = parser;
            this.Type = type;
            Id = NextId++;
            if (literal == null) {
                Head = new EntityPart(parser);
            } else {
                Literal = literal;
            }
        }

        public override string ToString() {
            return ToString(true);
        }

        public string ToString(bool forceParentheses) {
            if (Name != null) return Name;
            if (Literal != null) return "\"" + Literal + "\"";
            string description = Describe();
            if (!forceParentheses && Head.MinQuantity == 1 && Head.MaxQuantity == 1) {
                return description;
            }
            return "( " + description + " )";
        }

        public string Describe() {
            if (Literal != null) return Literal;
            return Head.Describe(0);
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

            if (SurveyExamples == null) SurveyExamples = new Dictionary<string, int>();
            var sampleText = text.Substring(startAt, match.Length);
            SurveyExamples.TryGetValue(sampleText, out int count);
            SurveyExamples[sampleText] = count + 1;

            return match;
        }

    }
}
