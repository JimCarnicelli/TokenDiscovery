using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenDiscovery_V1 {
    
    /// <summary>
    /// The top level that learns how to parse and does it
    /// </summary>
    public class Parser {

        public Dictionary<long, Entity> Entities = new();
        public Dictionary<string, Entity> EntitiesByName = new();
        public Dictionary<string, Entity> EntitiesByDescription = new();

        public Parser() {
            var allChars = new Entity(this, EntityType.Trivial);
            allChars.Name = "All chars";

            // Generate an entity for each of the supported visible ASCII characters
            for (int i = 32; i < 127; i++) {
                var entity = new Entity(this, EntityType.Baseline, "" + ((char)i));
                switch (entity.Literal) {
                    case " ":
                        entity.Name = "Space";
                        break;
                    default:
                        entity.Name = entity.Literal;
                        break;
                }
                RegisterEntity(entity);

                allChars.Head.AddAlternative(entity);
            }

            var letters = new Entity(this, EntityType.Baseline);
            letters.Name = "Letter";

            var upperCaseLetters = new Entity(this, EntityType.Baseline);
            upperCaseLetters.Name = "Uppercase";
            letters.Head.AddAlternative(upperCaseLetters);

            var lowerCaseLetters = new Entity(this, EntityType.Baseline);
            lowerCaseLetters.Name = "Lowercase";
            letters.Head.AddAlternative(lowerCaseLetters);

            // Generate an entity for all letters with both upper and lower case versions of each
            for (char c = 'A'; c <= 'Z'; c++) {
                string upperC = c.ToString();
                string lowerC = upperC.ToLower();
                var entity = new Entity(this, EntityType.Baseline);
                entity.Name = c + lowerC;
                entity.Head.AddAlternative(Entity(upperC));
                entity.Head.AddAlternative(Entity(lowerC));
                RegisterEntity(entity);

                upperCaseLetters.Head.AddAlternative(Entity(upperC));
                lowerCaseLetters.Head.AddAlternative(Entity(lowerC));
            }

            RegisterEntity(upperCaseLetters);
            RegisterEntity(lowerCaseLetters);
            RegisterEntity(letters);
            RegisterEntity(allChars);
        }

        public void RegisterEntity(Entity entity) {
            Entities[entity.Id] = entity;
            if (entity.Name != null) {
                if (EntitiesByName.ContainsKey(entity.Name)) {
                    throw new Exception("The '" + entity.Name + "' entity already exists");
                }
                EntitiesByName[entity.Name] = entity;
            }
            string description = entity.Describe();
            if (EntitiesByDescription.ContainsKey(description)) {
                throw new Exception("An entity already exists with this description: " + description);
            }
            EntitiesByDescription[description] = entity;
        }

        public Entity Entity(string name) {
            return EntitiesByName[name];
        }

        public void Remove(Entity entity) {
            Entities.Remove(entity.Id);
            var byName = EntitiesByName.Where(m => m.Value == entity).FirstOrDefault();
            if (byName.Key != null) EntitiesByName.Remove(byName.Key);
            var description = entity.Describe();
            var byDescription = EntitiesByDescription.Where(m => m.Value == entity).FirstOrDefault();
            if (byDescription.Key != null) EntitiesByDescription.Remove(byDescription.Key);
        }

        public void RemoveAllExperimental() {
            var experimentals = Entities.Where(m => m.Value.Type == EntityType.Experimental).Select(m => m.Value).ToList();
            foreach (var entity in experimentals) {
                Remove(entity);
            }
        }

        public void ClearSurveyCounts() {
            foreach (var entity in Entities.Values) {
                entity.SurveyCounts = 0;
                entity.SurveyLength = 0;
                entity.SurveyLongest = 0;
                entity.SurveyCoverage.Clear();
            }
            CandidatePairs.Clear();
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
                foreach (var entity in Entities.Values) {
                    var match = entity.Match(text, i, matchChain);
                    if (match != null && match.Length > 0) {
                        matchChain.Add(match);
                    }
                }

                /*
                foreach (var match in matchChain[i].Values) {
                    Console.WriteLine(new string(' ', i) + match.Entity + " >> " + text.Substring(match.StartAt, match.Length));
                }
                */

            }
            return matchChain;
        }

        public Dictionary<string, int> CandidatePairs = new();
        public Dictionary<string, bool> CandidatePairsAlreadyUsed = new();

        public void SurveyChain(EntityMatchChain matchChain, int iteration) {

            if (iteration == 2) {
                //Console.WriteLine(matchChain.Describe() + "-----------------------");
            }

            for (int i = 0; i < matchChain.Length; i++) {
                if (matchChain.Starts[i] == null) continue;
                var matches = matchChain.Starts[i].Values;
                foreach (var match in matches) {
                    if (match.Entity.Type == EntityType.Trivial) continue;

                    // Keep track of how many times we've seen this entity
                    match.Entity.SurveyCounts++;

                    if (match.Entity.ToString() == "( ( Word + Space )!< + ( Word + Space )+ + Word )") {
                        var x = matchChain.Text.Substring(match.StartAt, match.Length);
                        Console.WriteLine(">>>> " + x);
                    }

                    for (int j = match.StartAt; j < match.StartAt + match.Length; j++) {
                        match.Entity.SurveyCoverage[j] = true;
                    }
                    //match.Entity.SurveyLength += match.Length;
                    if (match.Length > match.Entity.SurveyLongest) match.Entity.SurveyLongest = match.Length;

                    // Look for candidate pairs of entities
                    int nextStartAt = match.StartAt + match.Length;
                    if (nextStartAt < matchChain.Length) {
                        var nextMatches = matchChain.Starts[nextStartAt].Values;

                        // Look for candidate pairs of two different entities.
                        if (!nextMatches.Where(e => e.Entity.Id == match.Entity.Id).Any()) {
                            foreach (var nextMatch in nextMatches) {
                                if (nextMatch.Entity.Type == EntityType.Trivial) continue;
                                // Don't consider any pairs where one is a subset of the other
                                if (matches.Where(e => e.Entity.Id == nextMatch.Entity.Id).Any()) continue;

                                string pairKey = "2dif|" + match.Entity.Id + "|" + nextMatch.Entity.Id;
                                if (CandidatePairsAlreadyUsed.ContainsKey(pairKey)) continue;

                                //Console.WriteLine(match.Entity + " + " + nextMatch.Entity);
                                CandidatePairs.TryGetValue(pairKey, out int count);
                                CandidatePairs[pairKey] = count + 1;
                                //CandidatePairs[pairKey] = count + match.Length + nextMatch.Length;
                                //if (nextMatch.Length > count) CandidatePairs[pairKey] = match.Length + nextMatch.Length;
                            }
                        }

                        // Look for candidate pairs of two or more of the same entity.
                        if (nextMatches.Where(e => e.Entity.Id == match.Entity.Id).Any()) {
                            string pairKey = "2sam|" + match.Entity.Id;
                            if (CandidatePairsAlreadyUsed.ContainsKey(pairKey)) continue;

                            CandidatePairs.TryGetValue(pairKey, out int count);
                            CandidatePairs[pairKey] = count + 1;
                        }

                    }

                }
            }

            foreach (var entity in Entities.Values.Where(m => m.SurveyCounts > 0)) {

                /*
                if (entity.ToString() == "( Space!< + ( ( Letter!< + Letter+ ) + Space )+ + ( Letter!< + Letter+ ) )") {
                    var x = 1;
                }
                */

                entity.SurveyLength += entity.SurveyCoverage.Count;
                entity.SurveyCoverage.Clear();
            }

        }

        public void SurveyResults(int iteration) {

            foreach (var entity in Entities.Values.Where(m => m.SurveyCounts > 0)) {
                entity.SurveyScore = entity.SurveyLength;
            }

            var sortedEntities = Entities
                .Where(m => m.Value.SurveyCounts > 2)
                .Where(m => m.Value.Type == EntityType.Derived || m.Value.Type == EntityType.Experimental)
                .OrderByDescending(m => m.Value.SurveyScore)
                .Take(20);
            foreach (var keyValue in sortedEntities) {
                var entity = keyValue.Value;
                Console.WriteLine(keyValue.Value + " > " + keyValue.Value.SurveyScore);
                var examples = entity.SurveyExamples
                    //.OrderByDescending(e => e.Value)
                    .OrderByDescending(e => e.Key.Length)
                    .Take(5);
                foreach (var example in examples) {
                    Console.WriteLine("   " + example.Key.Replace(" ", "_") + " > " + example.Value);
                }
            }

            //Console.WriteLine("---");

            // Look for one-next-to-the-other pairs of entities that have potential to form new entities
            var sortedPairs = CandidatePairs
                .Where(e => e.Value > 2)
                .OrderByDescending(e => e.Value)
                .Take(20);
            foreach (var pairKeyCount in sortedPairs) {
                string keyPair = pairKeyCount.Key;
                CandidatePairsAlreadyUsed[keyPair] = true;
                var parts = keyPair.Split('|');

                if (parts[0] == "2dif") {
                    Entity entity1 = Entities[long.Parse(parts[1])];
                    Entity entity2 = Entities[long.Parse(parts[2])];
                    //Console.WriteLine("2 different : " + entity1 + " + " + entity2 + " > " + pairKeyCount.Value);

                    {
                        var newEntity = new Entity(this, EntityType.Experimental);
                        newEntity.Head.Entity = entity1;
                        newEntity.Head.NewNextPart(entity2);
                        if (!EntitiesByDescription.ContainsKey(newEntity.Describe())) {
                            RegisterEntity(newEntity);
                        }
                    }

                    {
                        var newEntity = new Entity(this, EntityType.Experimental);

                        // (E1 + E2)! : Not preceded by main pattern
                        var lookBackPart = new EntityPart(this);
                        lookBackPart.Entity = entity1;  // E1
                        lookBackPart.NewNextPart(entity2);  // E2
                        newEntity.Head.AddAlternative(lookBackPart);
                        newEntity.Head.LookBehind = true;
                        newEntity.Head.Not = true;

                        // (E1 + E2)+ : One or more of main element plus separator
                        var firstPart = newEntity.Head.NewNextPart();
                        firstPart.MinQuantity = 1;
                        firstPart.MaxQuantity = int.MaxValue;

                        var mainPart = new EntityPart(this);
                        mainPart.Entity = entity1;  // E1
                        mainPart.NewNextPart(entity2);  // E2
                        firstPart.AddAlternative(mainPart);

                        // E1 : Ending with main element
                        var lastPart = newEntity.Head.Next.NewNextPart(entity1);

                        if (!EntitiesByDescription.ContainsKey(newEntity.Describe())) {
                            RegisterEntity(newEntity);
                        }

                        if (newEntity.ToString() == "( ( Word + Space )!< + ( Word + Space )+ + Word )") {
                            //Console.WriteLine("xxxxxxxxxx");
                        }

                    }

                } else if (parts[0] == "2sam") {
                    Entity entity1 = Entities[long.Parse(parts[1])];
                    //Console.WriteLine("2 same : " + entity1 + " + " + entity1 + " > " + pairKeyCount.Value);

                    var newEntity = new Entity(this, EntityType.Experimental);
                    newEntity.Head.Entity = entity1;
                    newEntity.Head.LookBehind = true;
                    newEntity.Head.Not = true;
                    var part = newEntity.Head.NewNextPart(entity1);
                    part.MinQuantity = 1;
                    part.MaxQuantity = int.MaxValue;

                    if (newEntity.ToString() == "( Letter!< + Letter+ )") newEntity.Name = "Word";

                    if (!EntitiesByDescription.ContainsKey(newEntity.Describe())) {
                        RegisterEntity(newEntity);
                    }

                }
            }
        }

    }

}
