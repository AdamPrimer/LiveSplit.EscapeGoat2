﻿using System;
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
            goatState.goatTriggers.OnSplit += OnSplit;
            goatState.OnTimerChanged += goatState_OnIGTChanged;
            goatState.OnTimerFixed += goatState_OnIGTFixed;
            goatState.OnTimerUpdated += goatState_OnIGTUpdated;
        }

        public override void Dispose() {
            try {
                goatState.Dispose();
            } catch (Exception e) {
                LogWriter.WriteLine(e.ToString());
            }
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

        public void OnReset(object sender, TimerPhase e) {
            LogWriter.WriteLine("[LiveSplit] Reset.");
            goatState.Reset();
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnSplit(object sender, SplitEventArgs e) {
            if (e.name == "Start") {
                LogWriter.WriteLine("[OriSplitter] Start.");
                Model.Start();
                _state.IsGameTimePaused = true;
            } else {
                if (!isLastSplit()) {
                    LogWriter.WriteLine(string.Format("[OriSplitter] Split {0} of {1}", _state.CurrentSplitIndex, _state.Run.Count));
                    Model.Split();
                } else {
                    LogWriter.WriteLine("[OriSplitter] Last Split, Pausing Timer.");
                    Model.Pause();
                }

                Room room = (Room)e.value;
                LogWriter.WriteLine("{0} Exited.", room);
            }
        }

        void goatState_OnIGTFixed(object sender, EventArgs e) {
            int idx = _state.CurrentSplitIndex;
            if (isLastSplit() && Model.CurrentState.CurrentPhase == TimerPhase.Paused) {
                LogWriter.WriteLine("[OriSplitter] Last Split, Stopping Timer.");
                Model.Pause();
                Model.Split();
            }
        }

        void goatState_OnIGTUpdated(object sender, EventArgs e) {
            _state.SetGameTime((TimeSpan?)sender);
            _state.IsGameTimePaused = true;
        }

        void goatState_OnIGTChanged(object sender, EventArgs e) { }

        public bool isLastSplit() {
            int idx = _state.CurrentSplitIndex;
            return (idx == _state.Run.Count - 1);
        }

        public void OnSkipSplit(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Skip Split.");
        }

        public void OnLiveSplit(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Split.");
        }

        public void OnResume(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Resume.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnPause(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Pause.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
        }

        public void OnStart(object sender, EventArgs e) {
            LogWriter.WriteLine("[LiveSplit] Start.");
            goatState.goatTriggers.timerRunning = (Model.CurrentState.CurrentPhase == TimerPhase.Running);
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
