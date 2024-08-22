using System;

namespace LiveSplit.WekoMaskGatherer;

//We need the unused fields to match the alignment of in-game fields.
#pragma warning disable CS0169 // private field is never used
public struct ItemEntry {
    public FNameEntryId ItemId;
    private int _padding;
    public IntPtr ItemData;
    public int Next;
    public int Hash;
}