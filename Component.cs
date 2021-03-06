﻿using System;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2
{
    public class EscapeGoat2Component : LogicComponent
    {
        public override string ComponentName {
            get { return "Escape Goat 2 Auto Splitter"; }
        }

        private LiveSplitState _state;
        private Process process;

        protected TimerModel Model { get; set; }

        public EscapeGoat2Component(LiveSplitState state) {
            _state = state;

            ProcessStartInfo processStartInfo;

            processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.Arguments = "";
            processStartInfo.FileName = "Components/EscapeGoat2.Autosplitter.exe";

            process = new Process();
            process.StartInfo = processStartInfo;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate(object sender, DataReceivedEventArgs e) {
                    if (!String.IsNullOrEmpty(e.Data)) {
                        string line = e.Data.ToString();
                        string[] cmd = line.Split();

                        if (cmd[0] == "Start") {
                            DoStart();
                        } else if (cmd[0] == "Split") {
                            DoSplit();
                        } else if (cmd[0] == "IGT") {
                            if (cmd[1] != "Fixed") {
                                _state.SetGameTime(TimeSpan.Parse(cmd[1]));
                                _state.IsGameTimePaused = true;
                            } else {
                                DoEndGameSplit();
                            }
                        }
                    }
                }
            );

            process.Start();
            process.BeginOutputReadLine();
        }

        public override void Dispose() {
            if (process != null && !process.HasExited) {
                process.CancelOutputRead();
                process.CloseMainWindow();
                process.Kill();
                process.Close();
            }

            if (Model != null) {
                Model.CurrentState.OnReset -= OnReset;
                Model.CurrentState.OnPause -= OnPause;
                Model.CurrentState.OnResume -= OnResume;
                Model.CurrentState.OnStart -= OnStart;
                Model.CurrentState.OnSplit -= OnSplit;
                Model.CurrentState.OnSkipSplit -= OnSkipSplit;
                Model.CurrentState.OnUndoSplit -= OnUndoSplit;
            }
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
        }

        public void OnUndoSplit(object sender, EventArgs e) {
            // On undo we want to reset the lastRoomID as we do not know the state
            // when the undo occured.
            LogWriter.WriteLine("[LiveSplit] Undo Split.");
            process.StandardInput.WriteLine("undo");
        }

        public void OnReset(object sender, TimerPhase e) {
            // Reset the autosplitter state whenever LiveSplit is reset
            LogWriter.WriteLine("[LiveSplit] Reset.");
            process.StandardInput.WriteLine("reset");
        }

        public void DoStart() {
            LogWriter.WriteLine("[GoatSplitter] Start.");
            Model.Start();
            _state.IsGameTimePaused = true;
        }

        public void DoSplit() {
            LogWriter.WriteLine("[GoatSplitter] RTA Split {0} of {1}", _state.CurrentSplitIndex + 1, _state.Run.Count);
            if (!isLastSplit()) {
                Model.Split();
            } else {
                LogWriter.WriteLine("[GoatSplitter] RTA Last Split, Pausing Timer.");
                Model.Pause();
            }
        }

        public void DoEndGameSplit() {
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

        public bool isLastSplit() {
            int idx = _state.CurrentSplitIndex;
            return (idx == _state.Run.Count - 1);
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
