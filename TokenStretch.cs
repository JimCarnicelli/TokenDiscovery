using System.Collections.Generic;

namespace TokenDiscovery {

    /// <summary>
    /// Linear list of tokens
    /// </summary>
    public class TokenStretch : List<Token> {

        public int StartAt {
            get {
                if (Count == 0) return 0;
                return this[0].StartAt;
            }
        }

        public int Length {
            get {
                if (Count == 0) return 0;
                var last = this[Count - 1];
                return (last.StartAt + last.Length) - this[0].StartAt;
            }
        }

        public string Text {
            get {
                if (Count == 0) return null;
                string text = "";
                foreach (var token in this) {
                    text += token.Text;
                }
                return text;
            }
        }

    }
}
