﻿using System;

namespace LiveSplit.WekoMaskGatherer;

//We need the unused fields to match the alignment of in-game fields.
#pragma warning disable CS0169 // private field is never used
public struct InventorySlot {
    public IntPtr ItemLookup;
    public FNameEntryId ItemId;
    private int _padding;
    public int Amount;
    private int _paddingA;

    public override string ToString() => $"{{{ItemId} x{Amount}}}";
}