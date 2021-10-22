using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenDiscovery {
    public class PatternPart {

        public TokenParser Parser;

        public Pattern Pattern;

        public List<PatternPart> Alternatives;

        public PatternPart Next;

        public int MinQuantity = 1;

        public int MaxQuantity = 1;  // -1 = unlimited

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
                text = Pattern.ToString(useIdsForNameless);
                if (Pattern.Name == null && !useIdsForNameless && text[0] != '(') {
                    text = "(" + text + ")";
                }

            } else {  // Alternatives
                foreach (var alt in Alternatives) {
                    if (text != "") text += " | ";
                    text += alt.ToString(false, false, useIdsForNameless);
                }

                if (!topLevel && (Alternatives.Count > 1 || Alternatives[0].Next != null || MaxQuantity != 1)) {
                    text = "(" + text + ")";
                }
            }

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
            MinQuantity = from.MinQuantity;
            MaxQuantity = from.MaxQuantity;
        }

        public void ParsePatternText(string text, int startAt, out int endAt, int parethesesDepth = 0) {
            if (!SkipWhitespace(text, ref startAt)) throw new Exception("Unexpected end of pattern");

            if (text[startAt] == '(') {
                ParsePatternText(text, startAt + 1, out endAt, 1);
                if (endAt >= text.Length) throw new Exception("Unexpected end of pattern");
                startAt = endAt;
                //if (text[startAt] != ')') throw new Exception("Expecting ')' at " + startAt + ": " + text.Substring(startAt));
                //startAt++;
                return;
            }

            var alts = new List<PatternPart>();
            while (true) {

                var part = ParsePatterText_NextAlt(text, ref startAt, parethesesDepth);
                alts.Add(part);

                if (!SkipWhitespace(text, ref startAt)) break;

                if (text[startAt] == '|') {
                    startAt++;
                    if (!SkipWhitespace(text, ref startAt)) break;
                    continue;
                }
                if (text[startAt] == ')') {
                    if (parethesesDepth > 0) break;
                    throw new Exception("Unexpected ')' at " + startAt + ": " + text.Substring(startAt));
                }
                break;
            }

            if (alts.Count == 0) {
                throw new Exception("Expecting one or more alternatives: " + text.Substring(startAt));
            } else if (alts.Count == 1) {
                // Copy the only alternative's settings into my own object
                var part = alts[0];
                Pattern = part.Pattern;
                Alternatives = part.Alternatives;
                Next = part.Next;
            } else {
                Alternatives = alts;
            }

            if (SkipWhitespace(text, ref startAt)) {
                if (text[startAt] == ')') {
                    if (parethesesDepth == 0) {
                        throw new Exception("Unexpected ')' at " + startAt + ": " + text.Substring(startAt));
                    }
                    if (parethesesDepth == 1) startAt++;
                } else {
                    Next = new PatternPart(Parser);
                    Next.ParsePatternText(text, startAt, out endAt, parethesesDepth);
                    return;
                }
            }

            endAt = startAt;
        }

        public PatternPart ParsePatterText_NextAlt(string text, ref int startAt, int parenthesesDepth) {
            if (text[startAt] == '(') {
                startAt++;
                var subPart = new PatternPart(Parser);
                subPart.ParsePatternText(text, startAt, out int endAt, 1);
                startAt = endAt;
                if (startAt >= text.Length) throw new Exception("Unexpected end of pattern");
                if (text[startAt] != ')') throw new Exception("Expecting ')' at " + startAt + ": " + text.Substring(startAt));
                startAt++;
                return subPart;
            }

            var part = new PatternPart(Parser);

            string tokenText = "";
            while (
                startAt < text.Length &&
                text[startAt] != ' ' &&
                text[startAt] != '|' &&
                text[startAt] != ')'
            ) {
                tokenText += text[startAt];
                startAt++;
            }

            if (!Parser.PatternsByName.TryGetValue(tokenText, out Pattern pattern)) throw new Exception("No such pattern named '" + tokenText + "'");
            part.Pattern = pattern;

            if (SkipWhitespace(text, ref startAt)) {
                if (text[startAt] == ')') return part;
                if (text[startAt] == '|') return part;

                part.Next = new PatternPart(Parser);
                part.Next.ParsePatternText(text, startAt, out int endAt, parenthesesDepth);
                startAt = endAt;
            }

            return part;
        }

        private bool SkipWhitespace(string text, ref int startAt) {
            while (true) {
                if (startAt >= text.Length) return false;  // No more text remaining
                char c = text[startAt];
                if (
                    c == ' ' ||
                    c == '\t' ||
                    c == '\r' ||
                    c == '\n'
                ) {
                    startAt++;
                    continue;
                }
                return true;  // More text remaining
            }
        }


        #endregion

    }
}
