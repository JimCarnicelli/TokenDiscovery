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
            sourceText = sourceText.Replace("\r\n", "\n");
            while (sourceText.Contains("\n\n\n")) sourceText = sourceText.Replace("\n\n\n", "\n\n");
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
            }
        }

        public void TrainParagraph(int iteration, int paragraphIndex) {
            string paragraph = Paragraphs[paragraphIndex];
        }

    }
}
