using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenDiscovery {
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
