using System;
using System.Collections.Generic;
using System.Linq;
using Voxif.IO;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer {
    public partial class WekoMaskGathererComponent {

        private const string BossPrefix = "Boss";
        private const string ItemPrefix = "Item";
        private const string QuickTravelPrefix = "Travel";
        private const string MapPrefix = "Map";
        private const string DoorPrefix = "Door";

        private const string CutsceneActor = "VideoPlayer_BP_C";

        private readonly HashSet<(string, string)> _completedSplits;
        private readonly RemainingDictionary _remainingSplits;
        private HashSet<FNameEntryId> _seenActors;

        private bool _waitingForActors;
        private bool _checkForCutscene;
        private bool _cutsceneStarted;
        private IntPtr _cutsceneActor;
        private bool _arrivingAtMap;

        public override bool Update() {
            bool attached = _memory.Update();
            if (!attached) {
                _seenActors.Clear();
                _waitingForActors = true;
                _checkForCutscene = false;
                _cutsceneStarted = false;
                _cutsceneActor = IntPtr.Zero;
                _arrivingAtMap = false;
                return false;
            }

            // Check for cutscene actor
            if (_memory.MapJustChanged) {
                _seenActors.Clear();
                _waitingForActors = true;
                _checkForCutscene = false;
                _cutsceneStarted = false;
                _cutsceneActor = IntPtr.Zero;
                _arrivingAtMap = _memory.CurrentMap != MapName.MainMenu;
            }

            if (_waitingForActors &&
                _memory.Actors.New.DataReference != IntPtr.Zero &&
                _memory.Actors.New.Count > 0
            ) {
                _checkForCutscene = true;
            }

            if (_checkForCutscene) {
                _waitingForActors = false;
                var lookup = _memory.NameLookup;
                int i = -1;
                foreach (var actor in _memory.Actors.Slots()) {
                    i++;
                    var actorFName = _memory.Game.Read<FNameEntryId>(actor, 0x18);
                    if (!_seenActors.Add(actorFName)) continue;
                    var actorName = lookup.FindString(actorFName);
                    if (actorName == CutsceneActor) {
                        logger.Log($"Found cutscene actor {actorFName} @{(ulong)actor:X16}");
                        _cutsceneActor = actor;
                        _cutsceneStarted = true;
                        _checkForCutscene = false;
                    }
                }
            } else if (_cutsceneActor != IntPtr.Zero) {
                var actorFName = _memory.Game.Read<FNameEntryId>(_cutsceneActor, 0x18);
                var actorName = _memory.NameLookup.FindString(actorFName);
                if (actorName != CutsceneActor) {
                    _cutsceneActor = IntPtr.Zero;
                    _checkForCutscene = true;
                }
            }

            //ToDo: Check for state QuickTravelling to also catch quick travels within a map.
            var state = _memory.State.New;
            if (_arrivingAtMap &&
                _memory.InstructionHud.New != IntPtr.Zero &&
                state is not (WekoState.FadingIn or WekoState.Cutsceneing)
            ) {
                logger.Log("Fully Arrived at Map");
                _arrivingAtMap = false;
            }

            return true;
        }

        public override void OnStart() {
            _completedSplits.Clear();
            _remainingSplits.Setup(settings.Splits);
            _memory.ResetData();
        }


        public override bool Start() {
            return _memory.NewGameStarted
                && _cutsceneStarted
                && _memory.State.Old == WekoState.Cutsceneing
                && _memory.State.New != WekoState.Cutsceneing;
        }

        public override bool Reset() {
            return _memory.MapJustChanged
                && _memory.CurrentMap != MapName.MainMenu
                && _memory.PreviousMap == MapName.MainMenu
                && _memory.SaveData.New == IntPtr.Zero;
        }

        public override bool Loading() {
            if (!_memory.IsGameRunning) {
                return true;
            }

            var map = _memory.CurrentMap;
            if (map is null or MapName.Startup) {
                return true;
            }

            if (map is MapName.MainMenu) {
                return _memory.Weko.New != IntPtr.Zero
                    || _memory.SaveData.New != IntPtr.Zero;
            }

            if (_cutsceneActor != IntPtr.Zero) {
                var cutsceneWidget = _memory.Game.Read<IntPtr>(_cutsceneActor + 0x248);
                if (cutsceneWidget != IntPtr.Zero) {
                    return true;
                }
            }

            return _memory.Weko.New == IntPtr.Zero
                || _memory.InstructionHud.New == IntPtr.Zero;
        }

        public override bool Split() {
            return _remainingSplits.Count != 0 &&
                SplitForMap() ||
                SplitForDoor() ||
                SplitForBoss() ||
                SplitForTravelDestination() ||
                SplitForItem();
        }

        private bool SplitForBoss() {
            // if (!_remainingSplits.ContainsKey(BossPrefix)) {
            //     return false;
            // }

            var boss = _memory.BossName.New;
            if (boss is { Chunk: 0, Offset: 0 }) {
                return false;
            }

            var bossName = _memory.NameLookup.FindString(boss);
            var bossStart = bossName + "_start";
            if (//_completedSplits.Add(bossStart) &&
                //_remainingSplits.Split(BossPrefix, bossStart)
                ShouldSplit(BossPrefix, bossStart)
            ) {
                return true;
            }

            var bossHealth = _memory.BossHealth.New;
            var bossDefeat = bossName + "_defeat";
            if (bossHealth <= 0 &&
                // _completedSplits.Add(bossDefeat) &&
                // _remainingSplits.Split(BossPrefix, bossDefeat)
                ShouldSplit(BossPrefix, bossDefeat)
            ) {
                return true;
            }

            return false;
        }

        private bool SplitForItem() {
            // if (!_remainingSplits.ContainsKey(ItemPrefix)) {
            //     return false;
            // }

            foreach(var tab in _memory.Inventory) {
                foreach(var item in tab.Slots()) {
                    if (item.Amount < 1) continue;

                    var itemName = _memory.NameLookup.FindString(item.ItemId);
                    if (ShouldSplit(ItemPrefix, itemName)) {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SplitForTravelDestination() {
            // if (!_remainingSplits.ContainsKey(QuickTravelPrefix)) {
            //     return false;
            // }

            var destsPtr = _memory.UnlockedTravelDestinations;
            foreach(var dest in destsPtr.Slots()) {
                if (dest is not null &&
                    ShouldSplit(QuickTravelPrefix, dest)
                ) {
                    return true;
                }
            }

            return false;
        }

        private bool SplitForMap() {
            if (!_memory.MapJustChanged) {
                return false;
            }
            // if (!_remainingSplits.ContainsKey(MapPrefix)) {
            //     return false;
            // }

            var currentMap = _memory.CurrentMap;
            if (currentMap is not null &&
                ShouldSplit(MapPrefix, currentMap)
            ) {
                return true;
            }

            return false;
        }

        private bool SplitForDoor() {
            if (!_arrivingAtMap) {
                return false;
            }
            // if (!_remainingSplits.ContainsKey(DoorPrefix)) {
            //     return false;
            // }

            var door = _memory.Door.New;
            var doorName = _memory.NameLookup.FindString(door);
            if (ShouldSplit(DoorPrefix, doorName)) {
                return true;
            }

            return false;
        }

        private bool ShouldSplit(string category, string split) {
            if (_completedSplits.Add((category, split))) {
                return _remainingSplits.Split(category, split);
            }

            return false;
        }
    }
}