using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TokenDiscovery {
    public class TokenParser {

        public Dictionary<int, Pattern> Patterns = new();

        public Dictionary<string, Pattern> PatternsByName = new();

        #region Construction and registration of patterns


        private int NextId = 0;

        public Pattern Register(Pattern pattern) {
            if (pattern.Id == -1) pattern.Id = NextId++;
            Unregister(pattern);
            Patterns[pattern.Id] = pattern;
            if (pattern.Name != null) PatternsByName[pattern.Name] = pattern;
            return pattern;
        }

        public Pattern RegisterLiteral(string name, string literalText) {
            Pattern pattern = new Pattern(this);
            pattern.Name = name;
            pattern.Literal = literalText;
            Register(pattern);
            return pattern;
        }

        public Pattern Register(string name, string patternText) {
            var pattern = NewPattern(name, patternText);
            Register(pattern);
            return pattern;
        }

        public Pattern NewPattern(string patternText) {
            return NewPattern(null, patternText);
        }

        private static Regex regexPatternToken = new(
            @"^(?<Token>( \( | \) | \| | [-_A-Za-z]+ | '('' | [^'])+' | \[\d{1,9}\] ) \s* )+$",
            RegexOptions.IgnorePatternWhitespace
        );

        public Pattern NewPattern(string name, string patternText) {
            Pattern pattern = new Pattern(this);
            pattern.Name = name;

            var matches = regexPatternToken.Matches(patternText);
            if (matches.Count == 0) throw new Exception("Syntax error in pattern");
            var tokens = new List<string>();
            foreach (var match in matches[0].Groups["Token"].Captures) {
                tokens.Add(match.ToString().Trim());
            }

            int startAt = 0;
            pattern.Head = NewPattern_Part(tokens, ref startAt);

            if (startAt < tokens.Count) throw new Exception("Expecting end of pattern around '" + tokens[startAt] + "'");

            return pattern;
        }

        private PatternPart NewPattern_Part(List<string> tokens, ref int startAt) {
            var alts = new List<List<PatternPart>>();
            string token;
            PatternPart part;

            var currentAlt = new List<PatternPart>();
            alts.Add(currentAlt);
            while (startAt < tokens.Count) {
                token = tokens[startAt];
                switch (token[0]) {
                    case ')':
                        break;
                    case '|':
                        currentAlt = new List<PatternPart>();
                        alts.Add(currentAlt);
                        startAt++;
                        break;
                    case '(':
                        startAt++;
                        part = NewPattern_Part(tokens, ref startAt);
                        currentAlt.Add(part);
                        if (startAt >= tokens.Count) throw new Exception("Unexpected end of pattern");
                        if (tokens[startAt] != ")") throw new Exception("Expecting ')' instead of '" + tokens[startAt] + "'");
                        startAt++;
                        break;
                    case '[':
                        token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        part = new PatternPart(this);
                        int tokenId = int.Parse(token);
                        if (!Patterns.TryGetValue(tokenId, out part.Pattern)) {
                            throw new Exception("No such pattern with ID = " + tokenId);
                        }
                        currentAlt.Add(part);
                        startAt++;
                        break;
                    default:
                        if (token[0] == '\'') {
                            token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        }
                        part = new PatternPart(this);
                        if (!PatternsByName.TryGetValue(token, out part.Pattern)) {
                            throw new Exception("No such pattern named '" + token + "'");
                        }
                        currentAlt.Add(part);
                        startAt++;
                        break;
                }
                if (startAt >= tokens.Count) break;
                if (tokens[startAt] == ")") break;
            }

            var thisPart = new PatternPart(this);
            if (alts.Count == 1) {
                NewPattern_PartChain(thisPart, alts[0], 0);
            } else {
                thisPart.Alternatives = new List<PatternPart>();
                foreach (var alt in alts) {
                    var altPart = new PatternPart(this);
                    NewPattern_PartChain(altPart, alt, 0);
                    thisPart.Alternatives.Add(altPart);
                }
            }

            return thisPart;
        }

        private void NewPattern_PartChain(PatternPart thisPart, List<PatternPart> partList, int index) {
            if (index >= partList.Count) return;
            var candidate = partList[index];
            if (candidate.Pattern != null && candidate.Next == null) {
                thisPart.Pattern = candidate.Pattern;
            } else {
                thisPart.Alternatives = new List<PatternPart>();
                thisPart.Alternatives.Add(candidate);
            }
            if (index + 1 < partList.Count) {
                thisPart.Next = new PatternPart(this);
                NewPattern_PartChain(thisPart.Next, partList, index + 1);
            }
        }

        public void Unregister(Pattern pattern) {
            Patterns.Remove(pattern.Id);
            var keyValue = PatternsByName.Where(e => e.Value == pattern).FirstOrDefault();
            if (keyValue.Key != null) {
                PatternsByName.Remove(keyValue.Key);
            }
        }


        #endregion

    }
}
