using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.EscapeGoat2.State;
using LiveSplit.EscapeGoat2.Memory;

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
            goatState.goatTriggers.OnSplit += OnSplit;
            goatState.OnTimerStarted += goatState_OnLoadStarted;
            goatState.OnTimerFinished += goatState_OnLoadFinished;
            goatState.OnTimerChanged += goatState_OnIGTChanged;
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
            if (Model == null) {
                Model = new TimerModel() { CurrentState = state };
                state.OnReset += OnReset;
                state.OnPause += OnPause;
                state.OnResume += OnResume;
                state.OnStart += OnStart;
                state.OnSkipSplit += OnSkipSplit;
                state.OnSplit += OnLiveSplit;
            }

            goatState.Loop();
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnSkipSplit(object sender, EventArgs e) {
            write("[LiveSplit] Skip Split.");
            //goatState.goatTriggers.GoToNextSplit();
        }

        public void OnLiveSplit(object sender, EventArgs e) {
            write("[LiveSplit] Split.");
            //goatState.goatTriggers.GoToNextSplit();
        }

        public void OnResume(object sender, EventArgs e) {
            write("[LiveSplit] Resume.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnPause(object sender, EventArgs e) {
            write("[LiveSplit] Pause.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnStart(object sender, EventArgs e) {
            write("[LiveSplit] Start.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnReset(object sender, TimerPhase e) {
            write("[LiveSplit] Reset.");
            goatState.Reset();
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnSplit(object sender, SplitEventArgs e) {
            if (e.name == "Start") {
                write("[OriSplitter] Start.");
                Model.Start();
                _state.IsGameTimePaused = true;
            } else {
                int idx = _state.CurrentSplitIndex;
                if (idx != _state.Run.Count - 1) {
                    write(string.Format("[OriSplitter] Split {0} of {1}", idx, _state.Run.Count));
                    Model.Split();
                } else {
                    write("[OriSplitter] Last Split, Pausing Timer.");
                    Model.Pause();
                }

                Room room = (Room)e.value;
                write(string.Format("{0} Exited.", room.ToString()));
            }
        }

        public override void Dispose() {
            try {
                goatState.Dispose();
            } catch (Exception e) {
                write(e.ToString());
            }
        }

        void goatState_OnLoadStarted(object sender, EventArgs e) {
            //_state.IsGameTimePaused = false;
        }

        void goatState_OnLoadFinished(object sender, EventArgs e) {
            //_state.IsGameTimePaused = true;
            int idx = _state.CurrentSplitIndex;
            if (idx == _state.Run.Count - 1 && Model.CurrentState.CurrentPhase == TimerPhase.Paused) {
                write("[OriSplitter] Last Split, Stopping Timer.");
                Model.Pause();
                Model.Split();
            }
        }

        void goatState_OnIGTChanged(object sender, EventArgs e) {
            _state.SetGameTime((TimeSpan?)sender);
            _state.IsGameTimePaused = true;
        }

        public override Control GetSettingsControl(LayoutMode mode) {
            return null;
        }

        public override void SetSettings(XmlNode settings) {
        }

        public override XmlNode GetSettings(XmlDocument document) {
            return document.CreateElement("x");
        }

        private void write(string str) {
            #if DEBUG
            StreamWriter wr = new StreamWriter("_goatauto.log", true);
            wr.WriteLine("[" + DateTime.Now + "] " + str);
            wr.Close();
            #endif
        }
    }
}
