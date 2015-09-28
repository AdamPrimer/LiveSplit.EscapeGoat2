using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace LiveSplit.EscapeGoat2.Debugging
{
    public class LogWriter
    {
        public static void WriteLine(string format, params object[] arg) {
#if DEBUG
            string str = format;
            if (arg.Length > 0)
                str = String.Format(format, arg);

            StreamWriter wr = new StreamWriter("_goatauto.log", true);
            wr.WriteLine("[" + DateTime.Now + "] " + str);
            wr.Close();
#endif
        }
    }
}
