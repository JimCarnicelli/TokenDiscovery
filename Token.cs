using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace TokenDiscovery {

    /// <summary>
    /// A single token completely matching a given pattern
    /// </summary>
    public class Token {

        public Pattern Pattern;

        [JsonPropertyName("Pattern")]
        public string PatternDescription {
            get {
                return Pattern.ToString();
            }
        }

        public int StartAt;

        public int Length;

        public string Text { get; set; }

        public List<Token> Children = new List<Token>();

        [JsonPropertyName("Children")]
        public List<Token> ChildrenForJson {
            get {
                if (Children.Where(e => e.Pattern.Type >= PatternType.Derived).Any()) {
                    return Children;
                }
                return null;
            }
        }

        public override string ToString() {
            return Pattern + " >> (" + StartAt + " - " + (StartAt + Length) + ") '" + Text + "'";
        }

    }

}
