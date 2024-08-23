using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;

public interface GameAddressHandle<T> where T : unmanaged {
    public Pointer<T> Address { get; }
    public ProcessWrapper Game { get; }
    public bool IsGameRunning { get; }
}