using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;
using LiveSplit.EscapeGoat2.Memory;

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

        public void ViewFields(ValuePointer point) {
            LogWriter.WriteLine(point.Type.Name.ToString());
            foreach (var field in point.Type.Fields) {
                string output;
                if (field.HasSimpleValue)
                    output = field.GetValue(point.Address).ToString();
                else
                    output = field.GetAddress(point.Address).ToString("X");

                LogWriter.WriteLine("  +{0,2:X2} {1} {2} = {3}", field.Offset, field.Type.Name, field.Name, output);
            }
        }
    }
}
