using System;
using System.Globalization;
using System.Xml;
using System.Timers;
using System.Collections.Generic;
// using System.Media;
using System.IO;
using SpeechLib;
using NAudio.CoreAudioApi;
using System.Linq;
using NAudio.Wave;

namespace BeepTweakable
{
    class Program
    {
        static allBeeps toBeep = new allBeeps();


        static int GetCurrentMicVolume()
        {
            int volume = 0;
            var enumerator = new MMDeviceEnumerator();

            IEnumerable<MMDevice> captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
            if (captureDevices.Count() > 0)
            {
                MMDevice mMDevice = captureDevices.ToList()[0];
                volume = (int)(mMDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            return volume;
        }


        static void SetCurrentMicVolume(int volume)
        {
            var enumerator = new MMDeviceEnumerator();
            IEnumerable<MMDevice> captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
            if (captureDevices.Count() > 0)
            {
                MMDevice mMDevice = captureDevices.ToList()[0];
                mMDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100.0f;
            }
        }

        static void Main(string[] args)
        {
            //[DllImport("user32.dll")]
            
            Console.WriteLine("Beep Tweakable Version 0.6 Public Evaluation Build 4");
            Console.WriteLine("Copyright 2020 Felix Wang. All rights reserved.");

            Console.WriteLine();
            Console.WriteLine("Update Log:");
            try
            {
                using (StreamReader sr = File.OpenText("Update.log"))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(s);
                    }
                }
            }
            catch (FileNotFoundException exp)
            {
                Console.WriteLine("Warning: FileNotFoundException handled. Directory not complete?");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");

                // // return;
                //Environment.Exit(-1);
            }
            Console.WriteLine();

            string filename;
            if (args.Length <= 0)
            {
                Console.WriteLine("Warning: No input file... Using default.xml");
                filename = "default.xml";
            }
            else
            {
                Console.WriteLine("Info: Using assigned file: " + args[0]);
                filename = args[0];
            }

            XmlDocument source = new XmlDocument();
            source.PreserveWhitespace = true;
            try
            {
                source.Load(filename);
            }
            catch(FileNotFoundException exp)
            {
                Console.WriteLine("Error: FileNotFoundException handled. Aborts.");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");
                // return;
                Environment.Exit(-1);
            }

            Console.WriteLine("Info: Begin parsing XML...");
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true
            };
            XmlReader reader = XmlReader.Create(filename, settings);
            source.Load(reader);

            // Parsing
            XmlNode root = source.SelectSingleNode("bt");
            XmlNodeList nodes1 = root.ChildNodes;

            // allBeeps toBeep = new allBeeps();

            foreach(XmlNode node in nodes1)
            {
                // Node (beep) structure
                // <beep time="19:40" class="default"/>
                if(node.Name == "config")
                {
                    Console.WriteLine("Info: Setting up file to play...");
                    
                    XmlElement element = (XmlElement)node;
                    if(element.GetAttribute("type").ToString() == "beeper")
                    {
                        Console.WriteLine("Info: Setting up beeper...");

                        toBeep.RegisterBeeper(element.GetAttribute("class").ToString(), element.GetAttribute("name"));
                    }
                }
                else if(node.Name == "beep")
                {
                    Console.WriteLine("Info: Setting up new beep...");

                    XmlElement element = (XmlElement)node;
                    string time = element.GetAttribute("time");
                    string bc = element.GetAttribute("class");

                    // Parse DateTime
                    // Time format: HH:mm
                    try
                    {
                        var cinfo = new CultureInfo("en-US");
                        var dtobj = DateTime.ParseExact(time, "HH:mm", cinfo);

                        beep tmp = new beep(dtobj, bc);
                        toBeep.AddBeep(tmp);
                    }
                    catch(FormatException exp)
                    {
                        Console.WriteLine("Error: FormatException handled. Aborts.");
                        Console.WriteLine($"[Data]    {exp.Data}");
                        Console.WriteLine($"[Message] {exp.Message}");
                        Environment.Exit(-1);
                    }
                }
                // Other things are ignored~ So tired...
            }
            reader.Close();

            // Parsing Done.
            Console.WriteLine("Info: Parsing Done!");
            Console.WriteLine("Info: Constructing Timers...");

            List<Timer> timers = new List<Timer>();
            while(true)
            {
                // Request next beep.
                beep next = toBeep.NextBeep();
                
                // Check for "SYSINFO-NULL"
                if(next.beepClass == "SYSINFO-NULL")
                {
                    // No beeps left.
                    break;
                }

                //next.time.Hour
                //TimeSpan ts = DateTime.Now - next.time;
                //int ms = ts.Milliseconds;

                double ms = (next.time.Hour - DateTime.Now.Hour) * 3600;
               // Console.Write("Info " + ms);
                ms += (next.time.Minute - DateTime.Now.Minute) * 60;
                //Console.Write(" " + ms);
                ms -= DateTime.Now.Second;
                //Console.Write(" " + ms);
                ms *= 1000;
                //Console.Write(" " + ms);  
                ms -= DateTime.Now.Millisecond;
                //Console.Write(" " + ms);
                Console.WriteLine($"Info: Variable {nameof(ms)} = {ms};");

                Timer aTimer;
                try
                {
                    aTimer = new Timer(ms);
                    aTimer.Elapsed += onTimedEvent;
                    aTimer.AutoReset = false;

                    timers.Add(aTimer);
                    timers[^1].Start(); // ^1 = timers.Count-1
                }
                catch(ArgumentException exp)
                {
                    Console.WriteLine("Error: ArgumentException handled. Time past? (Not Fatal)");
                    Console.WriteLine($"[Data]    {exp.Data}");
                    Console.WriteLine($"[Message] {exp.Message}");
                }
            }

            // Waiting loop
            Console.WriteLine("Info: Timer constrution done.");
            Console.WriteLine("Info: Press Q to quit the application.");
            while (true)
            {
                if (Console.ReadKey(true).KeyChar == 'Q')
                {
                    Console.WriteLine("Warning: You are about to quit. Quit?");
                    Console.Write("(y/N) ");
                    if (Console.ReadKey(false).KeyChar.ToString().ToUpper() == "Y")
                    {
                        // Quit.
                        Console.WriteLine("Info: Terminating application...");
                        foreach (Timer t in timers)
                        {
                            t.Stop();
                            t.Dispose();
                        }

                        //Console.WriteLine("Info: Application terminated.");
                        break;
                    }
                    else
                    {
                        // Return.
                        Console.WriteLine("\nInfo: Confirmation aborted.");
                    }
                }
            }

            Console.WriteLine("Info: Application terminated.");
        }

        private static void onTimedEvent(Object src, ElapsedEventArgs e)
        {
            // Time reached!
            Console.WriteLine("Info: Time reached. ElapsedEvent captured by onTimeEvent()");
            Console.WriteLine("Info: Requesting information about the comming beep...");
            Console.WriteLine("Info: Signal Time: " + e.SignalTime.Hour + ":" + e.SignalTime.Minute);

            SpVoice sp = new SpVoiceClass
            {
                Volume = 100
            };
            sp.Speak("Time Elapsed");
            //sp.Volume = 100;

            beep thisBeep = toBeep.SearchForBeepByHourMinute(e.SignalTime.Hour, e.SignalTime.Minute);
            beeper thisBeeper = toBeep.SearchForBeeperByClass(thisBeep.beepClass);
            string toSound = thisBeeper.filename;

            Console.WriteLine("Info: toSound = " + toSound);

            //try
            //{
            //    if (toSound == "NULL")
            //    {
            //        throw new ArgumentOutOfRangeException("Current toggled time cannot be found. Time past? (Not Fatal)");
            //    }
            //}
            //catch(ArgumentOutOfRangeException exp)
            //{
            //    Console.WriteLine("Error: ArgumentOutOfRangeException handled. Not-Fatal.");
            //    Console.WriteLine($"[Data]    {exp.Data}");
            //    Console.WriteLine($"[Message] {exp.Message}");
            //    //return;
            //    Console.WriteLine("Info: Trying to find using SearchForNearestBeepByHourMinute()...");
            //    thisBeep = toBeep.SearchForNearestBeepByHourMinute(e.SignalTime.Hour, e.SignalTime.Minute);
            //}

            if(toSound == "NULL")
            {
                thisBeep = toBeep.SearchForNearestBeepByHourMinute(e.SignalTime.Hour, e.SignalTime.Minute);
                thisBeeper = toBeep.SearchForBeeperByClass(thisBeep.beepClass);
                toSound = thisBeeper.filename;
            }

            // Check if .wav
            try
            {
                if (Path.GetExtension(toSound) != ".wav")
                {
                    throw new InvalidDataException("Not a .wav file!");
                }
            }
            catch(InvalidDataException exp)
            {
                Console.WriteLine("Error: InvalidDataException handled. Aborts.");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");

                Environment.Exit(-1);
            }

            // Now to sound the file of 'toSound'
            try
            {
                //// Initialize player
                //Console.WriteLine("Info: Calling SoundPlayer...");
                //SoundPlayer player = new SoundPlayer(toSound);

                //// Backup current Mic Volume
                //int previousMicVol = GetCurrentMicVolume();

                //// Activate player
                //player.Play();

                //// Restore previous Mic Volume
                //SetCurrentMicVolume(previousMicVol);

                // New method with NAudio
                Console.WriteLine("Info: Calling SoundPlayer... Using NAudio API...");

                // Backup current Mic Vol
                int previousMicVol = GetCurrentMicVolume();

                // Play
                using (var audioFile = new AudioFileReader(toSound))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        // Busy Wait Do Nothing...
                    }
                }

                //Restore
                SetCurrentMicVolume(previousMicVol);
            }
            catch(TimeoutException exp)
            {
                Console.WriteLine("Error: TimeoutException handled.");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");

                Environment.Exit(-1);
            }
            catch(FileNotFoundException exp)
            {
                Console.WriteLine("Error: FileNotFoundException handled.");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");

                Environment.Exit(-1);
            }
            catch(InvalidOperationException exp)
            {
                Console.WriteLine("Error: InvalidOperationException handled.");
                Console.WriteLine($"[Data]    {exp.Data}");
                Console.WriteLine($"[Message] {exp.Message}");

                Environment.Exit(-1);
            }
            

            Console.WriteLine("Info: The event handler function is done.");
        }
    }
}
