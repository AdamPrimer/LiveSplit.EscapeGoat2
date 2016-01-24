using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Diagnostics.Runtime;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.Memory
{
    public struct ArrayPointer
    {
        public readonly ValuePointer Value;
        public readonly ClrType ElementType;
        public readonly ClrHeap Heap;
        public readonly int Length;
        public readonly bool HasLength;

        public ArrayPointer(ValuePointer value, ClrType elementType) {
            Value = value;
            ElementType = elementType;
            Heap = elementType.Heap;
            Length = 0;
            HasLength = false;
        }

        public ArrayPointer(ValuePointer value, ClrType elementType, int length) {
            Value = value;
            ElementType = elementType;
            Heap = elementType.Heap;
            Length = length;
            HasLength = true;
        }

        // Initialize from System.Collections.Generic.List
        public ArrayPointer(ValuePointer state, string fieldName, string fieldType) {
            var tList =  state.Heap.GetTypeByName("System.Collections.Generic.List<T>") // CLR 4.x
                         ?? 
                         state.Heap.GetTypeByName("System.Collections.Generic.List`1"); // CLR 2.x

            var list = state[fieldName];
            if (!list.HasValue) {
                throw new Exception(string.Format("Unable to find List<{1}> {0}", fieldType, fieldName));
            }

            var listList = list.Value.ForceCast(tList);

            Value = listList["_items"].Value.ForceCast("System.Object[]");
            ElementType = state.Heap.GetTypeByName(fieldType);
            Heap = state.Heap;
            Length = listList.GetFieldValue<Int32>("_size");
            HasLength = true;
        }

        public int Count {
            get {
                return Value.Type.GetArrayLength(Value.Address);
            }
        }

        public List<T> Read<T>() where T : new() {
            ClrType type = Heap.GetObjectType(Value.Address);

            // Only consider types which are arrays that do not have simple values (I.E., are structs).
            if (!type.IsArray || type.ComponentType.HasSimpleValue) {
                return null;
            }

            int len = type.GetArrayLength(Value.Address);

            List<T> list = new List<T>();

            FieldInfo[] fields = typeof(T).GetFields();

            for (int i = 0; i < len; i++) {
                ulong addr = type.GetArrayElementAddress(Value.Address, i);

                T item = new T();
                foreach (var field in type.ComponentType.Fields) {
                    if (!field.HasSimpleValue) continue;

                    for (int j = 0; j < fields.Length; j++) {
                        if (fields[j].Name == field.Name) {
                            var val = field.GetValue(addr, true);
                            typeof(T).GetField(field.Name).SetValueDirect(__makeref(item), val);
                        }
                    }
                }
                list.Add(item);
            }

            return list;
        }

        public ValuePointer? this[int index] {
            get {
                ulong address;
                if (ElementType.IsObjectReference)
                    address = (ulong)Value.Type.GetArrayElementValue(Value.Address, index);
                else
                    address = Value.Type.GetArrayElementAddress(Value.Address, index);

                var elementType = ElementType.Heap.GetObjectType(address);
                if (elementType != null)
                    return new ValuePointer(address, ElementType, this.Heap);
                else {
                    return null;
                }
            }
        }
    }
}
