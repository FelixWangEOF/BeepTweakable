using BeepTweakable;
using System;
using System.Collections;
using System.Collections.Generic;

// using System.Text;

namespace BeepTweakable
{
    struct beeper
    {
        public string bclass;
        public string filename;
    };

    struct beepAndSpan
    {
        public int span;
        public beep bp;
        private beep tmp;

        public beepAndSpan(int span, beep tmp) : this()
        {
            this.span = span;
            this.tmp = tmp;
        }
    };

    class allBeeps
    {
        private List<beep> beeps = new List<beep>();
        private int beepCounter = 0;
        private List<beeper> beepers = new List<beeper>();

        public beep NextBeep()
        {
            Console.WriteLine("Info: NextBeep called.");

            beep tmp;
            if (beepCounter >= beeps.Count)
            {
                Console.WriteLine("Info: All beeps reached.");
                //return null;
                DateTime somedt = new DateTime();
                tmp = new beep(somedt, "SYSINFO-NULL");
            }
            else
            {
                tmp = beeps[beepCounter];
            }
            beepCounter++;
            return tmp;
        }
        
        public void AddBeep(beep b)
        {
            Console.WriteLine("Info: AddBeep called");
            beeps.Add(b);
        }

        public void RegisterBeeper(string bc, string filename)
        {
            Console.WriteLine("Info: RegisterBeeper called.");

            beeper tmp;
            tmp.bclass = bc;
            tmp.filename = filename;
            beepers.Add(tmp);
        }

        public beeper SearchForBeeperByClass(string bc)
        {
            foreach(beeper tmp in beepers)
            {
                if(tmp.bclass == bc)
                {
                    Console.WriteLine("Info: Found bc from beepers list.");

                    return tmp;
                }
            }

            beeper an;
            an.bclass = "NULL";
            an.filename = "NULL";
            return an;
        }

        public beep SearchForBeepByHourMinute(int hr, int min)
        {
            foreach(beep tmp in beeps)
            {
                if((tmp.time.Hour == hr) && (tmp.time.Minute == min))
                {
                    // Found it.
                    Console.WriteLine("Info: Found beep from beeps list.");

                    return tmp;
                }
            }

            var notime = new DateTime();
            beep an = new beep(notime, "NULL");
            return an;
        }

        public beep SearchForNearestBeepByHourMinute(int hr, int min)
        {
            var SpanAndBeep = new List<beepAndSpan>();
            foreach(beep tmp in beeps)
            {
                var span = (tmp.time.Hour - hr) * 60 + (tmp.time.Minute - min);
                beepAndSpan item = new beepAndSpan(span, tmp);
                SpanAndBeep.Add(item);
            }
            SpanAndBeep.Sort((IComparer<beepAndSpan>)BeepAndSpanComparer.Default);
            if(SpanAndBeep[0].span >= SpanAndBeep[^1].span)
            {
                SpanAndBeep[0] = SpanAndBeep[^1];
            }
            return SpanAndBeep[0].bp;
        }
    }
}

public class BeepAndSpanComparer : IComparer
{
    public static IComparer Default = new BeepAndSpanComparer();
    public int Compare(object a, object b)
    {
        return Math.Abs(((beepAndSpan)a).span - ((beepAndSpan)b).span);
    }
}