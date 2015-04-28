using System;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.State
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
        public event SplitEventHandler OnSplit;

        public delegate void SplitEventHandler(object sender, SplitEventArgs e);

        public bool timerRunning = false;

        public void SplitOnGameStart(bool status) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("Start", status));
            } else {
                LogWriter.WriteLine("Onsplit is null");
            }
        }

        public void SplitOnEndRoom(Room room) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("End Room", room));
            }
        }
    }
}
