using System;

namespace LiveSplit.WekoMaskGatherer;

public struct InventorySlot {
    public IntPtr ItemLookup;
    public long ItemId;
    public int Amount;
    private int Padding;
}