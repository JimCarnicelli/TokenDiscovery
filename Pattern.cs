using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TokenDiscovery {
    public class Pattern {

        #region Public properties


        public TokenParser Parser;

        public int Id = -1;

        public string Name;

        public string Tag;

        public string Literal;

        public PatternPart Head;


        #endregion

        public Pattern(TokenParser parser) {
            Parser = parser;
        }

        private static Regex regexSafeName = new Regex("^[A-Za-z][-_A-Za-z0-9]*$");

        public override string ToString() {
            return ToString(false);
        }

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
