using System;

namespace LiveSplit.WekoMaskGatherer;

public struct LookupEntry {
    public long ItemId;
    public IntPtr ItemData;
    public int Next;
    public int Hash;
}