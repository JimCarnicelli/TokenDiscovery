using System.Collections.Generic;

namespace TokenDiscovery {

    public enum Look {
        Here,
        Behind,
        Ahead,
    }

    /// <summary>
    /// The Pattern class represents its hierarchic patterns as a tree of PatternElement nodes
    /// </summary>
    /// <remarks>
    /// 
    /// The general structure of a simple sequence of patterns like "A B C" is a list in the 
    /// .Alternatives property with one sub-list containing a single sequence of 3 elements. Each 
    /// having its .Pattern property set to point to one named property (eg the "A" pattern).
    /// 
    /// A nested pattern like "A (B C) D" would be similar, but the second element would not have its 
    /// .Pattern property set. Instead its .Alternatives collection property would contain a single 
    /// sequence for "B C". This is a trivial example of nesting that behaves the same as if 
    /// it were "A B C D", thanks to the associative property of these patterns. But it does mean 
    /// that the "(B C)" sub-tree can have separate quantifiers. Eg "A (B C)+ D", which would 
    /// violate associativity.
    /// 
    /// Taking this one step further, "A (B | C D) E" would have a similar structure to the above. 
    /// However, its second node would take more advantage of the pattern.Root.Alternatives 
    /// collection. Its first sub-list would contain the "B" sequence and its second would contain 
    /// the "C D" sequence.
    /// 
    /// Note that TokenParser.NewPattern() also does some optimization to eliminate some unnecessary 
    /// nesting. For example "A (B C) D" would be reduced to its equivalent "A B C D". However 
    /// "A (B C)+ D" would not be reduced because of the "+" quantifier. One upshot of this reduction 
    /// process is that two patterns can effectively be compared for functional equivalence.
    /// 
    /// </remarks>
    public class PatternElement {

        #region Public properties


        public TokenParser Parser;

        public Pattern Pattern;

        /// <summary>
        /// A list of alternative sequences of child elements
        /// </summary>
        public List<List<PatternElement>> Alternatives;

        public int MinQuantity = 1;

        public int MaxQuantity = 1;  // -1 = unlimited

        public Look Look = Look.Here;


        #endregion

        public PatternElement(TokenParser parser) {
            Parser = parser;
        }

        #region Render a plain-text representation of a pattern


        public override string ToString() {
            return ToString(false, false, true);
        }

        public string ToString(bool useIdsForNameless, bool fullDepth, bool topLevel) {
            string text = "";

            if (Pattern != null) {
                text += Pattern.ToString(useIdsForNameless, fullDepth, false);
                if (!topLevel && (text.Contains(' ') || text.Contains('|'))) {
                    text = "(" + text + ")";
                }

            } else {  // Alternatives
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += " | ";
                    var alt = Alternatives[i];
                    for (int j = 0; j < alt.Count; j++) {
                        var subText = alt[j].ToString(useIdsForNameless, fullDepth, false);
                        if (j > 0) text += " ";
                        text += subText;
                    }
                }

                if (
                    (
                        Alternatives.Count > 1 &&
                        !topLevel
                    ) || (
                        (
                            Alternatives[0].Count > 1
                        ) && (
                            MinQuantity != 1 ||
                            MaxQuantity != 1 ||
                            Look != Look.Here
                        )
                    )
                ) {
                    text = "(" + text + ")";
                }

            }

            if (Look == Look.Behind) text = "<" + text;
            if (Look == Look.Ahead) text = ">" + text;

            if (MinQuantity == 0 && MaxQuantity == 0) {
                text += "!";
            } else if (MinQuantity == 0 && MaxQuantity == 1) {
                text += "?";
            } else if (MinQuantity == 0 && MaxQuantity == -1) {
                text += "*";
            } else if (MinQuantity == 1 && MaxQuantity == -1) {
                text += "+";
            } else if (MinQuantity != 1 && MaxQuantity == -1) {
                text += "{" + MinQuantity + "+}";
            } else if (MinQuantity != 1 && MaxQuantity == MinQuantity) {
                text += "{" + MinQuantity + "}";
            } else if (MinQuantity != 1 || MaxQuantity != 1) {
                text += "{" + MinQuantity + "-" + MaxQuantity + "}";
            }

            return text;
        }

        public string ToDebugString(string indent) {
            string text = "{\n";

            if (MinQuantity != 1) text += indent + "  MinQuantity: " + MinQuantity + "\n";
            if (MaxQuantity != 1) text += indent + "  MaxQuantity: " + (MaxQuantity == -1 ? "Unlimited" : MaxQuantity) + "\n";
            if (Look != Look.Here) text += indent + "  Look: " + Look + "\n";

            if (Pattern != null) {
                text += indent + "  Pattern: " + Pattern.Identity;
                if (Pattern.Name == null) {
                    text += " ( " + Pattern.Describe() + " )";
                }
                text += "\n";

            } else {
                text += indent + "  Alternatives: [\n";
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += "  ----------\n";
                    foreach (var subPattern in Alternatives[i]) {
                        text += indent + "    " + subPattern.ToDebugString(indent + "    ");
                    }
                }
                text += indent + "  ]\n";
            }

            text += indent + "}\n";
            return text;
        }

        public bool DependsOn(Pattern otherPattern) {
            if (Pattern == otherPattern) return true;
            foreach (var sequence in Alternatives) {
                foreach (var elem in sequence) {
                    if (elem.Pattern == otherPattern) return true;
                }
            }
            return false;
        }

        public int CalculatePenalty() {
            int penalty = 1;
            if (Look != Look.Here) penalty += 2;
            if (Pattern != null) {
                penalty += Pattern.Penalty;
            } else {
                //if (Alternatives.Count > 1) penalty++;
                int maxPenalty = 0;
                foreach (var sequence in Alternatives) {
                    int subPenalty = 0;
                    foreach (var elem in sequence) {
                        subPenalty += elem.CalculatePenalty();
                    }
                    if (subPenalty > maxPenalty) maxPenalty = subPenalty;
                }
                penalty += maxPenalty;
            }
            return penalty;
        }


        #endregion

        #region Parse plain-text representation of a pattern to construct the hierarchic definition


        public void Clear() {
            Pattern = null;
            Alternatives = null;
            MinQuantity = 1;
            MaxQuantity = 1;
        }

        public void Import(PatternElement from) {
            Pattern = from.Pattern;
            Alternatives = from.Alternatives;
            Look = from.Look;
            MinQuantity = from.MinQuantity;
            MaxQuantity = from.MaxQuantity;
        }


        #endregion

        #region Parsing text


        public bool Match(TokenChain chain, int startAt, out int endAt) {
            int origStartAt = startAt;
            endAt = startAt;

            if (Look == Look.Behind) {

                if (Pattern != null) {
                    int seqEndAt = startAt - 1;
                    if (seqEndAt < 0) {
                        if (MaxQuantity == 0) return true;  // We didn't want to find this
                        return false;  // We did want to find this
                    }
                    if (chain.Tails[seqEndAt].TryGetValue(Pattern.Id, out Token _)) {
                        if (MaxQuantity == 0) return false;  // We didn't want to find this
                        return true;  // We did want to find this
                    }
                    if (MaxQuantity == 0) return true;  // We didn't want to find this
                    return false;  // We did want to find this

                } else {
                    foreach (var sequence in Alternatives) {
                        int seqEndAt = startAt - 1;
                        if (seqEndAt < 0) {
                            if (MaxQuantity == 0) return true;  // We didn't want to find this
                            return false;  // We did want to find this
                        }
                        bool allMatched = true;
                        sequence.Reverse();  // We must reverse the sequence order because we're looking backward
                        foreach (var elem in sequence) {
                            if (seqEndAt < 0) {
                                allMatched = false;
                                break;  // Failed to match this alternative sequence
                            }
                            if (!chain.Tails[seqEndAt].TryGetValue(elem.Pattern.Id, out Token token)) {
                                allMatched = false;
                                break;  // Failed to match this alternative sequence
                            }
                            seqEndAt = token.StartAt - 1;
                        }
                        if (allMatched) {
                            if (MaxQuantity == 0) return false;  // We didn't want to find this
                            return true;  // We did want to find this
                        }
                    }
                    if (MaxQuantity == 0) return true;  // We didn't want to find this
                    return false;  // We did want to find this

                }
            }

            int matchCount = 0;

            while (
                MaxQuantity == -1 ||
                (MaxQuantity == 0 && matchCount < 1) ||
                matchCount < MaxQuantity
            ) {
                int innerEndAt;

                if (Pattern != null) {
                    var token = Pattern.Match(chain, startAt);
                    if (token == null) break;
                    endAt = startAt + token.Length;
                    startAt = endAt;
                    matchCount++;

                } else {

                    bool allMatched = true;
                    foreach (var sequence in Alternatives) {
                        allMatched = true;
                        int seqStartAt = startAt;
                        foreach (var elem in sequence) {
                            if (!elem.Match(chain, seqStartAt, out innerEndAt)) {
                                allMatched = false;
                                break;  // Failed to match this alternative sequence
                            }
                            seqStartAt = innerEndAt;
                        }
                        if (allMatched) {
                            matchCount++;
                            startAt = seqStartAt;
                            endAt = startAt;
                            break;
                        }
                    }
                    if (!allMatched) break;

                }

            }

            if (MaxQuantity == 0 && matchCount > 0) return false;
            if (matchCount >= MinQuantity) {
                if (Look == Look.Ahead) endAt = origStartAt;
                return true;
            }
            return false;
        }


        #endregion

    }
}
