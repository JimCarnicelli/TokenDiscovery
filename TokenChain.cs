using System.Collections.Generic;

namespace TokenDiscovery {

    public class TokenChain {

        #region Public properties


        public string Text;

        public int Length;

        public Dictionary<int, Token>[] Heads;

        public Dictionary<int, Token>[] Tails;


        #endregion

        public TokenChain(string text) {
            Text = text;
            Length = text.Length;
            Heads = new Dictionary<int, Token>[Length];
            Tails = new Dictionary<int, Token>[Length];

            for (int i = 0; i < Length; i++) {
                Heads[i] = new Dictionary<int, Token>();
                Tails[i] = new Dictionary<int, Token>();
            }
        }

        public void Add(Token token) {
            Heads[token.StartAt][token.Pattern.Id] = token;
            Tails[token.StartAt + token.Length - 1][token.Pattern.Id] = token;
        }

        public string ToDebugString(PatternType minType = PatternType.Basics, bool includeEmpty = false) {
            string text = "";
            for (int i = 0; i < Length; i++) {
                var subText = "";
                foreach (var token in Heads[i].Values) {
                    if (token.Pattern.Type < minType) continue;
                    subText += "  " + token.Pattern.Identity + ": " + token.Text;
                    if (token.Pattern.Name == null) subText += "  | " + token.Pattern.ToString();
                    subText += "\n";
                }
                if (subText != "" || includeEmpty) {
                    text += "----------  " + i + "  -  " + Text[i] + "  ----------\n" + subText;
                }
            }

            if (text == "") return "<empty>";
            return text;
        }

    }

}
