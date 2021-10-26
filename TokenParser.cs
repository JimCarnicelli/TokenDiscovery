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

        public Pattern Register(string name, PatternType patternType, string patternText) {
            var pattern = NewPattern(name, patternType, patternText);
            Register(pattern);
            return pattern;
        }

        public bool PatternExists(string patternText, out Pattern pattern) {
            // Parse the text into a pattern and then render it back to text
            var newPattern = NewPattern(patternText);
            var newPatternDescription = newPattern.Describe(false, true);

            if (newPattern.Describe() == "(Word Space)+ Word") {
                var x = 1;
            }

            //Console.WriteLine("- " + patternText + "  ->  " + newPatternDescription);

            // Check to see if this pattern already exists
            pattern = Patterns.Values.Where(e => e.Describe(false, true) == newPatternDescription).FirstOrDefault();
            if (pattern != null) return true;

            // It doesn't. So we'll return the newly constructed pattern anyway.
            pattern = newPattern;
            return false;
        }

        public Pattern RegisterExperiment(string patternText) {
            if (PatternExists(patternText, out Pattern pattern)) return pattern;
            return Register(pattern);
        }

        public Pattern NewPattern(PatternType patternType, string patternText) {
            return NewPattern(null, patternType, patternText);
        }

        private static Regex regexPatternToken = new(
            @"^\s* (?<Token>( \( | \| | \[\d{1,9}\] | [A-Za-z]+[-_A-Za-z]* | '('' | [^'])+' | \) | \< | \> | \! | \? | \* | \+ | \{ \s* \d+ \s* (\- \s* \d+ | \+)? \s* \} ) \s* )+$",
            RegexOptions.IgnorePatternWhitespace
        );

        public Pattern NewPattern(string patternText) {
            return NewPattern(null, PatternType.Experimental, patternText);
        }

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

            if (elem.Look == Look.Behind) {
                ConstraintLookBehind(elem);
            }

        }

        private void ConstraintLookBehind(PatternElement elem) {
            foreach (var alt in elem.Alternatives) {
                foreach (var childElem in alt) {

                    if (childElem.MinQuantity != 1) throw new Exception("Found quantifier within look-behind");
                    if (childElem.MaxQuantity != 1) throw new Exception("Found custom quantifier within look-behind");
                    if (childElem.Look == Look.Behind) throw new Exception("Found look-behind within look-behind");
                    if (childElem.Look == Look.Ahead) throw new Exception("Found look-ahead within look-behind");

                    NewPattern_Reduce(childElem);
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
                if (i == 32) {
                    string s = "" + (char)i;
                    RegisterLiteral(-1, "Space", s);
                } else {
                    string s = "" + (char)i;
                    RegisterLiteral(-1, s, s);
                }
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
                lines.Add(
                    pattern.Id + " | " +
                    (pattern.Name == null ? "" : pattern.Identity) + " | " +
                    pattern.Type + " | " +
                    pattern.Describe()
                );
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

        #region Experiment-based pattern learning


        private Dictionary<string, List<string>> SurveySequences = new();

        private Dictionary<string, bool> OldSurveySequences = new();

        /// <summary>
        /// Clears the survey-generated statistics in preparation for starting fresh
        /// </summary>
        public void ClearSurvey() {
            foreach (var pattern in Patterns.Values) {
                pattern.SurveyMatchCount = 0;
                pattern.SurveyLongest = 0;
                pattern.SurveyCoverage = 0;
                pattern.SurveyStretch = 0;
                SurveySequences.Clear();
            }
        }

        public void ClearAllSurveys() {
            ClearSurvey();
            foreach (var pattern in Patterns.Values) {
                pattern.SurveyExamples = null;
            }
            OldSurveySequences.Clear();
        }

        /// <summary>
        /// Collect some usage statistics about the patterns found in the given token chain created by parsing
        /// </summary>
        public void Survey(TokenChain chain) {
            var coverages = new Dictionary<Pattern, List<int>>();

            for (int i = 0; i < chain.Length; i++) {
                foreach (var token in chain.Heads[i].Values) {
                    var pattern = token.Pattern;

                    if (i == 0) {
                        pattern.SurveyStretch += token.Length;
                    }

                    // Collect examples
                    if (pattern.SurveyExamples == null) pattern.SurveyExamples = new();
                    if (!pattern.SurveyExamples.Where(e => e == token.Text).Any()) {
                        pattern.SurveyExamples.Add(token.Text);
                    }

                    // Count the number of matches
                    pattern.SurveyMatchCount++;

                    // Find the longest example
                    if (token.Length > pattern.SurveyLongest) pattern.SurveyLongest = token.Length;

                    // Count how many of the total characters are covered by these matches
                    if (!coverages.TryGetValue(pattern, out List<int> patternCoverage)) {
                        patternCoverage = new List<int>();
                        coverages[pattern] = patternCoverage;
                    }
                    for (int j = token.StartAt; j < token.StartAt + token.Length; j++) {
                        if (!patternCoverage.Contains(j)) patternCoverage.Add(j);
                    }
                }
            }

            foreach (var pattern in coverages.Keys) {
                pattern.SurveyCoverage += coverages[pattern].Count;
            }

            for (int i = 0; i < chain.Length - 1; i++) {
                foreach (var firstToken in chain.Heads[i].Values) {
                    int secondStartAt = firstToken.StartAt + firstToken.Length;
                    if (secondStartAt >= chain.Length) continue;
                    foreach (var secondToken in chain.Heads[secondStartAt].Values) {
                        string firstSecondKey = firstToken.Pattern.Id + "|" + secondToken.Pattern.Id;
                        // Don't waste time with old observations
                        if (!OldSurveySequences.ContainsKey(firstSecondKey)) {
                            if (!SurveySequences.TryGetValue(firstSecondKey, out List<string> firstSecondExamples)) {
                                firstSecondExamples = new List<string>();
                                SurveySequences[firstSecondKey] = firstSecondExamples;
                            }
                            firstSecondExamples.Add(firstToken.Text + secondToken.Text);
                        }

                        /*
                        int thirdStartAt = secondToken.StartAt + secondToken.Length;
                        if (thirdStartAt >= chain.Length) continue;
                        foreach (var thirdToken in chain.Heads[thirdStartAt].Values) {
                            string firstSecondThirdKey = firstSecondKey + "|" + thirdToken.Pattern.Id;
                            // Don't waste time with old observations
                            if (!OldSurveySequences.ContainsKey(firstSecondThirdKey)) {
                                if (!SurveySequences.TryGetValue(firstSecondThirdKey, out List<string> firstSecondThirdExamples)) {
                                    firstSecondThirdExamples = new List<string>();
                                    SurveySequences[firstSecondThirdKey] = firstSecondThirdExamples;
                                }
                                firstSecondThirdExamples.Add(firstToken.Text + secondToken.Text + thirdToken.Text);
                            }
                        }
                        */
                    }
                }
            }
        }

        /// <summary>
        /// Debugging-oriented output of some survey results
        /// </summary>
        public void SurveyResults() {
            var sortedList = Patterns.Values
                .Where(e => e.Type >= PatternType.Basics && e.SurveyMatchCount > 0)
                .OrderByDescending(e => e.SurveyStretch)
                .Take(200);
            foreach (var pattern in sortedList) {
                Console.WriteLine(
                    "- " + pattern.SurveyStretch.ToString("#,##0") +
                    " - [" + pattern.Id + "]" +
                    " - " + pattern
                );
                var examples = pattern.SurveyExamples
                    .OrderByDescending(e => e.Length)
                    .Take(3);
                foreach (string example in examples) {
                    Console.WriteLine("  | '" + example + "'");
                }
            }
        }

        public void CullExperiments() {
            return;
            var sortedPatterns = Patterns.Values
                .Where(e => e.Type == PatternType.Experimental && e.SurveyMatchCount > 0)
                .OrderByDescending(e => e.SurveyCoverage - e.Penalty)
                .Skip(30)  // We'll keep the current best
                .ToList();
            //Console.WriteLine("Culling " + sortedPatterns.Count() + " unproductive patterns\n");
            foreach (var pattern in sortedPatterns) {
                // Don't delete this one if anyone else depends on it
                if (Patterns.Values.Where(e => e.DependsOn(pattern)).Any()) continue;
                Unregister(pattern);
            }
        }

        public void ProposePatterns() {

            // Pairs ("A B")
            {
                var sortedSequences = SurveySequences
                    .Where(e => e.Key.Split('|').Length == 2 && e.Value.Count > 3)
                    .OrderByDescending(e => e.Value.Count)
                    .Take(50);
                foreach (var keyValue in sortedSequences) {
                    var parts = keyValue.Key.Split('|');
                    var examples = keyValue.Value;
                    var firstPattern = Patterns[int.Parse(parts[0])];
                    var secondPattern = Patterns[int.Parse(parts[1])];

                    // "A A"
                    if (parts[0] == parts[1]) {
                        RegisterExperiment("[" + firstPattern.Id + "]+");
                    } else {  // "A B"
                        var pattern = RegisterExperiment("[" + firstPattern.Id + "] [" + secondPattern.Id + "]");
                    }

                    /*
                    Console.WriteLine("- (2) " + firstPattern.Identity + " " + secondPattern.Identity + ": " + examples.Count);
                    var sortedExamples = examples
                        .OrderByDescending(e => e.Length)
                        .Take(3);
                    foreach (string example in sortedExamples) {
                        Console.WriteLine("  | " + example);
                    }
                    */

                    // Discard these sequences we discovered in this round so we don't waste time on them in later iterations
                    OldSurveySequences[keyValue.Key] = true;

                }
            }

            // Triples ("A B C")
            if (false) {
                var sortedSequences = SurveySequences
                    .Where(e => e.Key.Split('|').Length == 3)
                    .OrderByDescending(e => e.Value.Count)
                    .Take(20);
                foreach (var keyValue in sortedSequences) {
                    var parts = keyValue.Key.Split('|');
                    var examples = keyValue.Value;
                    var firstPattern = Patterns[int.Parse(parts[0])];
                    var secondPattern = Patterns[int.Parse(parts[1])];
                    var thirdPattern = Patterns[int.Parse(parts[2])];

                    // "A B B" type pattern
                    if (parts[0] != parts[1] && parts[1] == parts[2]) {
                        RegisterExperiment("[" + firstPattern.Id + "] [" + thirdPattern.Id + "]+");
                        RegisterExperiment("<[" + secondPattern.Id + "]! [" + thirdPattern.Id + "]+");
                    }

                    // "A A B" type pattern
                    if (parts[0] == parts[1] && parts[1] != parts[2]) {
                        RegisterExperiment("[" + firstPattern.Id + "]+ [" + thirdPattern.Id + "]");
                        RegisterExperiment("[" + firstPattern.Id + "]+ >[" + thirdPattern.Id + "]!");
                    }

                    /*
                    Console.WriteLine("- (3) " + firstPattern.Identity + " " + secondPattern.Identity + " " + thirdPattern.Identity + ": " + examples.Count);
                    var sortedExamples = examples
                        .OrderByDescending(e => e.Length)
                        .Take(3);
                    foreach (string example in sortedExamples) {
                        Console.WriteLine("  | " + example);
                    }
                    */

                    // Discard these sequences we discovered in this round so we don't waste time on them in later iterations
                    OldSurveySequences[keyValue.Key] = true;
                }
            }

        }


        #endregion

    }
}
