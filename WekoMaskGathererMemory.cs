using System;
using System.Collections.Generic;
using System.Linq;
using Voxif.AutoSplitter;
using Voxif.Helpers.Unity;
using Voxif.Helpers.MemoryHelper;
using Voxif.IO;
using Voxif.Memory;
using LiveSplit.ComponentUtil;

namespace LiveSplit.WekoMaskGatherer;
public class WekoMaskGathererMemory : Memory {

    protected override string[] ProcessNames => new string[] { "WekoProject-Win64-Shipping" };

    public bool IsGameRunning { get; private set; }

    private StringPointer Map { get; set; }
    public string CurrentMap { get; private set; } = "";
    public bool IsInMainMenu { get; private set; }
    public bool WasInMainMenu { get; private set; }
    private bool _lastMapWasMainMenu;
    public bool ComingFromMainMenu { get; private set; }

    public Pointer<IntPtr> Weko { get; private set; }
    public Pointer<IntPtr> InstructionHud { get; private set; }
    public Pointer<IntPtr> SaveData { get; private set; }


    private readonly Dictionary<string, int> trackers = new Dictionary<string, int>();

    public WekoMaskGathererMemory(Logger logger) : base(logger) {
        OnHook += Init;

        //OnExit += () => { };
    }

    private void Init() {
        CurrentMap = "";
        IsInMainMenu = false;
        WasInMainMenu = false;
        _lastMapWasMainMenu = false;
        ComingFromMainMenu = false;

        NestedPointerFactory ptrFactory = new NestedPointerFactory(game);

        var engine = ptrFactory.Make<IntPtr>(0x04DD2FD8);
        var gameViewport = ptrFactory.Make<IntPtr>(engine, 0x780);
        var world = ptrFactory.Make<IntPtr>(gameViewport, 0x78);
        var mapName = Map = ptrFactory.MakeString(world, 0x4A8, 0x0);
        mapName.StringType = EStringType.UTF16;
        var gameState = ptrFactory.Make<IntPtr>(world, 0x120);
        var persistentLevel = ptrFactory.Make<IntPtr>(world, 0x30);
        var worldSettings = ptrFactory.Make<IntPtr>(persistentLevel, 0x258);

        var gameInstance = ptrFactory.Make<IntPtr>(engine, 0xD28);
        var localPlayer0sPlayerController = ptrFactory.Make<IntPtr>(gameInstance, 0x38, 0x0, 0x30);
        var quickTravelDestination = ptrFactory.MakeString(gameInstance, 0x1B0, 0x0);
        quickTravelDestination.StringType = EStringType.UTF16;
        var doorId = ptrFactory.Make<ulong>(gameInstance, 0x1C0);
        var saveData = SaveData = ptrFactory.Make<IntPtr>(gameInstance, 0x1D0);
        var seenFinalBoss = ptrFactory.Make<bool>(gameInstance, 0x220);

        var weko = Weko = ptrFactory.Make<IntPtr>(gameInstance, 0x1C8);
        var rootComponent = ptrFactory.Make<IntPtr>(weko, 0x130);
        var wekoX = ptrFactory.Make<float>(rootComponent, 0x1D0);
        var wekoY = ptrFactory.Make<float>(rootComponent, 0x1D4);
        var wekoZ = ptrFactory.Make<float>(rootComponent, 0x1D8);
        var wekoHealth = ptrFactory.Make<float>(weko, 0x6A8);
        var wekoCurrency = ptrFactory.Make<int>(weko, 0x810);
        var doorInteractionComponent = ptrFactory.Make<IntPtr>(weko, 0x588);
        var hud = ptrFactory.Make<IntPtr>(weko, 0x718);
        var instructionHud = InstructionHud = ptrFactory.Make<IntPtr>(hud, 0x328);
        var saveWidget = ptrFactory.Make<IntPtr>(hud, 0x338);
        var wekoState = ptrFactory.Make<WekoState>(weko, 0x720);
        var currentMapName = ptrFactory.MakeString(weko, 0x800, 0x0);
        currentMapName.StringType = EStringType.UTF16;
        var progressEventAchieved = ptrFactory.Make<IntPtr>(weko, 0x8C0);
        var unlockedQuickTravelDestinations = ptrFactory.MakeStringArray(game, weko, 0x980);
        unlockedQuickTravelDestinations.StringType = EStringType.UTF16;
        var currentBoss = ptrFactory.Make<IntPtr>(weko, 0xB10);
        var currentBossHealth = ptrFactory.Make<int>(currentBoss, 0x5A8);

        var inventoryTab = ptrFactory.Make<int>(weko, 0x950);
        var inventory = ptrFactory.Make<IntPtr>(weko, 0x738);
        var inventoryRightMask = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x238);
        var inventoryLeftMask = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x248);
        var inventoryCenterMask = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x258);
        var inventoryWeapon = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x268);
        var inventoryRareItem = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x278);
        var inventoryTunic = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x288);
        var inventoryCollectable = ptrFactory.MakeArray<InventorySlot>(game, inventory, 0x298);

        var equipment = ptrFactory.MakeArray<InventorySlot>(game, weko, 0x740);
        var itemLookup = ptrFactory.Make<IntPtr>(weko, 0x740, 0x0, 0x0, 0x0);

        logger.Log(ptrFactory.ToString());
    }

    public override bool Update() {
        var attached = base.Update();
        if(!attached) {
            IsGameRunning = false;
            return false;
        }

        IsGameRunning = true;

        WasInMainMenu = IsInMainMenu;
        var map = Map.New;
        if(!string.IsNullOrEmpty(map) && map.StartsWith(MapName.Prefix)) {
            CurrentMap = map = map.Substring(MapName.Prefix.Length);
            bool inMainMenu = map == MapName.MainMenu;
            ComingFromMainMenu = !inMainMenu && _lastMapWasMainMenu;
            IsInMainMenu = inMainMenu;
            _lastMapWasMainMenu = inMainMenu;
        } else {
            CurrentMap = "";
            IsInMainMenu = false;
        }

        return true;
    }

    public void ResetData() {
        //trackers.Clear();
    }

    // public IEnumerable<string> NewTrackerSequence(bool useSavedData = true) {
    //     if(ShowTitleScreen.New) {
    //         yield break;
    //     }
    //     int count = game.Read<int>(Trackers.New + 0x20);
    //     IntPtr entries = game.Read<IntPtr>(Trackers.New + 0x18);
    //     for(int id = 0; id < count; id++) {
    //         IntPtr entry = entries + 0x28 + 0x18 * id;
    //         string key = game.ReadString(game.Read(entry, 0x0, 0x14), EStringType.UTF16Sized);
    //         int value = game.Read<int>(game.Read(entry, 0x8, 0x10));
    //         if(useSavedData) {
    //             if(trackers.ContainsKey(key)) {
    //                 if(trackers[key] != value) {
    //                     trackers[key] = value;
    //                     yield return key + "_" + value;
    //                 }
    //             } else {
    //                 trackers.Add(key, value);
    //                 yield return key + "_" + value;
    //             }
    //         } else {
    //             yield return key + "_" + value;
    //         }
    //     }
    // }

    // public string GetEpisode() {
    //     foreach(string name in NewTrackerSequence(false)) {
    //         if(name.StartsWith("episode_")) {
    //             return name;
    //         }
    //     }
    //     return String.Empty;
    // }
}
