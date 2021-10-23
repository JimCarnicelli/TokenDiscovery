using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TokenDiscovery {

    public enum PatternType {
        Trivial,
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

        public PatternPart Head;


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

        public string ToString(bool useIdIfNameless) {
            if (Name != null || useIdIfNameless) {
                return Identity;
            }
            return Describe(false);
        }

        public string Describe(bool useIds = false) {
            if (Literal != null) return "'" + Literal.Replace("'", "''") + "'";
            return Head.ToString(true, false, useIds);
        }

    }
}
