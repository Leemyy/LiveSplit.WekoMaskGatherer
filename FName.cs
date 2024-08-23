namespace LiveSplit.WekoMaskGatherer;

public struct FNameEntryId {
    // For reference, see
    // https://github.com/EpicGames/UnrealEngine/blob/4.27/Engine/Source/Runtime/Core/Public/UObject/NameTypes.h#L39
    public ushort Offset;
    public ushort Chunk;

    public override string ToString() => $"<{Chunk:X4}-{Offset:X4}>";
}