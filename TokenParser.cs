using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TokenDiscovery {
    public class TokenParser {

        #region Public properties


        public Dictionary<int, Pattern> Patterns = new();

        public Dictionary<string, Pattern> PatternsByName = new();


        #endregion

        public TokenParser() {
        }

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
            Pattern pattern = new Pattern(this, PatternType.Literal);
            pattern.Name = name;
            pattern.Literal = literalText;
            Register(pattern);
            return pattern;
        }

        public Pattern Register(string name, PatternType patternType, string patternText) {
            var pattern = NewPattern(name, patternType, patternText);
            Register(pattern);
            return pattern;
        }

        public Pattern NewPattern(PatternType patternType, string patternText) {
            return NewPattern(null, patternType, patternText);
        }

        private static Regex regexPatternToken = new(
            @"^\s* (?<Token>( \( | \| | \[\d{1,9}\] | [A-Za-z]+[-_A-Za-z]* | '('' | [^'])+' | \) | \^ | \! | \? | \* | \+ | \{ \s* \d+ \s* (\- \s* \d+ | \+)? \s* \} ) \s* )+$",
            RegexOptions.IgnorePatternWhitespace
        );

        public Pattern NewPattern(string name, PatternType patternType, string patternText) {
            Pattern pattern = new Pattern(this);
            pattern.Name = name;
            pattern.Type = patternType;

            try {

                var matches = regexPatternToken.Matches(patternText);
                if (matches.Count == 0) throw new Exception("Syntax error in pattern");
                var tokens = new List<string>();
                foreach (var match in matches[0].Groups["Token"].Captures) {
                    tokens.Add(match.ToString().Trim());
                }

                int startAt = 0;
                pattern.Head = NewPattern_Part(tokens, ref startAt);

                if (startAt < tokens.Count) throw new Exception("Expecting end of pattern around '" + tokens[startAt] + "'");
            } catch (Exception ex) {
                if (name != null) {
                    throw new Exception("Error parsing '" + name + "' pattern: " + ex.Message, ex);
                } else {
                    throw new Exception("Error parsing nameless pattern: " + ex.Message, ex);
                }
            }

            return pattern;
        }

        private PatternPart NewPattern_Part(List<string> tokens, ref int startAt) {
            var alts = new List<List<PatternPart>>();
            string token;
            PatternPart currentPart = null;
            bool nonePrior = false;

            var currentAlt = new List<PatternPart>();
            alts.Add(currentAlt);
            while (startAt < tokens.Count) {
                token = tokens[startAt];
                switch (token[0]) {
                    case '|':
                        if (nonePrior) throw new Exception("Found ^ before " + token);
                        if (currentAlt.Count == 0) throw new Exception("Found empty alternative before '|'");
                        currentPart = null;
                        currentAlt = new List<PatternPart>();
                        alts.Add(currentAlt);
                        startAt++;
                        break;
                    case '(':
                        startAt++;
                        currentPart = NewPattern_Part(tokens, ref startAt);
                        if (nonePrior) {
                            currentPart.NonePrior = true;
                            nonePrior = false;
                        }
                        currentAlt.Add(currentPart);
                        if (startAt >= tokens.Count) throw new Exception("Unexpected end of pattern");
                        if (tokens[startAt] != ")") throw new Exception("Expecting ')' instead of '" + tokens[startAt] + "'");
                        startAt++;
                        break;

                    case '^':
                        nonePrior = true;
                        startAt++;
                        break;

                    case '!':
                    case '?':
                    case '*':
                    case '+':
                    case '{':
                        if (nonePrior) throw new Exception("Found ^ before " + token);
                        if (currentPart == null) {
                            if (startAt == 0) {
                                throw new Exception("Found '" + token + "' quantifier at pattern start");
                            } else {
                                throw new Exception("Found '" + token + "' quantifier after non-token: " + tokens[startAt - 1]);
                            }
                        }

                        if (currentPart.Next != null) {
                            var innerPart = new PatternPart(this);
                            innerPart.Import(currentPart);
                            currentPart.Clear();
                            currentPart.Alternatives = new();
                            currentPart.Alternatives.Add(innerPart);
                        }

                        if (token == "!") {
                            currentPart.MinQuantity = 0;
                            currentPart.MaxQuantity = 0;
                        } else if (token == "?") {
                            currentPart.MinQuantity = 0;
                        } else if (token == "*") {
                            currentPart.MinQuantity = 0;
                            currentPart.MaxQuantity = -1;  // Unlimited
                        } else if (token == "+") {
                            currentPart.MinQuantity = 1;
                            currentPart.MaxQuantity = -1;  // Unlimited
                        } else {
                            var strippedToken = token.Substring(1, token.Length - 2).Replace(" ", "");
                            if (strippedToken.Contains("-")) {
                                var parts = strippedToken.Split("-");
                                currentPart.MinQuantity = int.Parse(parts[0]);
                                currentPart.MaxQuantity = int.Parse(parts[1]);
                                if (currentPart.MinQuantity > currentPart.MaxQuantity) {
                                    throw new Exception("Range quantifier cannot have max less than min: " + token);
                                }
                            } else if (strippedToken.EndsWith("+")) {
                                currentPart.MinQuantity = int.Parse(strippedToken.Substring(0, strippedToken.Length - 1));
                                currentPart.MaxQuantity = -1;  // Unlimited
                            } else {
                                currentPart.MinQuantity = int.Parse(strippedToken);
                                currentPart.MaxQuantity = currentPart.MinQuantity;
                            }
                        }
                        currentPart = null;
                        startAt++;
                        break;

                    case ')':
                        if (nonePrior) throw new Exception("Found ^ before " + token);
                        break;
                    case '[':
                        token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        currentPart = new PatternPart(this);
                        if (nonePrior) {
                            currentPart.NonePrior = true;
                            nonePrior = false;
                        }
                        int tokenId = int.Parse(token);
                        if (!Patterns.TryGetValue(tokenId, out currentPart.Pattern)) {
                            throw new Exception("No such pattern with ID = " + tokenId);
                        }
                        currentAlt.Add(currentPart);
                        startAt++;
                        break;
                    default:
                        if (token[0] == '\'') {
                            token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        }
                        currentPart = new PatternPart(this);
                        if (nonePrior) {
                            currentPart.NonePrior = true;
                            nonePrior = false;
                        }
                        if (!PatternsByName.TryGetValue(token, out currentPart.Pattern)) {
                            throw new Exception("No such pattern named '" + token + "'");
                        }
                        currentAlt.Add(currentPart);
                        startAt++;
                        break;
                }
                if (startAt >= tokens.Count) break;
                if (tokens[startAt] == ")") {
                    if (currentAlt.Count == 0) throw new Exception("Found empty alternative before ')'");
                    break;
                }
            }

            var thisPart = new PatternPart(this);
            thisPart.Alternatives = new List<PatternPart>();
            foreach (var alt in alts) {
                var altPart = new PatternPart(this);
                NewPattern_PartChain(altPart, alt, 0);
                thisPart.Alternatives.Add(altPart);
            }

            return thisPart;
        }

        private void NewPattern_PartChain(PatternPart thisPart, List<PatternPart> partList, int index) {
            if (index >= partList.Count) return;
            var candidate = partList[index];
            if (candidate.Pattern != null && candidate.Next == null) {
                thisPart.Import(candidate);

            } else if (
                (candidate.MinQuantity != 1 || candidate.MaxQuantity != 1) &&
                candidate.Alternatives.Count == 1 &&
                candidate.Alternatives[0].Pattern != null &&
                candidate.Alternatives[0].Next == null &&
                !candidate.Alternatives[0].NonePrior &&
                candidate.Alternatives[0].MinQuantity == 1 &&
                candidate.Alternatives[0].MaxQuantity == 1
            ) {
                var alt = candidate.Alternatives[0];
                candidate.Pattern = alt.Pattern;
                candidate.Alternatives = null;
                thisPart.Import(candidate);

            } else if (
                candidate.Alternatives.Count == 1 &&
                !candidate.NonePrior &&
                candidate.MinQuantity == 1 &&
                candidate.MaxQuantity == 1
            ) {
                var innerPart = candidate.Alternatives[0];
                thisPart.Import(innerPart);

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

        public void RegisterBasics() {
            string allChars = "";

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                string s = "" + (char)i;
                RegisterLiteral(s, s);

                if (i > 32) allChars += " | ";
                allChars += PatternsByName[s].Identity;
            }

            string uppers = "";
            string lowers = "";
            for (char c = 'A'; c <= 'Z'; c++) {
                string S = "" + c;
                string s = S.ToLower();
                Register(S + s, PatternType.Basics, S + "|" + s);

                if (uppers != "") uppers += "|";
                uppers += S;
                if (lowers != "") lowers += "|";
                lowers += s;
            }
            Register("Uppercase", PatternType.Basics, uppers);
            Register("Lowercase", PatternType.Basics, lowers);
            Register("Letters", PatternType.Basics, "Uppercase | Lowercase");
            Register("All_chars", PatternType.Trivial, allChars);

        }


        #endregion

    }
}
