using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.Memory
{
    public class ProcessMangler : IDisposable
    {
        const uint AttachTimeout = 5000;
        const AttachFlag AttachMode = AttachFlag.Passive;

        public readonly DataTarget DataTarget;
        public readonly ClrRuntime Runtime;
        public readonly ClrHeap Heap;
        private Dictionary<string, ClrType> typeCache = new Dictionary<string, ClrType>();

        public ProcessMangler(int processId) {
            DataTarget = DataTarget.AttachToProcess(processId, AttachTimeout, AttachMode);
            Runtime = DataTarget.ClrVersions.First().CreateRuntime();
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

            return from o in Heap.EnumerateObjectAddresses()
                   let t = Heap.GetObjectType(o)
                   where hs.Contains(t.Index)
                   select new ValuePointer(o, t, this);
        }

        public IEnumerable<ValuePointer> AllValuesOfType(params string[] typeNames) {
            return AllValuesOfType(from tn in typeNames select GetTypeByName(tn));
        }

        public ClrType GetTypeByName(string typename)
        {
            if (typeCache.ContainsKey(typename))
                return typeCache[typename];
            ClrType ret = Heap.GetTypeByName(typename);
            typeCache[typename] = ret;
            return ret;
        }

        public ValuePointer? this[ulong address] {
            get {
                var t = Heap.GetObjectType(address);
                if (t == null)
                    return null;

                return new ValuePointer(address, t, this);
            }
        }

        public void Dispose() {
            DataTarget.Dispose();
        }
    }
}
