using System;
using System.Collections.Generic;

namespace TokenDiscovery {
    
    /// <summary>
    /// The top level that learns how to parse and does it
    /// </summary>
    public class Parser {

        public List<Entity> Entities = new();

        public Entity NewRootEntity(string literal = null) {
            var entity = new Entity(this, literal);
            Entities.Add(entity);
            return entity;
        }

        public Entity Entity(string name) {
            foreach (var entity in Entities) {
                if (entity.Name == name) return entity;
            }
            throw new KeyNotFoundException(name);
        }

        public Parser() {
            var allChars = NewRootEntity();
            allChars.Name = "All chars";

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                var entity = NewRootEntity("" + ((char)i));
                switch (entity.Literal) {
                    case " ":
                        entity.Name = "Space";
                        break;
                    default:
                        entity.Name = entity.Literal;
                        break;
                }

                allChars.Head.AddAlternative(entity);
            }

            var letters = NewRootEntity();
            letters.Name = "Letter";

            var upperCaseLetters = NewRootEntity();
            upperCaseLetters.Name = "Uppercase";
            letters.Head.AddAlternative(upperCaseLetters);

            var lowerCaseLetters = NewRootEntity();
            lowerCaseLetters.Name = "Lowercase";
            letters.Head.AddAlternative(lowerCaseLetters);

            // Generate an entity for all letters with both upper and lower case versions of each
            for (char c = 'A'; c <= 'Z'; c++) {
                string upperC = c.ToString();
                string lowerC = upperC.ToLower();
                var entity = NewRootEntity();
                entity.Name = c + lowerC;
                entity.Head.AddAlternative(Entity(upperC));
                entity.Head.AddAlternative(Entity(lowerC));

                upperCaseLetters.Head.AddAlternative(Entity(upperC));
                lowerCaseLetters.Head.AddAlternative(Entity(lowerC));
            }

        }

        public EntityMatchChain Parse(string text) {

            // We'll create an array with the same number of elements as the characters 
            // in the text. Each element will represent a character position at which we 
            // had a reason to start parsing for a single element. As soon as we find one 
            // match we will note its length. At the character position just past the end 
            // of that match we will note that we need to eventually start parsing there 
            // eventually as we step forward in the process. Each of the valid matches 
            // found at each starting character will be added to a list in that array 
            // element. The end result is a very compact representation of a branching tree 
            // that would balloon quickly into a gargantuan number of nodes. If you start 
            // from the first array element and look at each match stored there, you can 
            // hopscotch your way to each of their next positions and see what next elements 
            // exist there as well. The compactness of this structure stems from the fact 
            // that each match found at this stage is independent of the one immediately 
            // before and after it. That massive redundancy collapses down to this neat 
            // structure.

            var matchChain = new EntityMatchChain(text);

            for (int i = 0; i < text.Length; i++) {
                // If we have not found any earlier matches whose next characters would be
                // here then we never will. Move on past this one.
                if (matchChain[i] == null) continue;

                // Test all entities for possible matches starting at this character position
                foreach (var entity in Entities) {
                    var match = entity.Match(text, i, matchChain);
                    if (match != null && match.Length > 0) {
                        matchChain.Add(match);
                    }
                }

                foreach (var match in matchChain[i].Values) {
                    Console.WriteLine(new string(' ', i) + match.Entity + " >> " + text.Substring(match.StartAt, match.Length));
                }

            }
            return matchChain;
        }

    }

}
