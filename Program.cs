using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TokenDiscovery {
    class Program {

        static string sourceText = @"
Eating too much salt can kill you. Excessive salt intake will cause an 
estimated 1.6 million deaths worldwide this year. Four out of five of these 
deaths will occur in low- and middle-income countries, and nearly half will 
be among people younger than 70.

These deaths from excessive salt intake are preventable. In most countries, 
daily salt intake is far above the 5-gram daily upper limit recommended by the 
World Health Organization; the global average, 10.1 grams of salt, is double 
this amount.

Eating less salt can save millions of lives. But reducing dietary salt intake 
has been difficult. Food preferences tend to change slowly, industry has no 
incentive to decrease sodium content of food—and considerable incentives to 
maintain or increase it—and there aren't many alternatives to salt that 
provide the same taste.

A new study published in the New England Journal of Medicine highlights the 
potential of low-sodium salts to reduce sodium consumption and save lives. 
Low-sodium salts replace some of the sodium with potassium, which has similar 
properties. These substitutes contain about a quarter less sodium, but taste 
similar to regular salt and can be used the same way in cooking.

Taming the world's leading killer: high blood pressure

Potassium salts provide a double benefit. Reducing sodium reduces blood 
pressure and saves lives. Increasing potassium, which most people in most 
countries, including the United States, don't consume enough of, further 
reduces blood pressure and improves heart health.

Findings from this groundbreaking study, conducted in China by the George 
Institute for Global Health, show that low-sodium salt substitutes save lives 
and prevent heart attacks and strokes. Low-sodium salt decreased the risk of 
death by 12%, the risk of stroke by 14%, and total cardiovascular events 
(strokes and heart attacks combined) by 13%.
";

        static void Main(string[] args) {
            Console.WriteLine("Starting\n");

            // Pre-parse the raw text into a set of paragraphs with some text cleanup
            var paragraphs = new List<string>();
            foreach (var rawParagraph in sourceText.Split("\r\n\r\n")) {
                string paragraphText = rawParagraph.Replace("\r\n", " ");
                paragraphText = paragraphText.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
                while (paragraphText.StartsWith(" ")) paragraphText = paragraphText.Substring(1);
                while (paragraphText.EndsWith(" ")) paragraphText = paragraphText.Substring(0, paragraphText.Length - 1);
                paragraphs.Add(paragraphText);
            }

            var parser = new TokenParser();
            parser.RegisterBasics();
            //parser.Register("Word", PatternType.Derived, "<Letter! Letter+");
            //parser.Register("Phrase", PatternType.Derived, "(Word Space)+ Word");
            //parser.RegisterExperiment("(Word (Space Letter)) ((Word Space)+)");
            //parser.Register(null, PatternType.Experimental, "(Aa+ Cc)* | (D E)?");
            //parser.Register(null, PatternType.Experimental, "Lowercase+ | Uppercase+");

            /*
            Console.WriteLine(parser.Patterns[126].Describe());
            Console.WriteLine(parser.Patterns[126].Describe(false, true));
            Console.WriteLine(parser.Patterns[126].ToDebugString());
            Console.ReadLine();
            */

            //parser.RegisterExperiment("(Word Space)+");

            string dataPath = @"G:\My Drive\Ventures\MsDev\TokenDiscovery\Data\";
            //parser.SavePatterns(dataPath + "Patterns.txt");
            //parser.LoadPatterns(dataPath + "Patterns.txt");

            /*
            Console.WriteLine("#################### Patterns ####################");
            foreach (var pattern in parser.Patterns.Values) {
                //if (pattern.Type < PatternType.Derived) continue;
                Console.WriteLine("- " + pattern.Identity + ": " + pattern.Describe());
                //Console.WriteLine("- " + pattern.Identity + ": " + pattern.Describe(false, true));
            }
            Console.WriteLine();
            Console.ReadLine();
            */

            const int iterations = 6;

            for (int i = 1; i <= iterations; i++) {

                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ Iteration " + i + " @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\n");

                parser.ClearSurvey();

                // Process each of the paragraphs
                foreach (var paragraph in paragraphs) {
                    //Console.WriteLine(paragraph + "\n");

                    var chain = parser.Parse(paragraph);
                    // Console.WriteLine(chain.ToDebugString(PatternType.Derived, false)); Console.WriteLine();

                    parser.Survey(chain, i);
                }

                File.WriteAllText(dataPath + "TokenChain.txt",
                    paragraphs[3] + "\n\n" +
                    parser.Parse(paragraphs[0]).ToDebugString(PatternType.Derived, false)
                );

                /*
                Console.WriteLine("#################### Survey results ####################");
                parser.SurveyResults();
                Console.WriteLine();
                */

                if (i < iterations) {
                    //Console.WriteLine("#################### Propose patterns ####################");
                    parser.ProposePatterns();
                    //Console.WriteLine();
                }

                // Name certain expected patterns
                var patterns = parser.Patterns.Values
                    .Where(e => e.Name == null)
                    .ToList();
                foreach (var pattern in patterns) {
                    if (pattern.Name != null) continue;
                    var description = pattern.Describe();

                    switch (description) {
                        case "Letter+":
                            pattern.Name = "Word";
                            parser.Register(pattern);
                            break;
                        case "(Word Space)+ Word":
                            pattern.Name = "Phrase";
                            parser.Register(pattern);
                            break;
                    }
                }

                parser.CullExperiments();

                Console.WriteLine("#################### Patterns ####################");
                var sortedPatterns = parser.Patterns.Values
                    .OrderByDescending(e => e.SurveyStretch - e.Penalty);
                foreach (var pattern in sortedPatterns) {
                    if (pattern.Type < PatternType.Experimental) continue;
                    Console.WriteLine(
                        pattern.Identity + " - " +
                        (pattern.SurveyStretch - pattern.Penalty) + " = (" + pattern.SurveyStretch + "-" + pattern.Penalty + ") - " +
                        pattern.Describe()
                    );
                    if (pattern.SurveyExamples != null) {
                        var sortedExamples = pattern.SurveyExamples
                            .OrderByDescending(e => e.Length)
                            .Take(3)
                            .ToList();
                        foreach (string example in sortedExamples) {
                            Console.WriteLine("  | " + example);
                        }
                    }
                }
                Console.WriteLine();
            }

            File.WriteAllText(dataPath + "TokenChain.txt",
                paragraphs[3] + "\n\n" +
                parser.Parse(paragraphs[0]).ToDebugString(PatternType.Derived, false)
            );

            parser.SavePatterns(dataPath + "Patterns.txt");

            //Console.Write(parser.Patterns[142].ToDebugString());
            //Console.WriteLine(parser.Patterns[142].Describe());

            parser.ClearAllSurveys();

            Console.WriteLine("Done");
            Console.ReadLine();
        }

    }
}
