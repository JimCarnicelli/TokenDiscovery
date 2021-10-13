﻿using System;

namespace TokenDiscovery {
    class Program {

        static string sourceText = @"
(CNN)Eating too much salt can kill you. Excessive salt intake will cause an 
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
            var parser = new Parser();

            {
                var entity = parser.NewRootEntity();
                entity.Name = "The";
                entity.Head.Entity = parser.Entity("'T' letter");
                entity.Head.NewNextEntity(parser.Entity("'H' letter"));
                entity.Head.Next.NewNextEntity(parser.Entity("'E' letter"));
                entity.Head.Next.Next.MinQuantity = 1;
                entity.Head.Next.Next.MaxQuantity = 3;
            }

            foreach (var entity in parser.Entities) {
                //Console.WriteLine(entity);  // + " -> " + entity.Describe());
            }

            foreach (string rawParagraph in sourceText.Split("\r\n\r\n")) {
                string paragraphText = rawParagraph.Replace("\r\n", " ");
                parser.Parse(paragraphText);
                break;
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
