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
        public readonly ProcessMangler Mangler;

        public StaticField(ProcessMangler mangler, string typee, string name) {
            Domain = mangler.Runtime.AppDomains.First();
            ClrHeap heap = mangler.Runtime.GetHeap();
            ClrType type = mangler.GetTypeByName(typee);
            Field = type.GetStaticFieldByName(name);
            Mangler = mangler;
        }

        public StaticField(ProcessMangler mangler, ClrType type, string name) {
            Domain = mangler.Runtime.AppDomains.First();
            Field = type.GetStaticFieldByName(name);
            Mangler = mangler;
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

                return new ValuePointer(address, Type, Mangler);
            }
        }
    }
}
