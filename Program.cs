using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TokenDiscovery {
    class Program {

        static void Main(string[] args) {
            Console.WriteLine("Starting\n");

            string dataPath = DataDirectory();

            var trainer = new Trainer();
            trainer.Initialize();

            var sourceText = File.ReadAllText(dataPath + "SourceText.txt");
            sourceText = Regex.Replace(sourceText, "# .*", "");  // Strip out comment lines

            trainer.ImportSourceText(sourceText);

            trainer.parser.RegisterExperiment("BasicWord",          "<Letter! Letter+");
            trainer.parser.RegisterExperiment("Number",             "<(Digit | '.')! '-'? Digit+ ('.' Digit+)?");
            trainer.parser.RegisterExperiment("Percent",            "Number '%'");
            trainer.parser.RegisterExperiment("ApostrophedWord",    "BasicWord Apostrophe BasicWord");
            trainer.parser.RegisterExperiment("Word",               "ApostrophedWord | BasicWord | Number | Percent");
            trainer.parser.RegisterExperiment("Dash",               "Space '-'{2} Space | Space '-' Space | <'-'! '-'{2} | <'-'! '-'");
            trainer.parser.RegisterExperiment("WordSeparator",      "',' Space | ';' Space | ':' Space | Dash | Space");
            trainer.parser.RegisterExperiment("Phrase",             "<WordSeparator! (Word WordSeparator)* Word");
            trainer.parser.RegisterExperiment("Sentence",           "Phrase '.' Space?");
            trainer.parser.RegisterExperiment("Paragraph",          "<Sentence! Sentence+");

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

            var paragraph = trainer.Paragraphs[3];
            var chain = trainer.parser.Parse(paragraph);
            File.WriteAllText(dataPath + "TokenChain.txt",
                paragraph + "\n\n" +
                chain.ToDebugString(PatternType.Derived, false)
            );
            if (chain.Tops.Count > 0) {
                File.WriteAllText(dataPath + "TokenTree.json",
                    "{\n" +
                    "  \"SourceText\": " + JsonSerialize(paragraph) + ",\n" +
                    "  \"Root\":\n\n" +
                    JsonSerialize(chain.Tops[0]) +
                    "\n\n}\n"
                );
            }

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

        private static string JsonSerialize(object obj) {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
        }

    }
}
