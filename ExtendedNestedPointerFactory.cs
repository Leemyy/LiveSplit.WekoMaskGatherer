using System;
using System.Collections.Generic;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;

public static class NestedPointerFactoryExtensions {


    // public ArrayPointer<T> MakeArray<T>(IntPtr basePtr, params int[] offsets)
    //     where T : unmanaged
    // {
    //     return this.MakeArray<T>(this.defaultDerefType, basePtr, offsets);
    // }
    //
    // public ArrayPointer<T> MakeArray<T>(EDerefType derefType, IntPtr basePtr, params int[] offsets)
    //     where T : unmanaged
    // {
    //     StringPointer stringPointer = (StringPointer) this.Make(typeof (string), derefType, basePtr, offsets);
    //     string str = stringPointer.New;
    //     return stringPointer;
    // }
    //
    // public StringArrayPointer MakeStringArray(IntPtr basePtr, params int[] offsets)
    // {
    //     return this.MakeStringArray(this.defaultDerefType, basePtr, offsets);
    // }
    //
    // public StringArrayPointer MakeStringArray(EDerefType derefType, IntPtr basePtr, params int[] offsets)
    // {
    //     StringPointer stringPointer = (StringPointer) this.Make(typeof (string), derefType, basePtr, offsets);
    //     string str = stringPointer.New;
    //     return stringPointer;
    // }

    public static ArrayPointer<T> MakeArray<T>(
        this NestedPointerFactory factory,
        TickableProcessWrapper wrapper,
        Pointer parent,
        params int[] offsets
    )
        where T : unmanaged
    {
        return new ArrayPointer<T>(factory.Make<ArrayBody>(parent, offsets), wrapper);
    }

    public static ArrayPointer<T> MakeArray<T>(
        this NestedPointerFactory factory,
        TickableProcessWrapper wrapper,
        EDerefType derefType,
        Pointer parent,
        params int[] offsets
    )
        where T : unmanaged
    {
        return new ArrayPointer<T>(factory.Make<ArrayBody>(derefType, parent, offsets), wrapper);
    }

    public static StringArrayPointer MakeStringArray(
        this NestedPointerFactory factory,
        TickableProcessWrapper wrapper,
        Pointer parent,
        params int[] offsets
    )
    {
        return new StringArrayPointer(factory.Make<ArrayBody>(parent, offsets), wrapper);
    }

    public static StringArrayPointer MakeStringArray(
        this NestedPointerFactory factory,
        TickableProcessWrapper wrapper,
        EDerefType derefType,
        Pointer parent,
        params int[] offsets
    )
    {
        return new StringArrayPointer(factory.Make<ArrayBody>(derefType, parent, offsets), wrapper);
    }
}