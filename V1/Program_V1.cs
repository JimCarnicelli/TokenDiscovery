using System;

namespace TokenDiscovery_V1 {
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
            Console.WriteLine("Starting");
            var parser = new Parser();

            /*
            {
                var entity = new Entity(parser, EntityType.Experimental);
                entity.Name = "Word";
                entity.Head.Entity = parser.Entity("Letter");
                entity.Head.LookBehind = true;
                entity.Head.Not = true;
                var part = entity.Head.NewNextPart(parser.Entity("Letter"));
                part.MinQuantity = 2;
                part.MaxQuantity = int.MaxValue;
                part = entity.Head.Next.NewNextPart(parser.Entity("Letter"));
                part.Not = true;
                parser.RegisterEntity(entity);
            }
            */

            const int iterations = 3;
            for (int i = 0; i < iterations; i++) {
                parser.ClearSurveyCounts();

                if (i > 0) Console.WriteLine("#########################################################");
                foreach (string rawParagraph in sourceText.Split("\r\n\r\n")) {
                    string paragraphText = rawParagraph.Replace("\r\n", " ");
                    paragraphText = paragraphText.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
                    while (paragraphText.StartsWith(" ")) paragraphText = paragraphText.Substring(1);
                    while (paragraphText.EndsWith(" ")) paragraphText = paragraphText.Substring(0, paragraphText.Length - 1);
                    var matchChain = parser.Parse(paragraphText);
                    parser.SurveyChain(matchChain, i);
                    //break;
                }

                parser.SurveyResults(i);
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
