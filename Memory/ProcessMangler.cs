using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Diagnostics.Runtime;

namespace LiveSplit.EscapeGoat2Autosplitter
{
    public class ProcessMangler : IDisposable
    {
        const uint AttachTimeout = 5000;
        const AttachFlag AttachMode = AttachFlag.Passive;

        public readonly DataTarget DataTarget;
        public readonly ClrRuntime Runtime;
        public readonly ClrHeap Heap;

        public ProcessMangler(int processId) {
            DataTarget = DataTarget.AttachToProcess(processId, AttachTimeout, AttachMode);
            var dac = DataTarget.ClrVersions.First().TryGetDacLocation();
            write(DataTarget.ClrVersions.First().Version.ToString());
            write(DataTarget.ClrVersions.First().DacInfo.ToString());

            if (dac == null)
                throw new Exception("// Couldn't get DAC location.");
            Runtime = DataTarget.CreateRuntime(dac);
            Heap = Runtime.GetHeap();
        }

        public IEnumerable<ClrRoot> StackLocals {
            get {
                foreach (var thread in Runtime.Threads) {
                    foreach (var r in thread.EnumerateStackObjects())
                        yield return r;
                }
            }
        }

        public IEnumerable<ValuePointer> AllValuesOfType(params ClrType[] types) {
            return AllValuesOfType((IEnumerable<ClrType>)types);
        }

        private IEnumerable<ValuePointer> AllValuesOfType(IEnumerable<ClrType> types) {
            var hs = new HashSet<int>(from t in types select t.Index);

            return from o in Heap.EnumerateObjects()
                   let t = Heap.GetObjectType(o)
                   where hs.Contains(t.Index)
                   select new ValuePointer(o, t, Heap);
        }

        public IEnumerable<ValuePointer> AllValuesOfType(params string[] typeNames) {
            return AllValuesOfType(from tn in typeNames select Heap.GetTypeByName(tn));
        }

        public ValuePointer? this[ulong address] {
            get {
                var t = Heap.GetObjectType(address);
                if (t == null)
                    return null;

                return new ValuePointer(address, t, Heap);
            }
        }

        public void Dispose() {
            DataTarget.Dispose();
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
