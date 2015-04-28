using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace LiveSplit.EscapeGoat2
{
    public struct ValuePointer
    {
        public readonly ulong Address;
        public readonly ClrType Type;
        public readonly ClrHeap Heap;

        public ValuePointer(ClrRoot r, ClrHeap heap) {
            // r.Address is the memory location of the root, not the thing it points to
            r.Type.Heap.ReadPointer(r.Address, out Address);
            Type = r.Type;
            Heap = heap;
        }

        public ValuePointer(ulong address, ClrType type, ClrHeap heap) {
            if (type == null)
                throw new ArgumentNullException("type");

            Address = address;
            Type = type;
            Heap = heap;
        }

        public ValuePointer? this[string fieldName] {
            get {
                var field = Type.GetFieldByName(fieldName);
                if (field == null)
                    throw new Exception("No field with this name");

                ulong address;

                if (field.IsObjectReference()) {
                    var fieldValue = field.GetValue(Address);
                    if (fieldValue == null)
                        return null;

                    address = (ulong)fieldValue;
                } else {
                    address = field.GetAddress(Address, false);
                }

                if (address == 0)
                    return null;

                return new ValuePointer(address, field.Type, this.Heap);
            }
        }

        public T GetFieldValue<T>(string fieldName) {
            var field = Type.GetFieldByName(fieldName);
            if (field == null)
                throw new Exception("No field with this name");
            return (T)Convert.ChangeType(field.GetValue(Address), typeof(T));
        }

        public T GetDeepFieldValue<T>(string[] fieldNames) {
            return (T)Convert.ChangeType(Type.GetFieldValue(Address, fieldNames), typeof(T));
        }

        public ValuePointer ForceCast(ClrType newType) {
            return new ValuePointer(Address, newType, this.Heap);
        }

        public ValuePointer ForceCast(string newTypeName) {
            var newType = Type.Heap.GetTypeByName(newTypeName);
            if (newType == null)
                throw new Exception("No type with this name");

            return ForceCast(newType);
        }

        public void EnumerateReferences(Action<ulong, int> action) {
            Type.EnumerateRefsOfObjectCarefully(Address, action);
        }

        public object Read() {
            ulong address = Address;
            // This is required due to a bug in Microsoft.Diagnostics.Runtime
            if (Type.IsPrimitive) {
               address -= (ulong)((long)this.Heap.PointerSize);
            }

            return Type.GetValue(address);
        }

        public T Read<T>() {
            object res = Read();
            if (res == null) {
                return default(T);
            }
            return (T)Convert.ChangeType(res, typeof(T));
        }

        public override string ToString() {
            return String.Format("<{0:X8} {1}>", Address, Type.Name);
        }

        private void write(string str) {
#if DEBUG
            StreamWriter wr = new StreamWriter("_goatauto.log", true);
            wr.WriteLine("[" + DateTime.Now + "] " + str);
            wr.Close();
#endif
        }
    }

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
            if (!type.IsArray || type.ArrayComponentType.HasSimpleValue) {
                return null;
            }

            int len = type.GetArrayLength(Value.Address);

            List<T> list = new List<T>();

            FieldInfo[] fields = typeof(T).GetFields();

            for (int i = 0; i < len; i++) {
                ulong addr = type.GetArrayElementAddress(Value.Address, i);

                T item = new T();

                foreach (var field in type.ArrayComponentType.Fields) {
                    for (int j = 0; j < fields.Length; j++) {
                        if (field.HasSimpleValue && fields[j].Name == field.Name) {
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

                //var elementType = ElementType.Heap.GetObjectType(address);
                //if (elementType != null)
                    return new ValuePointer(address, ElementType, this.Heap);
                //else { 
                //    return null;
                //}
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
