using System;
using System.Collections.Generic;
using Voxif.AutoSplitter;
using Voxif.Helpers.Unity;
using Voxif.Helpers.MemoryHelper;
using Voxif.IO;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;
public partial class WekoMaskGathererMemory : Memory {

    protected override string[] ProcessNames => [ "WekoProject-Win64-Shipping" ];

    public bool IsGameRunning { get; private set; }
    public ProcessWrapper Game => game;

    public FNameLookup NameLookup { get; private set; }

    private StringPointer Map { get; set; }
    public ArrayPointer<IntPtr> Actors { get; private set; }
    public string? CurrentMap { get; private set; }
    private string _lastValidMap = MapName.Startup;
    public string PreviousMap { get; private set; } = MapName.Startup;
    public bool MapJustChanged { get; private set; }

    public bool NewGameStarted { get; private set; }

    public Pointer<FNameEntryId> Door { get; private set; }
    public Pointer<IntPtr> Weko { get; private set; }
    public Pointer<WekoState> State { get; private set; }
    public Pointer<IntPtr> InstructionHud { get; private set; }
    public StringArrayPointer UnlockedTravelDestinations { get; private set; }
    public Pointer<FNameEntryId> BossName { get; private set; }
    public Pointer<int> BossHealth { get; private set; }
    public ArrayPointer<ProgressEvent> ProgressEvents { get; private set; }
    public Pointer<IntPtr> SaveData { get; private set; }

    public ArrayPointer<InventorySlot> Equipment { get; private set; }
    public ArrayPointer<InventorySlot>[] Inventory { get; private set; } =
        Array.Empty<ArrayPointer<InventorySlot>>();
    public Pointer<int> InventoryTab { get; private set; }

    private bool _itemLookupInitialized;
    public Dictionary<FNameEntryId, string> ItemLookup { get; private set; }
    public Pointer<IntPtr> FNameTable { get; private set; }


    private readonly Dictionary<string, int> trackers = new Dictionary<string, int>();

    public WekoMaskGathererMemory(Logger logger) : base(logger) {
        OnHook += Init;

        NameLookup = new FNameLookup(new FNameTableHandle(this));

        //OnExit += () => { };
    }

    private void Init() {
        CurrentMap = null;
        _lastValidMap = MapName.Startup;
        PreviousMap = MapName.Startup;

        var ptrFactory = new NestedPointerFactory(game);

        try {
            var engine = ptrFactory.Make<IntPtr>(0x04DD2FD8);
            var gameViewport = ptrFactory.Make<IntPtr>(engine, 0x780);
            var world = ptrFactory.Make<IntPtr>(gameViewport, 0x78);
            var mapName = Map = ptrFactory.MakeString(world, 0x4A8, 0x0);
            mapName.StringType = EStringType.UTF16;
            var gameState = ptrFactory.Make<IntPtr>(world, 0x120);
            var persistentLevel = ptrFactory.Make<IntPtr>(world, 0x30);
            // This post was really helpful in figuring out where to look for actors
            // Seems like an ultra-dodgy website, tho. So browse with care.
            // https://www.unknowncheats.me/forum/unreal-engine-4-a/507230-ue4-cheat-sheet.html
            var actors = Actors = ptrFactory.MakeArray<IntPtr>(game, persistentLevel, 0x98);
            var worldSettings = ptrFactory.Make<IntPtr>(persistentLevel, 0x258);

            var gameInstance = ptrFactory.Make<IntPtr>(engine, 0xD28);
            var localPlayer0sPlayerController = ptrFactory.Make<IntPtr>(gameInstance, 0x38, 0x0, 0x30);
            var quickTravelDestination = ptrFactory.MakeString(gameInstance, 0x1B0, 0x0);
            quickTravelDestination.StringType = EStringType.UTF16;
            var doorId = Door = ptrFactory.Make<FNameEntryId>(gameInstance, 0x1C0);
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
            var saveWidgetActive = ptrFactory.Make<int>(saveWidget, 0x1A8);
            var wekoState = State = ptrFactory.Make<WekoState>(weko, 0x720);
            var currentMapName = ptrFactory.MakeString(weko, 0x800, 0x0);
            currentMapName.StringType = EStringType.UTF16;
            var progressEvents = ProgressEvents = ptrFactory.MakeArray<ProgressEvent>(game, weko, 0x8C0);
            var unlockedQuickTravelDestinations = UnlockedTravelDestinations = ptrFactory.MakeStringArray(game, weko, 0x980);
            unlockedQuickTravelDestinations.StringType = EStringType.UTF16;
            var currentBoss = ptrFactory.Make<IntPtr>(weko, 0xB10);
            var currentBossName = BossName = ptrFactory.Make<FNameEntryId>(currentBoss, 0x18);
            var currentBossHealth = BossHealth = ptrFactory.Make<int>(currentBoss, 0x5A8);

            var inventoryTab = InventoryTab = ptrFactory.Make<int>(weko, 0x950);
            var inventoryBackpack = ptrFactory.Make<IntPtr>(weko, 0x738);
            var inventory = new ArrayPointer<InventorySlot>[7];
            var inventoryRightMask = inventory[2] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x238);
            var inventoryLeftMask = inventory[1] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x248);
            var inventoryCenterMask =
                inventory[0] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x258);
            var inventoryWeapon = inventory[3] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x268);
            var inventoryRareItem = inventory[4] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x278);
            var inventoryTunic = inventory[6] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x288);
            var inventoryCollectable =
                inventory[5] = ptrFactory.MakeArray<InventorySlot>(game, inventoryBackpack, 0x298);
            Inventory = inventory;

            var equipment = Equipment = ptrFactory.MakeArray<InventorySlot>(game, weko, 0x740);

            var fNameTable = FNameTable = ptrFactory.Make<IntPtr>(0x04BB4F80);
        } catch (Exception e) {
            logger.Log("Error initializing pointers:");
            logger.Log(e);
        }

        logger.Log("Pointers Initialized:");
        logger.Log(ptrFactory.ToString());
        _itemLookupInitialized = false;
    }

    public override bool Update() {
        var attached = base.Update();
        if(!attached) {
            if(IsGameRunning) {
                logger.Log("Game Lost!");
                NameLookup.Reset();
            }
            IsGameRunning = false;
            return false;
        }

        if(!IsGameRunning) {
            logger.Log("Game Detected!");
        }
        IsGameRunning = true;

        UpdateMap();
        if(MapJustChanged) {
            NewGameStarted = PreviousMap == MapName.MainMenu && SaveData.New == IntPtr.Zero;
            if(NewGameStarted) {
                logger.Log("New Game Started!");
            }
        }

        return true;
    }

    private void UpdateMap() {
        bool mapJustChanged = false;
        var map = Map.New;
        if(!string.IsNullOrEmpty(map) && map.StartsWith(MapName.Prefix)) {
            map = map.Substring(MapName.Prefix.Length);
            if(_lastValidMap != map && PreviousMap != _lastValidMap) {
                logger.Log($"Map changed to: '{map}'");
                mapJustChanged = true;
                PreviousMap = _lastValidMap;
            }

            CurrentMap = _lastValidMap = map;
        } else {
            CurrentMap = null;
        }

        MapJustChanged = mapJustChanged;
    }

    public void ResetData() {
        trackers.Clear();
    }
}
