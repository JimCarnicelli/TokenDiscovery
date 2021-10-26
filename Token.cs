using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenDiscovery {

    /// <summary>
    /// A single token completely matching a given pattern
    /// </summary>
    public class Token {

        public Pattern Pattern;

        public int StartAt;

        public int Length;

        public string Text;

        public override string ToString() {
            return Pattern + " >> (" + StartAt + " - " + (StartAt + Length) + ") '" + Text + "'";
        }

    }

}
