# MoreCupboards

Allows you to buy more of the default cupboard for your ship (up to 5 more) as a simple solution for storage issues.

---

## WARNING: Experimental mod!

I've tested to remove any critical issues and made sure it works in the modpack I intend to use it in, but there may be some unintended side-effects if you mess around with things too much.

Some particular things to note:

- **MAJOR POTENTIAL ISSUE: It appears that MoreCupboards may be increasing the occurance of a bug on Company moons where players will fall through the floor alongside a console error similar to:**

`Infinity or NaN floating point numbers appear when calculating matrix for a Collider...`

This issue definitely isn't unique to MoreCupboards (it has been recreated without it installed at all), and the 1.3.0 patch should make things more stable overall, but it has been reported to still be occuring more frequently than usual with the latest version of MoreCupboards installed.

If you have any more details about this issue, or even if you don't have this issue at all, please let me know so I can narrow down what mods or other factors might be involved in causing it. I'll try to address this once I can more thoroughly test and recreate it.

- **[Matty Fixes](https://thunderstore.io/c/lethal-company/p/mattymatty/Matty_Fixes/) is highly recommended to avoid items falling through cupboards/not being parented to them**

(MoreCupboards will do a roughly equivalent fix to its own cupboards that Matty does to the vanilla cupboard when Matty Fixes is installed)

- **Any mod which makes major changes to the terminal or shop system may yield unexpected results or strange displays**

(compatibility with [mrov's TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter/) is included, though)

If you have these kinds of issues, you can try enabling the `Separate Cupboard Entries` debug config option, which will make each cupboard a separate unlockable.

- **All config options should be the same for all players in the game**

(this is fairly easy if you're using a custom modpack, since those can include config files)

- **Config options should not be changed in the middle of a save, in particular the maximum cupboards value**

(it shouldn't cause anything catastrophic to happen, but you might get some weird stuff like your unlocked suits disappearing or new unlockables randomly spawning)

- **If a cupboard is sent to storage with items still on it, those items can be found floating outside the ship on your current moon or the next moon you land at**

(not an ideal situation, I know, but you shouldn't need to do this in the first place)

- **If you encounter any types of errors with items, try turning off `Auto Parent` in the Debug config (and let me know)**

## Overview

![Cupboards](https://imgur.com/rnG6y4H.png)

You can buy up to 5 more cupboards of various colours for a fixed amount each (default is 300). This means you can have a total of 6 cupboards!

You'll find this listed in the "ship upgrades" section of the store (purchased by simply typing `cupboard`):

![TerminalStore](https://imgur.com/B2KtlPU.png)

The following can be adjusted in config:

- **Price per cupboard**

- **Maximum cupboards purchasable** (1-5)

- **Removal of doors from all cupboards, including the vanilla cupboard** (off by default)

- **Separation of cupboard entries, giving each cupboard its own keyword (`1cupboard`, `2cupboard`, and so on) instead of being all grouped under the single keyword `cupboard`** (off by default)

![DoorlessCupboard](https://imgur.com/K0v2bGf.png)

---

## Contact

Let me know about any suggestions or issues on the [GitHub](https://github.com/Science-Bird/MoreCupboards), or if you'd prefer you can mention it in my [Discord thread](https://discord.com/channels/1168655651455639582/1338282728918880399) (I'm "sciencebird" on Discord).
