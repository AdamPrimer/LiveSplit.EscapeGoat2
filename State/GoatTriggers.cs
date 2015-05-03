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
        // This function is called when an event that will trigger an action in LiveSplit occurs
        public event SplitEventHandler OnSplit;

        // This is the required signature of the OnSplit function.
        public delegate void SplitEventHandler(object sender, SplitEventArgs e);

        public void SplitOnGameStart(bool status) {
            if (OnSplit != null) OnSplit(this, new SplitEventArgs("Start", status));
        }

        public void SplitOnEndRoom(Room room) {
            if (OnSplit != null) OnSplit(this, new SplitEventArgs("End Room", room));
        }
    }
}
