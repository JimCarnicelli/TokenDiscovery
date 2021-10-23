using System;
using System.Collections.Generic;
using System.IO;
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

        public Pattern RegisterLiteral(int id, string name, string literalText) {
            Pattern pattern = new Pattern(this, PatternType.Literal);
            if (id != -1) pattern.Id = id;
            pattern.Name = name;
            pattern.Literal = literalText;
            Register(pattern);
            return pattern;
        }

        public Pattern Register(Pattern pattern) {
            if (pattern.Name != null && PatternsByName.ContainsKey(pattern.Name)) {
                throw new Exception("Already a pattern named '" + pattern.Name + "'");
            }
            if (pattern.Id == -1) pattern.Id = NextId++;
            Unregister(pattern);
            Patterns[pattern.Id] = pattern;
            if (pattern.Name != null) PatternsByName[pattern.Name] = pattern;
            return pattern;
        }

        public Pattern Register(string name, string patternText) {
            return Register(name, PatternType.Experimental, patternText);
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
            @"^\s* (?<Token>( \( | \| | \[\d{1,9}\] | [A-Za-z]+[-_A-Za-z]* | '('' | [^'])+' | \) | \< | \> | \! | \? | \* | \+ | \{ \s* \d+ \s* (\- \s* \d+ | \+)? \s* \} ) \s* )+$",
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
                pattern.Root = NewPattern_Element(tokens, ref startAt);

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

        private PatternElement NewPattern_Element(List<string> tokens, ref int startAt) {
            var thisElem = new PatternElement(this);
            var alts = thisElem.Alternatives = new List<List<PatternElement>>();

            string token;
            PatternElement currentElem = null;
            Look look = Look.Here;

            var sequence = new List<PatternElement>();
            alts.Add(sequence);
            while (startAt < tokens.Count) {
                token = tokens[startAt];
                switch (token[0]) {
                    case '|':
                        if (look != Look.Here) throw new Exception("Found " + (look == Look.Behind ? "<" : ">") + " before " + token);
                        if (sequence.Count == 0) throw new Exception("Found empty sequence before '|'");
                        currentElem = null;
                        sequence = new List<PatternElement>();
                        alts.Add(sequence);
                        startAt++;
                        break;
                    case '(':
                        startAt++;
                        currentElem = NewPattern_Element(tokens, ref startAt);
                        currentElem.Look = look;
                        look = Look.Here;
                        sequence.Add(currentElem);
                        if (startAt >= tokens.Count) throw new Exception("Unexpected end of pattern");
                        if (tokens[startAt] != ")") throw new Exception("Expecting ')' instead of '" + tokens[startAt] + "'");
                        startAt++;
                        break;

                    case '<':
                        if (look != Look.Here) throw new Exception("Found " + (look == Look.Behind ? "<" : ">") + " before " + token);
                        look = Look.Behind;
                        startAt++;
                        break;
                    case '>':
                        if (look != Look.Here) throw new Exception("Found " + (look == Look.Behind ? "<" : ">") + " before " + token);
                        look = Look.Ahead;
                        startAt++;
                        break;

                    case '!':
                    case '?':
                    case '*':
                    case '+':
                    case '{':
                        if (look != Look.Here) throw new Exception("Found " + (look == Look.Behind ? "<" : ">") + " before " + token);
                        if (currentElem == null) {
                            if (startAt == 0) {
                                throw new Exception("Found '" + token + "' quantifier at pattern start");
                            } else {
                                throw new Exception("Found '" + token + "' quantifier after non-token: " + tokens[startAt - 1]);
                            }
                        }

                        if (token == "!") {
                            currentElem.MinQuantity = 0;
                            currentElem.MaxQuantity = 0;
                        } else if (token == "?") {
                            currentElem.MinQuantity = 0;
                        } else if (token == "*") {
                            currentElem.MinQuantity = 0;
                            currentElem.MaxQuantity = -1;  // Unlimited
                        } else if (token == "+") {
                            currentElem.MinQuantity = 1;
                            currentElem.MaxQuantity = -1;  // Unlimited
                        } else {
                            var strippedToken = token.Substring(1, token.Length - 2).Replace(" ", "");
                            if (strippedToken.Contains("-")) {
                                var parts = strippedToken.Split("-");
                                currentElem.MinQuantity = int.Parse(parts[0]);
                                currentElem.MaxQuantity = int.Parse(parts[1]);
                                if (currentElem.MinQuantity > currentElem.MaxQuantity) {
                                    throw new Exception("Range quantifier cannot have max less than min: " + token);
                                }
                            } else if (strippedToken.EndsWith("+")) {
                                currentElem.MinQuantity = int.Parse(strippedToken.Substring(0, strippedToken.Length - 1));
                                currentElem.MaxQuantity = -1;  // Unlimited
                            } else {
                                currentElem.MinQuantity = int.Parse(strippedToken);
                                currentElem.MaxQuantity = currentElem.MinQuantity;
                            }
                        }
                        currentElem = null;
                        startAt++;
                        break;

                    case ')':
                        if (look != Look.Here) throw new Exception("Found " + (look == Look.Behind ? "<" : ">") + " before " + token);
                        break;
                    case '[':
                        token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        currentElem = new PatternElement(this);
                        currentElem.Look = look;
                        look = Look.Here;
                        int tokenId = int.Parse(token);
                        if (!Patterns.TryGetValue(tokenId, out currentElem.Pattern)) {
                            throw new Exception("No such pattern with ID = " + tokenId);
                        }
                        sequence.Add(currentElem);
                        startAt++;
                        break;
                    default:
                        if (token[0] == '\'') {
                            token = token.Substring(1, token.Length - 2).Replace("''", "'");
                        }
                        currentElem = new PatternElement(this);
                        currentElem.Look = look;
                        look = Look.Here;
                        if (!PatternsByName.TryGetValue(token, out currentElem.Pattern)) {
                            throw new Exception("No such pattern named '" + token + "'");
                        }
                        sequence.Add(currentElem);
                        startAt++;
                        break;
                }
                if (startAt >= tokens.Count) break;
                if (tokens[startAt] == ")") {
                    if (sequence.Count == 0) throw new Exception("Found empty sequence before ')'");
                    break;
                }
            }

            NewPattern_Reduce(thisElem);
            return thisElem;
        }

        void NewPattern_Reduce(PatternElement elem) {
            if (elem.Alternatives == null) return;

            //TODO: Prevent complex look-behinds

            // Apply the associative property by reducing needless parentheses. Eg "A (B C)" to "A B C".

            foreach (var alt in elem.Alternatives) {

                // Reduce each of my children
                foreach (var childElem in alt) {
                    NewPattern_Reduce(childElem);
                }

                // Reduce needless sub-sequences
                for (int i = 0; i < alt.Count; i++) {
                    var childElem = alt[i];
                    if (
                        childElem.Alternatives != null &&
                        childElem.Alternatives.Count == 1 &&
                        childElem.MinQuantity == 1 &&
                        childElem.MaxQuantity == 1 &&
                        childElem.Look == Look.Here
                    ) {
                        alt.InsertRange(i, childElem.Alternatives[0]);
                        alt.Remove(childElem);
                    }
                }
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

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                string s = "" + (char)i;
                RegisterLiteral(-1, s, s);
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
            Register("Letter", PatternType.Basics, "Uppercase | Lowercase");
        }

        public void LoadPatterns(string path) {
            Patterns.Clear();
            PatternsByName.Clear();

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++) {
                var parts = lines[i].Split(" | ", 4);
                if (parts[2] == "Literal") {
                    RegisterLiteral(
                        int.Parse(parts[0]),
                        Pattern.Unescape(parts[1]),
                        Pattern.Unescape(parts[3])
                    );
                } else {
                    var pattern = NewPattern(
                        parts[1] == "" ? null : Pattern.Unescape(parts[1]),
                        (PatternType)Enum.Parse(typeof(PatternType), parts[2]),
                        parts[3]
                    );
                    pattern.Id = int.Parse(parts[0]);
                    Register(pattern);
                }
            }
        }

        public void SavePatterns(string path) {
            var lines = new List<string>();

            lines.Add("Id | Name | Type | Pattern");
            foreach (var pattern in Patterns.Values) {
                lines.Add(pattern.Id + " | " + pattern.Identity + " | " + pattern.Type + " | " + pattern.Describe());
            }

            File.WriteAllLines(path, lines);
        }


        #endregion

        #region Parsing text


        public TokenChain Parse(string text) {
            var chain = new TokenChain(text);
            for (int i = 0; i < text.Length; i++) {
                foreach (var pattern in Patterns.Values) {
                    Parse(chain, i, pattern);
                }
            }
            return chain;
        }

        public void Parse(TokenChain chain, int startAt, Pattern pattern) {
            pattern.Match(chain, startAt);
        }


        #endregion

    }
}
