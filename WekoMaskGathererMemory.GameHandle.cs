using System;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;

public partial class WekoMaskGathererMemory {
    private class FNameTableHandle : GameAddressHandle<IntPtr> {
        private WekoMaskGathererMemory _memory;

        public FNameTableHandle(WekoMaskGathererMemory memory) {
            _memory = memory;
        }

        public Pointer<IntPtr> Address => _memory.FNameTable;
        public ProcessWrapper Game => _memory.game;
        public bool IsGameRunning => _memory.IsGameRunning;
    }
}