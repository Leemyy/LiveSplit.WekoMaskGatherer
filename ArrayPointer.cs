using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer {
    public class ArrayPointer<T> where T : unmanaged {
        private const int MaxBytesToRead = 4096;

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

        public IEnumerable<T> Slots_Old() {
            int count = New.Count;
            IntPtr entries = New.DataReference;

            for(int index = 0; index < count; index++) {
                IntPtr entryPtr = entries + Stride * index;
                yield return _wrapper.Read<T>(entryPtr);
            }
        }

        public IEnumerable<T> Slots() {
            int count = New.Count;
            if (count == 0) return Enumerable.Empty<T>();
            if (count == 1) return new[] { _wrapper.Read<T>(New.DataReference) };
            return SlotsInternal();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IEnumerable<T> SlotsInternal() {
            int count = New.Count;
            IntPtr data = New.DataReference;
            var entriesPerBatch = Math.Max(MaxBytesToRead / Stride, 1);
            var maxBytesToRead = entriesPerBatch * Stride;
            var totalBytesToRead = count * Stride;
            if(totalBytesToRead <= maxBytesToRead) {
                _wrapper.Process.ReadBytes(data, totalBytesToRead, out var buffer);
                return SlotsInternal_Simple(buffer);
            }

            return SlotsInternal_Complex(data, maxBytesToRead, totalBytesToRead);
        }

        private IEnumerable<T> SlotsInternal_Simple(byte[] buffer) {
            for(int offset = 0; offset < buffer.Length; offset += Stride) {
                yield return buffer.To<T>(offset);
            }
        }

        private IEnumerable<T> SlotsInternal_Complex(IntPtr data, int maxBytesToRead, int totalBytesToRead) {
            byte[] buffer;
            int offset = 0;
            int bufferOffset;
            for(; offset + maxBytesToRead < totalBytesToRead; offset += maxBytesToRead) {
                _wrapper.Process.ReadBytes(data + offset, maxBytesToRead, out buffer);
                for(bufferOffset = 0; bufferOffset < maxBytesToRead; bufferOffset += Stride) {
                    yield return buffer.To<T>(bufferOffset);
                }
            }

            if (offset >= totalBytesToRead) yield break;
            maxBytesToRead = totalBytesToRead - offset;
            _wrapper.Process.ReadBytes(data + offset, maxBytesToRead, out buffer);
            for(bufferOffset = 0; bufferOffset < maxBytesToRead; bufferOffset += Stride) {
                yield return buffer.To<T>(bufferOffset);
            }
        }

        private unsafe int Stride => sizeof(T);
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
                IntPtr entryPtr = entries + _wrapper.PointerSize * index;
                yield return _wrapper.ReadString(entryPtr, StringType);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}