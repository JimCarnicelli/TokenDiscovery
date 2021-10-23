using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenDiscovery {

    /// <summary>
    /// The Pattern class represents its hierarchic patterns as a tree of PatternPart nodes
    /// </summary>
    /// <remarks>
    /// 
    /// The general structure of a simple string of patterns like "A B C" is a linked list with 
    /// pattern.Head containing the "A" node and pattern.Head.Next referring to the "B" node 
    /// and finally the "C" node attached to pattern.Head.Next.Next. In this case "A", "B", and 
    /// "C" are patterns found in TokenParser.PatternsByName and linked via the PatternPart.Pattern 
    /// properties of each of the 3 nodes.
    /// 
    /// A nested pattern like "A (B C) D" would be similar, but the second node would not have its 
    /// .Pattern property set. Instead its .Alternatives collection property would contain a single 
    /// node for the "B" pattern. And its .Next property would link to yet another node for "C". 
    /// This is a trivial example of nesting that behaves the same as if it were "A B C D". But it 
    /// does mean that the "(B C)" sub-tree can have separate quantifiers. Eg "A (B C)+ D".
    /// 
    /// Taking this one step further, "A (B | C D) E" would have a similar structure to the above. 
    /// However, its second node would take more advantage of the pattern.Head.Next.Alternatives 
    /// collection. Its first element would be for the "B" node and its second would be for the "C" 
    /// node. And as above, the "D" node would be attached to the "C" node's .Next property.
    /// 
    /// To see the inner tree structure of a Pattern object, use pattern.Head.ToDebugString() to 
    /// render a JSON-like mulit-line representation. But pattern.Describe() produces the more 
    /// compact and useful representation.
    /// 
    /// Note that TokenParser.NewPattern() also does some optimization to eliminate some unnecessary 
    /// nesting. For example "A (B C) D" would be reduced to its equivalent "A B C D". However 
    /// "A (B C)+ D" would not be reduced because of the "+" quantifier. One upshot of this reduction 
    /// process is that two patterns can effectively be compared for functional equivalence.
    /// 
    /// </remarks>
    public class PatternPart {

        #region Public properties


        public TokenParser Parser;

        public Pattern Pattern;

        public List<PatternPart> Alternatives;

        public PatternPart Next;

        public int MinQuantity = 1;

        public int MaxQuantity = 1;  // -1 = unlimited

        public bool NonePrior = false;


        #endregion

        public PatternPart(TokenParser parser) {
            Parser = parser;
        }

        #region Render a plain-text representation of a pattern


        public override string ToString() {
            return ToString(true, false, false);
        }

        public string ToString(bool topLevel, bool forceParentheses, bool useIdsForNameless) {
            string text = "";

            if (Pattern != null) {
                text += Pattern.ToString(useIdsForNameless);
                if (Pattern.Name == null && !useIdsForNameless && text[0] != '(') {
                    text += "(" + text + ")";
                }

            } else {  // Alternatives
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += " | ";
                    var alt = Alternatives[i];
                    text += alt.ToString(false, false, useIdsForNameless);
                }

                if (!topLevel && (Alternatives.Count > 1 || Alternatives[0].Next != null || MaxQuantity != 1)) {
                    text = "(" + text + ")";
                }
            }

            if (NonePrior) text = "^" + text;

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

            if (Next != null) {
                text += " " + Next.ToString(topLevel, false, useIdsForNameless);
            }

            if (forceParentheses && text[0] != '(') text = "( " + text + " )";
            return text;
        }

        public string ToDebugString(string indent = "") {
            string text = indent + "{\n";
            if (NonePrior) {
                text += indent + "  NonePrior: true\n";
            }
            if (MinQuantity != 1) {
                text += indent + "  MinQuantity: " + MinQuantity + "\n";
            }
            if (MaxQuantity != 1) {
                text += indent + "  MaxQuantity: " + MaxQuantity + "\n";
            }
            if (Pattern != null) {
                text += indent + "  Pattern: " + Pattern.ToString(true) + "\n";
            }
            if (Alternatives != null) {
                text += indent + "  Alternatives: [\n";
                for (int i = 0; i < Alternatives.Count; i++) {
                    if (i > 0) text += indent + "  ----------\n";
                    var alt = Alternatives[i];
                    text += alt.ToDebugString(indent + "    ");
                }
                text += indent + "  ]\n";
            }
            text += indent + "}\n";
            if (Next != null) {
                text += indent + "->\n";
                text += Next.ToDebugString(indent);
            }
            return text;
        }


        #endregion

        #region Parse plain-text representation of a pattern to construct the hierarchic definition


        public void Clear() {
            Pattern = null;
            Alternatives = null;
            Next = null;
            MinQuantity = 1;
            MaxQuantity = 1;
        }

        public void Import(PatternPart from) {
            Pattern = from.Pattern;
            Alternatives = from.Alternatives;
            Next = from.Next;
            NonePrior = from.NonePrior;
            MinQuantity = from.MinQuantity;
            MaxQuantity = from.MaxQuantity;
        }


        #endregion

    }
}
