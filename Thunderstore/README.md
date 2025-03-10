# Gimme

A simple Risk of Rain 2 mod that adds a command to let you spawn in items.

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

As of version 1.0.1, the argument parser is much more flexible.
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

Some items either make the game way too unplayable, or will lag the hell out of the host, so a very small
pool of items have a fixed limit within Gimme, you can still obtain more of these items normally through
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

There are some other items are not available since they are internal items and their functionality is not known.

# Credits

- [DropItem](https://thunderstore.io/package/Thrayonlosa/DropItem/) by Thrayonlosa, basis for the mod.

# License

```
MIT License

Copyright (c) 2025 IRQL_NOT_LESS_OR_EQUAL

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```