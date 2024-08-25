namespace LiveSplit.WekoMaskGatherer;

public enum WekoState : byte {
    None = 0,
    FadingIn = 1, //Not sure
    Sprinting = 2,
    Rolling = 3,
    Attacking = 4,
    JumpSlash = 5,
    Blocking = 6,
    Dying = 8,
    Swimming = 9,
    Riding = 13,
    Talking = 16,
    Inventory = 17,
    Gliding = 19,
    QuickTravelling = 21,
    Cutsceneing = 22,
    KnockedDown = 24,
}