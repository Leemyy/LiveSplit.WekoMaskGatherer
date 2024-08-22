using System;
using System.Linq;

namespace LiveSplit.WekoMaskGatherer {
    public partial class WekoMaskGathererComponent {

        private const string BossPrefix = "Boss";
        private const string ItemPrefix = "Item";
        private const string MapPrefix = "Map";

        private readonly RemainingDictionary _remainingSplits;

        public override bool Update() {
            return memory.Update();
        }

        public override void OnStart() {
            _remainingSplits.Setup(settings.Splits);
            memory.ResetData();
        }


        public override bool Start() {
            return memory.NewGameStarted
                //&& memory.SaveData.New != IntPtr.Zero
                && memory.State.Old == WekoState.Cutsceneing
                && memory.State.New != WekoState.Cutsceneing;
        }

        public override bool Reset() {
            return memory.MapJustChanged
                && memory.CurrentMap != MapName.MainMenu
                && memory.PreviousMap == MapName.MainMenu
                && memory.SaveData.New == IntPtr.Zero;
        }

        public override bool Loading() {
            if(!memory.IsGameRunning) {
                return true;
            }

            var map = memory.CurrentMap;
            if(map is null or MapName.Startup) {
                return true;
            }

            if(map is MapName.MainMenu) {
                return memory.Weko.New != IntPtr.Zero
                    || memory.SaveData.New != IntPtr.Zero;
            }

            return memory.Weko.New == IntPtr.Zero
                || memory.InstructionHud.New == IntPtr.Zero;
        }

        public override bool Split() {
            return _remainingSplits.Count != 0 && SplitForItem();
        }

        private bool SplitForItem() {
            if(!_remainingSplits.ContainsKey(ItemPrefix)) {
                return false;
            }

            foreach(var tab in memory.Inventory) {
                foreach(var entry in tab.Slots()) {
                    if(entry.Amount < 1) {
                        continue;
                    }

                    var itemName = memory.ItemLookup[entry.ItemId];
                    if(_remainingSplits.Split(ItemPrefix, itemName)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}