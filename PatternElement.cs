﻿using System;
using System.Collections.Generic;
using System.Linq;

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
                    for (int j = 0; j < alt.Count; j++) {
                        if (j > 0) text += " ";
                        text += alt[j].ToString(false, false, useIdsForNameless);
                    }
                }

                if (!topLevel && (Alternatives.Count > 1 || Alternatives[0].Count > 1 || MaxQuantity != 1)) {
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

            if (forceParentheses && text[0] != '(') text = "( " + text + " )";
            return text;
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

    }
}