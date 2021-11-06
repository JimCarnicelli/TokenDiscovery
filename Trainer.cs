using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {

    /// <summary>
    /// Responsible for training the parser by proposing and evaluating patterns based on sample texts
    /// </summary>
    public class Trainer {

        #region Public properties


        public Parser parser = new Parser();

        public List<string> Paragraphs;

        public int Iterations = 1;


        #endregion

        public void Initialize() {

            parser.Initialize();

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                if (i == ' ') {
                    parser.RegisterLiteral(-1, "Space", " ");
                } else if (i == '|') {
                    parser.RegisterLiteral(-1, "Pipe", "|");
                } else if (i == '\'') {
                    parser.RegisterLiteral(-1, "Apostrophe", "'");
                } else {
                    string s = "" + (char)i;
                    parser.RegisterLiteral(-1, s, s);
                }
            }

            string uppers = "";
            string lowers = "";
            for (char c = 'A'; c <= 'Z'; c++) {
                string S = "" + c;
                string s = S.ToLower();
                parser.Register(S + s, PatternType.Basics, S + "|" + s);

                if (uppers != "") uppers += "|";
                uppers += S;
                if (lowers != "") lowers += "|";
                lowers += s;
            }
            parser.Register("Uppercase", PatternType.Basics, uppers);
            parser.Register("Lowercase", PatternType.Basics, lowers);
            parser.Register("Letter", PatternType.Basics, "Uppercase | Lowercase");

            string digits = "";
            for (char c = '0'; c <= '9'; c++) {
                if (c > '0') digits += "|";
                digits += "'" + c + "'";
            }
            parser.Register("Digit", PatternType.Basics, digits);
        }

        public void ImportSourceText(string sourceText) {
            // Pre-parse the raw text into a set of paragraphs with some text cleanup

            // Split on 2 or more newlines
            sourceText = sourceText.Replace("\r\n", "\n").Replace("\r", "\n");
            while (sourceText.Contains("\n\n\n")) sourceText = sourceText.Replace("\n\n\n", "\n\n");
            while (sourceText.StartsWith("\n")) sourceText = sourceText.Substring(1);
            while (sourceText.EndsWith("\n")) sourceText = sourceText.Substring(0, sourceText.Length - 1);
            Paragraphs = new List<string>();
            var rawParagraphs = sourceText.Split("\n\n");
            for (int i = 0; i < rawParagraphs.Length; i++) {
                var paragraphText = rawParagraphs[i];
                // Collapse multiple lines into one long one
                paragraphText = paragraphText.Replace("\n", " ");

                // Translate some known non-allowed characters to allowed equivalents
                paragraphText = paragraphText.Replace("\t", " ");  // Tab
                paragraphText = paragraphText.Replace("—", "--");  // Em dash
                paragraphText = paragraphText.Replace("–", "-");  // En dash

                // Collapse multiple spaces down to single spaces
                while (paragraphText.Contains("  ")) paragraphText = paragraphText.Replace("  ", " ");
                // Trim leading and trailing whitespace
                while (paragraphText.StartsWith(" ")) paragraphText = paragraphText.Substring(1);
                while (paragraphText.EndsWith(" ")) paragraphText = paragraphText.Substring(0, paragraphText.Length - 1);

                // Validate the characters
                for (int j = 0; j < paragraphText.Length; j++) {
                    char c = paragraphText[j];
                    if (!parser.AllowableCharacters.ContainsKey(c)) {
                        int startAt = j - 20;
                        int endAt = j + 20;
                        if (startAt < 0) startAt = 0;
                        if (endAt >= paragraphText.Length - 1) endAt = paragraphText.Length - 1;
                        string badText = paragraphText.Substring(startAt, endAt - startAt + 1);
                        throw new Exception("Found a non-allowed character '" + c + "' in paragraph " + (i + 1) + ": \"" + badText + "\"");
                    }
                }

                Paragraphs.Add(paragraphText);
            }

        }

        public void Train() {
            for (int i = 1; i <= Iterations; i++) {
                TrainIteration(i);
            }
        }

        public void TrainIteration(int iteration) {
            Console.WriteLine("\n@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ Iteration " + iteration + " @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\n");
            for (int i = 0; i < Paragraphs.Count; i++) {
                TrainParagraph(iteration, i);
                break;
            }
        }

        public void TrainParagraph(int iteration, int paragraphIndex) {
            string paragraph = Paragraphs[paragraphIndex];

            // Let's see what we can already parse with existing rules
            var chain = parser.Parse(paragraph);

            FindHoles(chain);
            if (!ProposeNewPatterns(chain)) {
                // TODO: Something
                //Console.WriteLine("I have no idea what to do here!");
            }

        }

        public void FindHoles(TokenChain chain) {
            var runningTokens = new List<Token>();
            Hole hole = null;

            for (int i = 0; i < chain.Length; i++) {
                foreach (var token in chain.Heads[i].Values) {
                    if (token.Pattern.Type < PatternType.Derived) continue;
                    runningTokens.Add(token);
                }
                if (runningTokens.Count == 0) {
                    if (hole == null) {
                        hole = new Hole();
                        hole.StartAt = i;
                        hole.Length = 0;
                    }
                    hole.Length++;
                    hole.Text += chain.Text[i];
                } else {
                    if (hole != null) {
                        chain.Holes.Add(hole);
                        hole = null;
                    }
                }
                foreach (var token in chain.Tails[i].Values) {
                    if (token.Pattern.Type < PatternType.Derived) continue;
                    runningTokens.Remove(token);
                }
            }

            if (hole != null) chain.Holes.Add(hole);
        }

        public bool ProposeNewPatterns(TokenChain chain) {

            #region Find stretches of repeating patterns

            var repetitions = new Dictionary<string, List<TokenStretch>>();

            // Inch along looking for starts of repetition stretches
            for (int startAt = 0; startAt < chain.Length; startAt++) {
                // Consider each of the potential starting point
                foreach (var firstToken in chain.Heads[startAt].Values) {

                    // Find the set of stretches that we'll add a new stretch to
                    string key = "" + firstToken.Pattern.Id;
                    if (!repetitions.TryGetValue(key, out List<TokenStretch> stretches)) {
                        stretches = new();
                    }
                    // Don't even bother looking for a stretch here that we already found earlier
                    if (stretches.Where(e => startAt < e.StartAt + e.Length).Any()) continue;

                    // We haven't found this stretch previously, so let's create it
                    var stretch = new TokenStretch() { firstToken };
                    int nextPos = firstToken.StartAt + firstToken.Length;
                    while (nextPos < chain.Length) {
                        var nextToken = chain.Heads[nextPos].Values
                            .Where(e => e.Pattern.Id == firstToken.Pattern.Id)
                            .FirstOrDefault();
                        if (nextToken == null) break;
                        stretch.Add(nextToken);
                        nextPos = nextToken.StartAt + nextToken.Length;
                    }

                    // Did we find more than one repetition?
                    if (stretch.Count > 1) {

                        string patternText = "<" + firstToken.Pattern.Identity + "! " + firstToken.Pattern.Identity + "+";
                        var pattern = parser.NewPattern(patternText);
                        patternText = pattern.Describe(false, true);
                        if (!parser.Patterns.Values.Where(e => e.Describe(false, true) == patternText).Any()) {
                            // Let's add it and register this pattern as having one or more stretches of repetitions
                            stretches.Add(stretch);
                            repetitions[key] = stretches;
                        }
                    }

                }
            }

            if (repetitions.Count > 0) {

                /*
                Console.WriteLine("Repetitions:");
                foreach (var stretches in repetitions.Values
                    .OrderByDescending(e => e.Count)  // Most stretches first
                    .ThenByDescending(e => e.Sum(f => f.Length))  // Most characters covered by these stretches first
                ) {
                    Console.WriteLine("- " + stretches[0][0].Pattern.Identity + "+ x " + stretches.Count + " (" + stretches.Sum(e => e.Length) + ")");
                }
                */

                // Pick the best
                var best = repetitions.Values
                    .OrderByDescending(e => e.Sum(f => f.Length))  // Best coverage
                    .First();
                // Propose a new pattern
                var firstPattern = best[0][0].Pattern;
                string patternText = "<" + firstPattern.Identity + "! " + firstPattern.Identity + "+";
                Console.WriteLine("Proposing a new pattern: " + patternText);
                parser.RegisterExperiment(patternText);

                //return true;  // One innovation
            }

            #endregion

            #region Find stretches of repeating alternations of patterns

            var repetitingAlternations = new Dictionary<string, List<TokenStretch>>();

            // Inch along looking for starts of repetition stretches
            for (int startAt = 0; startAt < chain.Length; startAt++) {
                // Consider each of the potential starting point
                foreach (var firstToken in chain.Heads[startAt].Values) {
                    int nextPos = firstToken.StartAt + firstToken.Length;
                    if (nextPos >= chain.Length) continue;

                    // The two alternatives have to be different and not subset/superset
                    if (chain.Heads[nextPos].ContainsKey(firstToken.Pattern.Id)) continue;

                    foreach (var secondToken in chain.Heads[nextPos].Values) {

                        // Find the set of stretches that we'll add a new stretch to
                        string key = firstToken.Pattern.Id + "|" + secondToken.Pattern.Id;
                        if (secondToken.Pattern.Id < firstToken.Pattern.Id) {
                            key = secondToken.Pattern.Id + "|" + firstToken.Pattern.Id;
                        }
                        if (!repetitingAlternations.TryGetValue(key, out List<TokenStretch> stretches)) {
                            stretches = new();
                        }
                        // Don't even bother looking for a stretch here that we already found earlier
                        if (stretches.Where(e => startAt < e.StartAt + e.Length).Any()) continue;

                        // We haven't found this stretch previously, so let's create it
                        var stretch = new TokenStretch() { firstToken, secondToken };
                        nextPos = secondToken.StartAt + firstToken.Length;
                        if (nextPos >= chain.Length) continue;
                        while (nextPos < chain.Length) {
                            var nextToken = chain.Heads[nextPos].Values
                                .Where(e => e.Pattern.Id == (
                                    stretch.Count % 2 == 0
                                        ? firstToken.Pattern.Id  // Even
                                        : secondToken.Pattern.Id  // Odd
                                ))
                                .FirstOrDefault();
                            if (nextToken == null) break;
                            stretch.Add(nextToken);
                            nextPos = nextToken.StartAt + nextToken.Length;
                        }

                        if (stretch.Count >= 4) {

                            string patternText = null;
                            if (stretch.Count % 2 == 0) {  // Even
                                patternText = "(" + firstToken.Pattern.Identity + " " + secondToken.Pattern.Identity + ")+";
                            } else { // Odd
                                patternText = firstToken.Pattern.Identity + " (" + secondToken.Pattern.Identity + " " + firstToken.Pattern.Identity + ")*";
                            }

                            var pattern = parser.NewPattern(patternText);
                            patternText = pattern.Describe(false, true);
                            if (!parser.Patterns.Values.Where(e => e.Describe(false, true) == patternText).Any()) {
                                // Let's add it and register this pattern as having one or more stretches of repetitions
                                stretches.Add(stretch);
                                repetitingAlternations[key] = stretches;
                            }
                        }

                    }

                }
            }

            if (repetitingAlternations.Count > 0) {

                /*
                Console.WriteLine("Repetitions:");
                foreach (var stretches in repetitingAlternations.Values
                    .OrderByDescending(e => e.Count)  // Most stretches first
                    .ThenByDescending(e => e.Sum(f => f.Length))  // Most characters covered by these stretches first
                ) {
                    Console.WriteLine("- " + stretches[0][0].Pattern.Identity + "+ x " + stretches.Count + " (" + stretches.Sum(e => e.Length) + ")");
                }
                */

                // Pick the best
                var best = repetitingAlternations.Values
                    .OrderByDescending(e => e.Sum(f => f.Length))  // Best coverage
                    .First();
                // Propose a new pattern
                var firstPattern = best[0][0].Pattern;
                var secondPattern = best[0][1].Pattern;

                if (best.Where(e => e.Count % 2 == 0).Any()) {  // Even
                    string patternText = "(" + firstPattern.Identity + " " + secondPattern.Identity + ")+";
                    Console.WriteLine("Proposing a new pattern: " + patternText);
                    parser.RegisterExperiment(patternText);
                }
                if (best.Where(e => e.Count % 2 == 1).Any()) {  // Odd
                    string patternText = firstPattern.Identity + "(" + secondPattern.Identity + " " + firstPattern.Identity + ")*";
                    Console.WriteLine("Proposing a new pattern: " + patternText);
                    parser.RegisterExperiment(patternText);
                }

                //return true;  // One innovation
            }

            #endregion

            #region Call out charcter-for-character exact duplicates

            var duplicates = new Dictionary<string, List<Hole>>();
            foreach (var hole in chain.Holes) {
                if (hole.Length == 1) continue;  // Skip single-character holes
                if (!duplicates.TryGetValue(hole.Text, out List<Hole> list)) {
                    list = new();
                    duplicates[hole.Text] = list;
                }
                list.Add(hole);
            }
            // Keep only those holes that have more than one duplicate
            foreach (var dup in duplicates.Where(e => e.Value.Count == 1).ToList()) {
                duplicates.Remove(dup.Key);
            }

            if (duplicates.Count > 0) {
                Console.WriteLine("Duplicates:");
                foreach (var dup in duplicates.Values.OrderByDescending(e => e.Count)) {
                    Console.WriteLine(dup.Count + " x '" + dup[0].Text + "'");
                }

                var best = duplicates.Values.OrderByDescending(e => e.Count).First()[0].Text;
                var pattern = parser.NewPatternFromLiterals(best);
                parser.Register(pattern);
            }

            #endregion

            return false;  // No innovations
        }

    }
}
