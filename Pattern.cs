using System;
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
    /// See .Describe() and TokenParser.NewPattern() for more about the regex-like expression
    /// language for representing patterns.
    /// 
    /// </remarks>
    public class Pattern {

        #region Public properties


        public TokenParser Parser;

        public int Id = -1;

        public string Name;

        public PatternType Type;

        public string Tag;

        public string Literal;

        public PatternElement Root;


        #endregion

        public Pattern(TokenParser parser, PatternType patternType = PatternType.Experimental) {
            Parser = parser;
            Type = patternType;
        }

        public override string ToString() {
            return ToString(false);
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

        public string ToString(bool useIdIfNameless) {
            if (Name != null || useIdIfNameless) {
                return Identity;
            }
            return Describe(false);
        }

        public string Describe(bool useIds = false) {
            if (Literal != null) return "'" + Literal.Replace("'", "''") + "'";
            return Root.ToString(true, false, useIds);
        }

        public Token Match(TokenChain chain, int startAt) {
            Token token = null;

            // If it's already been found previously then don't bother trying again
            if (chain.Heads[startAt].TryGetValue(Id, out token)) return token;

            // Literals are simple string matches
            if (Literal != null) {
                var snippet = chain.Text.Substring(startAt, Literal.Length);
                if (snippet != Literal) return null;
                token = new Token();
                token.Text = snippet;
                token.StartAt = startAt;
                token.Length = snippet.Length;

            } else {
                if (!Root.Match(chain, startAt, out int endAt)) return null;
                token = new Token();
                token.StartAt = startAt;
                token.Length = endAt - startAt;
                token.Text = chain.Text.Substring(startAt, token.Length);

            }

            token.Pattern = this;
            chain.Heads[startAt][token.Pattern.Id] = token;
            chain.Tails[startAt + token.Length - 1][token.Pattern.Id] = token;
            return token;
        }

    }
}
