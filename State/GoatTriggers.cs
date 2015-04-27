using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LiveSplit.EscapeGoat2;
using LiveSplit.EscapeGoat2.Memory;

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
        public delegate void OnSplitHandler(object sender, SplitEventArgs e);
        public event OnSplitHandler OnSplit;

        public bool timerRunning = false;

        public void SplitOnGameStart(bool status) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("Start", status));
            } else {
                write("Onsplit is null");
            }
        }

        public void SplitOnEndRoom(Room room) {
            if (OnSplit != null) {
                OnSplit(this, new SplitEventArgs("End Room", room));
            }
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
