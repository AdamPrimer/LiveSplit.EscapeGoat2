using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiveSplit.EscapeGoat2Autosplitter;
using LiveSplit.EscapeGoat2Autosplitter.Memory;

namespace LiveSplit.EscapeGoat2Autosplitter.State
{
    public class SplitEventArgs : EventArgs
    {
        public string name { get; set; }
        public object value { get; set; }
        public SplitEventArgs(string name, object value) {
            this.name = name;
            this.value = value;
        }
    }

    public class GoatTriggers
    {
        public delegate void OnSplitHandler(object sender, SplitEventArgs e);
        public event OnSplitHandler OnSplit;

        public bool timerRunning = false;

        public void SplitOnGameStart(bool status) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("Start", status));
            }
        }

        public void SplitOnEndRoom(Room room) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("End Room", room));
            }
        }
    }
}
