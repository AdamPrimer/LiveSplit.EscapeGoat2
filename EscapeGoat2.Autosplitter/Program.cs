using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using LiveSplit.EscapeGoat2.State;
using LiveSplit.EscapeGoat2.Memory;
using LiveSplit.EscapeGoat2.Debugging;

namespace EscapeGoat2.Autosplitter
{
    class Program
    {
        public static GoatState goatState;
        private const int TARGET_UPDATE_RATE = 1;
        private static int cTimeFixed = 0;

        static void Main(string[] args) {
            LogWriter.WriteLine("[GoatSplitter] Launched");
            goatState = new GoatState();

            // Hook into the split triggers
            goatState.goatTriggers.OnSplit += OnSplitTriggered;

            // Hook into the in-game timer updates
            goatState.OnTimerFixed += goatState_OnIGTFixed;
            goatState.OnTimerChanged += goatState_OnIGTChanged;
            goatState.OnTimerUpdated += goatState_OnIGTUpdated;

            new Thread(new ThreadStart(ReceiveCommands)).Start();

            bool hasExited = false;

            var profiler = Stopwatch.StartNew();
            while (!hasExited) {
                Update();

                if (profiler.ElapsedMilliseconds >= TARGET_UPDATE_RATE)
                    Debug.WriteLine("Update iteration took too long: " + profiler.ElapsedMilliseconds);

                Thread.Sleep(Math.Max(TARGET_UPDATE_RATE - (int)profiler.ElapsedMilliseconds, 1));
                profiler.Restart();
            }
        }

        public static void ReceiveCommands() {
            string line;
            while ((line = Console.ReadLine()) != null) {
                LogWriter.WriteLine("Received {0}", line);
                if (line == "reset") {
                    goatState_Reset();
                } else if (line == "undo") {
                    goatState_UndoSplit();
                }
            }
        }

        public static void Update() {
            goatState.Loop();
        }

        static public void OnSplitTriggered(object sender, SplitEventArgs e) {
            // If we have received a Start event trigger, then we want to start our timer Model causing 
            // LiveSplit to begin counting. We then want to immediatly pause the in-game time tracking 
            // as we will be setting this absolutely based on reading the actual in-game time from 
            // the process memory. Pausing the in-game timer stops LiveSplit from getting in our way.
            if (e.name == "Start") {
                Console.WriteLine("Start");
            }

            // Escape Goat 2 only has one other condition for splitting, the end of a room. This event is 
                // called when: a door is entered, a soul shard is collected, or a glass fragment is obtained.
                // Due to the differences in IGT and RTA timings for Escape Goat 2, we do not want to split
                // if we are on the final split, we want to instead pause the timer. A more detailed explanation
                // for this is in the comments for `goatState_OnIGTFixed()`.
            else {
                Room room = (Room)e.value;
                Console.WriteLine("Split {0}", room);
            }
        }

        static void goatState_OnIGTFixed(object sender, EventArgs e) {
            if (cTimeFixed * TARGET_UPDATE_RATE > 50) {
                Console.WriteLine("IGT Fixed");
            } else {
                cTimeFixed = cTimeFixed + 1;
            }
        }

        static void goatState_OnIGTChanged(object sender, EventArgs e) {
            cTimeFixed = 0;
        }

        static void goatState_OnIGTUpdated(object sender, EventArgs e) {
            Console.WriteLine("IGT {0}", (TimeSpan?)sender);
        }

        static public void goatState_UndoSplit() {
            // On undo we want to reset the lastRoomID as we do not know the state
            // when the undo occured.
            goatState.lastRoomID = 0;
        }

        static public void goatState_Reset() {
            // Reset the autosplitter state whenever LiveSplit is reset
            goatState.Reset();
        }
    }
}
