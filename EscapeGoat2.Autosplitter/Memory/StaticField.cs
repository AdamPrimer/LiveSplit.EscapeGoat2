using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.Memory
{
    public class StaticField
    {
        public readonly ClrAppDomain Domain;
        public readonly ClrStaticField Field;
        public readonly ClrHeap Heap;

        public StaticField(ClrRuntime runtime, string typee, string name) {
            Domain = runtime.AppDomains.First();
            ClrHeap heap = runtime.GetHeap();
            ClrType type = heap.GetTypeByName(typee);
            Field = type.GetStaticFieldByName(name);
            Heap = runtime.GetHeap();
        }

        public StaticField(ClrRuntime runtime, ClrType type, string name) {
            Domain = runtime.AppDomains.First();
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
                return Field.GetAddress(Domain) != 0;
            }
        }

        public ValuePointer? Value {
            get {
                ulong address;
                if (Field.Type.IsObjectReference) {
                    var o = Field.GetValue(Domain);
                    if (o == null)
                        return null;

                    address = (ulong)o;
                } else {
                    address = Field.GetAddress(Domain);
                }

                if (address == 0)
                    return null;

                return new ValuePointer(address, Type, this.Heap);
            }
        }
    }
}
