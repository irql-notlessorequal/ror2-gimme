# Gimme

A simple Risk of Rain 2 mod that adds a set of commands to let you spawn in items.

## 🚨 README BEFORE INSTALLING 🚨

This mod intentionally disables the ability to gain achievements as it becomes REALLY easy to get them.

If you wish to get your achievements, **MAKE SURE THAT THIS MOD IS DISABLED**.

# Usage

```
/gi [ITEM] [PLAYER] {QUANTITY}
/gr {ITEM} {QUANTITY}
/gimme [ITEM] [PLAYER] {QUANTITY}
/gimmerandom  {ITEM} {QUANTITY}
```

where:
- `[ITEM]` is a single word (no spaces!!!), you can use parts of an item name like "Syringe" for "Soldier's Syringe", case insenstive
- `[PLAYER]` for a player, not required when playing solo.
- `{QUANTITY}` the amount to give, a number between one and 1024, optional.

As of version 1.0.1, the argument parser is more flexible.

The following combos are now permitted:
```
/gi Tougher 255 <-- Gives 255 Tougher Times to self (solo only)
/gi Tougher 21 John <-- Gives 21 Tougher Times to player "John"
/gi Tougher John 21 <-- Same as above.
```

# FAQ

## Why?

funny

## Why do I see "Too much of item requested"...

Some items make the game either way too unplayable or will lag the hell out of the host, so a very small
pool of items have a fixed limit within Gimme, however you can still obtain more of these items normally through
the game.

## Why isn't this mod host only?

The achievement blocker wouldn't work then, see above.

## Can I spawn DLC items in without owning the DLC?

No.

(the game prevents doing so meaning I don't have to code a DLC check)

## Can you give other players (in your lobby) items?

Yes.

This is intentional :-)

## Why can't I spawn in some items?

For some reason, player equipment isn't available in `ItemCatalog.allItems`.

I may fix this in the future.

There are some other items that are not available because they are internal items and their functionality is not known.

# Credits

- [DropItem](https://thunderstore.io/package/Thrayonlosa/DropItem/) by Thrayonlosa, basis for the mod.