# Smart Expert Delivery

[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Valiice/SmartExpertDelivery/total?style=flat-square&color=blue)](https://github.com/Valiice/SmartExpertDelivery/releases)
[![GitHub Release](https://img.shields.io/github/v/release/Valiice/SmartExpertDelivery?include_prereleases&style=flat-square&color=green)](https://github.com/Valiice/SmartExpertDelivery/releases/latest)
[![GitHub License](https://img.shields.io/github/license/Valiice/SmartExpertDelivery?style=flat-square&color=orange)](LICENSE.md)
[![GitHub Issues](https://img.shields.io/github/issues/Valiice/SmartExpertDelivery?style=flat-square)](https://github.com/Valiice/SmartExpertDelivery/issues)
[![Download Latest Zip](https://img.shields.io/badge/Download-Latest%20Zip-blue?style=flat-square)](https://github.com/Valiice/SmartExpertDelivery/releases/latest/download/SmartExpertDelivery.zip)

Expert delivery that keeps the gear you still need.

Handing in gear for Grand Company seals one item at a time is slow, and it is easy to give away a piece a lower level job was about to wear. Smart Expert Delivery adds a **Deliver** button to the Expert Delivery window. It hands in everything on the list except the gear you still have a use for.

## Installation

1. Add my custom plugin repository: [https://raw.githubusercontent.com/Valiice/DalamudPluginRepo/master/repo.json](https://github.com/Valiice/DalamudPluginRepo)
2. Search for **Smart Expert Delivery** in the Plugin Installer.
3. Install and Enable.

Not on the official repo. Alternatively, build with `dotnet build SmartExpertDelivery/SmartExpertDelivery.csproj -c Release` and load `SmartExpertDelivery/bin/Release/SmartExpertDelivery` as a dev plugin.

## Usage

1. Talk to your Grand Company's personnel officer and open the **Expert Delivery** tab.
2. A bar appears above the window. Hover **Deliver N items** to see what will be handed in, and hover **N kept** to see what stays and why.
3. Press **Deliver**. **Stop** ends the run at any time.

The cheapest items go first. The run stops by itself when the list is empty, when the next item would take you over the seal cap, or when an item will not hand in.

`/sed` opens the settings. With the Expert Delivery window closed, the settings window shows an estimate from your inventory and armoury chest.

## What is kept

Each rule can be turned on or off in `/sed`.

| Rule | Default | Keeps |
|------|---------|-------|
| Lower level jobs | On | Gear an unlocked job that can equip it has not reached the level of yet. |
| Also at a job's current level | Off | Level 50 gear while a job that can wear it is exactly level 50. |
| Only the best piece per slot | On | Delivers a piece the job rules would keep when you own a better item level piece for the same slot and equip level that the same jobs can wear. |
| Jobs I have not unlocked | Off | Gear for locked jobs, counted from the job's starting level. |
| Tomestone gear | Off | Gear bought with tomestones, and its augmented versions. |
| Gear with materia | On | Gear with materia melded. If one copy has materia, every copy of that item is kept. |
| Gear set pieces | On | Gear saved in any of your gear sets. |

## Limitations

- Only the Expert Delivery tab is handled. Supply and provisioning missions are left alone.
- The plugin reads the delivery list straight from the game's memory, so a game patch can break it until the plugin is updated. When the list cannot be read, the button does not appear.

## License & Copyright

Copyright © 2026 Valiice

Smart Expert Delivery is licensed under the GNU Affero General Public License v3.0; see [LICENSE](LICENSE.md) for details. You are free to use, modify, and redistribute the source code, provided derivative works keep the same license, credit this project with a link back to [https://github.com/Valiice/SmartExpertDelivery](https://github.com/Valiice/SmartExpertDelivery), and retain this copyright notice.
