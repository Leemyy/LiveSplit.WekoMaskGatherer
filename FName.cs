namespace LiveSplit.WekoMaskGatherer;

public struct FNameEntryId {
    public ushort Offset;
    public ushort Table;

    public override string ToString() => $"{Table:X4}-{Offset:X4}";
}