using System;
// using System.Collections.Generic;

namespace BeepTweakable
{
    class beep
    {
        public DateTime time { get; }
        public string beepClass { get; }

        public beep(DateTime t, string bc)
        {
            time = t;
            beepClass = bc;
        }
    }
}
