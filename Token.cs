using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenDiscovery {

    /// <summary>
    /// A single token completely matching a given pattern
    /// </summary>
    public class Token {

        public Pattern Pattern;

        [JsonPropertyName("Pattern")]
        public string PatternName {
            get {
                return Pattern.Identity;
            }
        }

        public int StartAt;

        public int Length;

        public string Text { get; set; }

        public List<Token> Children { get; set; } = new List<Token>();

        public override string ToString() {
            return Pattern + " >> (" + StartAt + " - " + (StartAt + Length) + ") '" + Text + "'";
        }

    }

}
