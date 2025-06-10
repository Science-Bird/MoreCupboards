# MoreCupboards

Allows you to buy more of the default cupboard for your ship (up to 5 more) as a simple solution for storage issues.

---

## Overview

**Note that [Matty Fixes](https://thunderstore.io/c/lethal-company/p/mattymatty/Matty_Fixes/) is highly recommended with this mod, as its fixes for the vanilla cupboard will also translate to the new cupboards added by this mod!**

![Cupboards](https://imgur.com/rnG6y4H.png)

You can buy up to 5 more cupboards of various colours for a fixed amount each. This means you can have a total of 6 cupboards!

You'll find this listed in the "ship upgrades" section of the store (purchased by simply typing `cupboard`):

![TerminalStore](https://imgur.com/B2KtlPU.png)

The following can be adjusted in config:

- **Price per cupboard** (default: 250)

- **Maximum cupboards purchasable** (1-5)

- **Removal of doors from all cupboards, including the vanilla cupboard** (off by default)

- **Separation of cupboard entries, giving each cupboard its own keyword (`1Cupboard`, `2Cupboard`, and so on) instead of being all grouped under the single keyword `Cupboard`** (off by default)

    - There's an additional option to have cupboards named by colour instead of by number (`Orange Cupboard`, `Yellow Cupboard`, etc.)

![DoorlessCupboard](https://imgur.com/K0v2bGf.png)

---

## Potential compatibility issues

- **Let me know if you encounter players falling through the floor followed by an error that looks like this:**

`Infinity or NaN floating point numbers appear when calculating matrix for a Collider...`

This isn't an issue with MoreCupboards itself, but it's some strange combination of things that can sometimes mess with shelves (like those in cupboards or the company desk). Any information about it would be helpful to narrow it down and help recreate it consistently.

- **If you've installed mods that change the terminal and the terminal isn't displaying cupboards correctly, try adjusting the terminal options in config**

(compatibility with [mrov's TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter/) is included)

- **Ensure players are using the same config and try to avoid changing config in the middle of a save file**

(changing config mid-save shouldn't cause anything catastrophic to happen, but you might get some weird stuff like your unlocked suits disappearing or new unlockables randomly spawning)

- **If you encounter any type of errors, try turning off `Auto Parent` in the Technical config (and let me know)**


## Contact

Let me know about any suggestions or issues on the [GitHub](https://github.com/Science-Bird/MoreCupboards), or if you'd prefer you can mention it in my [Discord thread](https://discord.com/channels/1168655651455639582/1350616165289951272) (I'm "sciencebird" on Discord).