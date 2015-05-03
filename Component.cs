using System;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using LiveSplit.EscapeGoat2.State;
using LiveSplit.EscapeGoat2.Memory;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2
{
    public class EscapeGoat2Component : LogicComponent
    {
        //public EscapeGoat2Settings Settings { get; set; }

        public override string ComponentName {
            get { return "Escape Goat 2 Auto Splitter"; }
        }

        public GoatState goatState;
        private LiveSplitState _state;

        protected TimerModel Model { get; set; }

        public EscapeGoat2Component(LiveSplitState state) {
            _state = state;

            goatState = new GoatState();

            // Hook into the split triggers
            goatState.goatTriggers.OnSplit += OnSplitTriggered;

            // Hook into the in-game timer updates
            goatState.OnTimerFixed += goatState_OnIGTFixed; 
            goatState.OnTimerUpdated += goatState_OnIGTUpdated;
        }

        public override void Dispose() {
            // We need to appropriately dispose of the goatState as it will unhook from the process
            goatState.Dispose();
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
            // Hook a TimerModel to the current LiveSplit state
            if (Model == null) {
                Model = new TimerModel() { CurrentState = state };
                state.OnReset += OnReset;
                state.OnPause += OnPause;
                state.OnResume += OnResume;
                state.OnStart += OnStart;
                state.OnSplit += OnSplit;
                state.OnSkipSplit += OnSkipSplit;
                state.OnUndoSplit += OnUndoSplit;
            }

            // Update our goatState!
            goatState.Loop();
        }

        public void OnSplitTriggered(object sender, SplitEventArgs e) {
            // If we have received a Start event trigger, then we want to start our timer Model causing 
            // LiveSplit to begin counting. We then want to immediatly pause the in-game time tracking 
            // as we will be setting this absolutely based on reading the actual in-game time from 
            // the process memory. Pausing the in-game timer stops LiveSplit from getting in our way.
            if (e.name == "Start") {
                LogWriter.WriteLine("[GoatSplitter] Start.");
                Model.Start();
                _state.IsGameTimePaused = true;
            }

            // Escape Goat 2 only has one other condition for splitting, the end of a room. This event is 
            // called when: a door is entered, a soul shard is collected, or a glass fragment is obtained.
            // Due to the differences in IGT and RTA timings for Escape Goat 2, we do not want to split
            // if we are on the final split, we want to instead pause the timer. A more detailed explanation
            // for this is in the comments for `goatState_OnIGTFixed()`.
            else {
                LogWriter.WriteLine("[GoatSplitter] RTA Split {0} of {1}", _state.CurrentSplitIndex + 1, _state.Run.Count);
                if (!isLastSplit()) {
                    Model.Split();
                } else {
                    LogWriter.WriteLine("[GoatSplitter] RTA Last Split, Pausing Timer.");
                    Model.Pause();
                }

                Room room = (Room)e.value;
                LogWriter.WriteLine("[Room Exit] {0}", room);
            }
        }

        void goatState_OnIGTFixed(object sender, EventArgs e) {
            // Escape Goat 2 has two different timing methods. RTA and IGT. RTA timings are stopped
            // upon entering the last door (final input), while IGT continues for approximately
            // 2-3 seconds as it stops when the level fades out completely. As a result, we 
            // Pause LiveSplit when the final trigger occurs, this "stops" the RTA timer, but 
            // we can continue to update the IGT timer directly.
            
            // As the IGT never updates inside live split, we set its value absolutely, 
            // pausing LiveSplit therefore has the effect of "stopping" the RTA timer while 
            // IGT continues. 

            // Therefore, if we are on the final split, and we receive the "IGT is the same as 
            // the last time we checked" event, we know that the IGT has stopped for the last time.
            // We therefore unpause LiveSplit (by calling Pause again) so we can call Split 
            // (this cannot be called while paused) and then perform the final split.
            if (isLastSplit() && Model.CurrentState.CurrentPhase == TimerPhase.Paused) {
                LogWriter.WriteLine("[GoatSplitter] IGT Last Split, Stopping Timer.");
                Model.Pause();
                Model.Split();
            }
        }

        void goatState_OnIGTUpdated(object sender, EventArgs e) {
            // Set the In-Game Timer to be the value read from memory
            // and ensure the In-Game Timer in LiveSplit is set to paused
            // as we do not want LiveSplit to increment the time itself.
            _state.SetGameTime((TimeSpan?)sender);
            _state.IsGameTimePaused = true;
        }

        public bool isLastSplit() {
            int idx = _state.CurrentSplitIndex;
            return (idx == _state.Run.Count - 1);
        }

        public void OnUndoSplit(object sender, EventArgs e) {
            // On undo we want to reset the lastRoomID as we do not know the state
            // when the undo occured.
            LogWriter.WriteLine("[LiveSplit] Undo Split.");
            goatState.lastRoomID = 0;
        }

        public void OnReset(object sender, TimerPhase e) {
            // Reset the autosplitter state whenever LiveSplit is reset
            LogWriter.WriteLine("[LiveSplit] Reset.");
            goatState.Reset();
        }

        public void OnSkipSplit(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Skip Split.");
        }

        public void OnSplit(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Split.");
        }

        public void OnResume(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Resume.");
        }

        public void OnPause(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Pause.");
        }

        public void OnStart(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Start.");
        }

        public override Control GetSettingsControl(LayoutMode mode) {
            return null;
        }

        public override void SetSettings(XmlNode settings) { }

        public override XmlNode GetSettings(XmlDocument document) {
            return document.CreateElement("x");
        }
    }
}
