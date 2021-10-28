using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace TokenDiscovery {
    class Program {

        // https://www.cnn.com/2021/10/10/health/frieden-salt-sodium/index.html
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

            string dataPath = DataDirectory();

            var trainer = new Trainer();
            trainer.Initialize();
            trainer.ImportSourceText(sourceText);

            trainer.Iterations = 2;
            trainer.Train();

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

            var paragraph = trainer.Paragraphs[0];
            File.WriteAllText(dataPath + "TokenChain.txt",
                paragraph + "\n\n" +
                trainer.parser.Parse(paragraph).ToDebugString(PatternType.Derived, false)
            );

            trainer.parser.SavePatterns(dataPath + "Patterns.txt");

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        /// <summary>
        /// Get the path to the data directory based on whether we are in debug or release mode
        /// </summary>
        private static string DataDirectory([CallerFilePath] string path = null) {
#if DEBUG
            // We were given as input the path to this code file at compile time
            path = Path.GetDirectoryName(path);
#else
            // We will rely on the executable's directory
            path = Environment.CurrentDirectory;
#endif
            if (path.Contains("\\")) {
                path += "\\Data\\";
            } else {
                path += "//Data//";
            }
            return path;
        }

    }
}
