using System;
using System.Collections;
using System.Collections.Generic;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer {
    public class ArrayPointer<T> : IEnumerable<T> where T : unmanaged
    {
        private Pointer<ArrayBody> _pointer;
        private TickableProcessWrapper _wrapper;

        public ArrayBody New {
            get => _pointer.New;
            set => _pointer.New = value;
        }
        
        public ArrayBody Old {
            get => _pointer.Old;
            set => _pointer.Old = value;
        }

        public bool CountChanged => !Old.Count.Equals(New.Count);

        public bool DataReferenceChanged => !Old.DataReference.Equals(New.DataReference);

        public bool UpdateOnNullPointer {
            get => _pointer.UpdateOnNullPointer;
            set => _pointer.UpdateOnNullPointer = value;
        }

        internal ArrayPointer(Pointer<ArrayBody> array, TickableProcessWrapper wrapper) {
            _pointer = array;
            _wrapper = wrapper;
        }

        public IEnumerator<T> GetEnumerator() {
            int count = New.Count;
            IntPtr entries = New.DataReference;
            for(int index = 0; index < count; index++) {
                IntPtr entryPtr = entries + Stride * index;
                yield return _wrapper.Read<T>(entryPtr);
            }
        }

        private unsafe int Stride => sizeof(T);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class StringArrayPointer : IEnumerable<string>
    {
        private Pointer<ArrayBody> _pointer;
        private TickableProcessWrapper _wrapper;

        public ArrayBody New {
            get => _pointer.New;
            set => _pointer.New = value;
        }
        
        public ArrayBody Old {
            get => _pointer.Old;
            set => _pointer.Old = value;
        }

        public bool CountChanged => !Old.Count.Equals(New.Count);

        public bool DataReferenceChanged => !Old.DataReference.Equals(New.DataReference);

        public bool UpdateOnNullPointer {
            get => _pointer.UpdateOnNullPointer;
            set => _pointer.UpdateOnNullPointer = value;
        }

        public EStringType StringType { get; set; }


        internal StringArrayPointer(Pointer<ArrayBody> array, TickableProcessWrapper wrapper) {
            _pointer = array;
            _wrapper = wrapper;
        }

        public IEnumerator<string> GetEnumerator() {
            int count = New.Count;
            IntPtr entries = New.DataReference;
            for(int index = 0; index < count; index++) {
                IntPtr entryPtr = entries + (_wrapper.Is64Bit?8:4) * index;
                yield return _wrapper.ReadString(entryPtr, StringType);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}