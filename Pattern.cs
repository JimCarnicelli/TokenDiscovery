using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TokenDiscovery {

    public enum PatternType {
        Literal,
        Basics,
        Derived,
        Experimental,
    }

    /// <summary>
    /// One defined pattern that behaves similarly to a regular expression for matching text
    /// </summary>
    /// <remarks>
    /// 
    /// See .Describe() and Parser.NewPattern() for more about the regex-like expression
    /// language for representing patterns.
    /// 
    /// </remarks>
    public class Pattern {

        #region Public properties


        public Parser Parser;

        public int Id = -1;

        public string Name;

        public PatternType Type;

        public string Literal;

        public PatternElement Root;

        public int Penalty {
            get {
                // TODO: Reconsider penalties later
                return 0;

                // Already calculated and cached?
                if (penalty <= 0) {  // Not yet
                    if (Type == PatternType.Literal) {
                        penalty = 1;
                    } else {
                        penalty = Root.CalculatePenalty();
                    }
                }
                return penalty;
            }
        }
        private int penalty = 0;


        #endregion

        public Pattern(Parser parser, PatternType patternType = PatternType.Experimental) {
            Parser = parser;
            Type = patternType;
        }

        public override string ToString() {
            return ToString(false, false, true);
        }

        public string ToString(bool useIdIfNameless, bool fullDepth, bool topLevel) {
            if (!fullDepth) {
                if (Name != null || useIdIfNameless) {
                    return Identity;
                }
            }
            return Describe(false, fullDepth, topLevel);
        }

        public string ToDebugString(string indent = "") {
            if (Literal != null) return "Literal: '" + Literal.Replace("'", "''") + "'";
            return Identity + " " + Root.ToDebugString(indent);
        }

        private static Regex regexSafeName = new Regex("^[A-Za-z][-_A-Za-z0-9]*$");

        public string Identity {
            get {
                if (Name != null) {
                    if (regexSafeName.IsMatch(Name)) return Name;
                    return "'" + Name.Replace("'", "''") + "'";
                } else if (Id >= 0) {
                    return "[" + Id + "]";
                }
                throw new Exception("This pattern has no identity");
            }
        }

        public static string Unescape(string text) {
            if (text[0] == '\'') {
                return text.Substring(1, text.Length - 2).Replace("''", "'");
            }
            return text;
        }

        public string Describe(bool useIds = false, bool fullDepth = false, bool topLevel = true) {
            string text;
            if (!useIds && fullDepth) {  // We'll cache these
                if (fullDepthDescription != null) return fullDepthDescription;

                if (Literal != null) {
                    text = Identity;
                } else {
                    text = Root.ToString(useIds, fullDepth, topLevel);
                }
                fullDepthDescription = text;
            } else if (Literal != null) {
                text = "'" + Literal.Replace("'", "''") + "'";
            } else {
                text = Root.ToString(useIds, fullDepth, topLevel);
            }
            return text;
        }

        private string fullDepthDescription;

        /// <summary>
        /// Determine if I directly rely on the existence of the given pattern
        /// </summary>
        public bool DependsOn(Pattern otherPattern) {
            if (this == otherPattern) return false;
            if (Literal != null) return false;
            return Root.DependsOn(otherPattern);
        }

        public Token Match(TokenChain chain, Token parentToken, int startAt) {
            Token token;

            if (startAt < 0) return null;
            if (startAt >= chain.Length) return null;

            // If it's already been found previously then don't bother trying again
            if (chain.Heads[startAt].TryGetValue(Id, out token)) {
                if (parentToken != null) parentToken.Children.Add(token);
                return token;
            }

            // Literals are simple string matches
            if (Literal != null) {
                var snippet = chain.Text.Substring(startAt, Literal.Length);
                if (snippet != Literal) return null;
                token = new Token();
                token.Text = snippet;
                token.StartAt = startAt;
                token.Length = snippet.Length;

            } else {
                token = new Token();
                if (!Root.Match(chain, token, startAt, out int endAt)) return null;
                token.StartAt = startAt;
                token.Length = endAt - startAt;
                token.Text = chain.Text.Substring(startAt, token.Length);

            }

            token.Pattern = this;
            chain.Heads[startAt][token.Pattern.Id] = token;
            if (startAt + token.Length - 1 >= 0) {
                chain.Tails[startAt + token.Length - 1][token.Pattern.Id] = token;
            }

            if (parentToken != null) parentToken.Children.Add(token);
            return token;
        }

    }
}
