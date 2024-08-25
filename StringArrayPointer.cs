using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;

public class StringArrayPointer
{
    private readonly ArrayPointer<StringHeader> _pointer;
    private readonly ProcessWrapper _wrapper;

    private struct StringHeader {
        public IntPtr Data;
        public int Size;
        public int Capacity;
    }

    public ArrayBody New {
        get => _pointer.New;
        set => _pointer.New = value;
    }

    public ArrayBody Old {
        get => _pointer.Old;
        set => _pointer.Old = value;
    }

    public bool UpdateOnNullPointer {
        get => _pointer.UpdateOnNullPointer;
        set => _pointer.UpdateOnNullPointer = value;
    }

    public EStringType StringType { get; set; }


    internal StringArrayPointer(Pointer<ArrayBody> array, ProcessWrapper wrapper) {
        _pointer = new ArrayPointer<StringHeader>(array, wrapper);
        _wrapper = wrapper;
    }

    public IEnumerable<string> Slots() {
        int count = New.Count;
        if (count == 0) return Enumerable.Empty<string>();
        if (count == 1) {
            var header = _wrapper.Read<StringHeader>(New.DataReference);
            // Null terminator is included in string size, so cut it off.
            var bytes = header.Size - 1;
            bytes *= (StringType is EStringType.UTF16 or EStringType.UTF16Sized ? 2 : 1);
            return new []{ count + _wrapper.ReadString(header.Data, bytes, StringType) };
        }
        return SlotsInternal();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IEnumerable<string> SlotsInternal() {
        foreach (var header in _pointer.Slots()) {
            var bytes = header.Size - 1;
            bytes *= (StringType is EStringType.UTF16 or EStringType.UTF16Sized ? 2 : 1);
            yield return _wrapper.ReadString(header.Data, bytes, StringType);
        }
    }
}