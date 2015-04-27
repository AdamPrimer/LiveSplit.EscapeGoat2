using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace LiveSplit.EscapeGoat2Autosplitter
{
    public class StaticField
    {
        public readonly ClrAppDomain Domain;
        public readonly ClrStaticField Field;
        public readonly ClrHeap Heap;

        public StaticField(ClrRuntime runtime, string typee, string name) {
            Domain = runtime.AppDomains.First();
            if (Domain == null) {
                throw new Exception("Domain is null.");
            }

            ClrHeap heap = runtime.GetHeap();

            if (heap == null) {
                throw new Exception("Heap is null.");
            }

            ClrType type = heap.GetTypeByName(typee);

            if (type == null) {
                throw new Exception("Type is null.");
            }
            if (runtime == null) {
                throw new Exception("Runtime is null.");
            }
            Field = type.GetStaticFieldByName(name);
            Heap = runtime.GetHeap();
        }

        public StaticField(ClrRuntime runtime, ClrType type, string name) {
            Domain = runtime.AppDomains.First();
            if (Domain == null) {
                throw new Exception("Domain is null.");
            }
            if (type == null) {
                throw new Exception("Type is null.");
            }
            if (runtime == null) {
                throw new Exception("Runtime is null.");
            }
            Field = type.GetStaticFieldByName(name);
            Heap = runtime.GetHeap();
        }

        public ClrType Type {
            get {
                return Field.Type;
            }
        }

        public bool IsAccessible {
            get {
                return Field.GetFieldAddress(Domain) != 0;
            }
        }

        public ValuePointer? Value {
            get {
                ulong address;
                if (Field.Type.IsObjectReference) {
                    var o = Field.GetFieldValue(Domain);
                    if (o == null)
                        return null;

                    address = (ulong)o;
                } else {
                    address = Field.GetFieldAddress(Domain);
                }

                if (address == 0)
                    return null;

                return new ValuePointer(address, Type, this.Heap);
            }
        }
    }
}
