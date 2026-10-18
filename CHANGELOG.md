# Changelog — Rogue Legacy mods (unofficial)

Two separate mods are built from this repository: **Weapons** and **Co-op**.

## Weapons 1.0.0 — first release

- 3 Neon Abyss-style guns, used as spells by the Knight and the Mage:
  - **Pistol**: fast straight bullet (5 MP, 1.0× magic damage)
  - **Shotgun**: 5 pellets in a fan, random speed, short range (15 MP, 0.5× per pellet)
  - **SMG**: full auto while the spell button is held, small spread (3 MP per bullet, 0.35×)
- Gun icons, bullets and impact sparks from CC0 sprite packs (SnoopethDuckDuck, Master484).
- F9 cycles the current spell between the 3 guns.
- English and French texts.
- Own save slot (`RogueLegacyGunsStorageContainer`), seeded with a copy of your saves.

## Co-op 1.0.0 — first release

- Drop-in local 2-player co-op: START on a 2nd gamepad (or SELECT + START on the only pad) joins anywhere.
- Player 2 classes: Knight / Mage / Barbarian (SELECT in the starting camp); upgraded class powers if unlocked
  in the manor. Vanilla spells only.
- Shared Metal Slug-style camera with invisible screen walls; a player falling off-screen comes back after 2 s.
- Enemies target the nearest player; hits, crits, mana and kill rewards are credited to the attacker.
- Player 2 is knocked out for 8 s then revives at 50 % HP; Player 2 comes back automatically after Player 1's death.
- Player 2 is parked in single-player scripted rooms (bonus rooms, challenge bosses, ending…).
- Hold SELECT 3 s to leave. Player 2 is not saved.
- Own save slot (`RogueLegacyCoopStorageContainer`), seeded with a copy of your saves.

## Known issues (both mods)

- **Windows 64-bit only** (no Linux / Steam Deck build yet).
- Steam achievements are disabled (the mod runs without Steam).
- The executable is not signed: Windows SmartScreen may show "Windows protected your PC" on first launch
  → click **More info** > **Run anyway**.
- If you add the mod to Steam as a non-Steam game and a gamepad is not detected, disable **Steam Input** for that
  shortcut (Properties > Controller).

### Co-op only

- Only Player 1 can navigate the pause menu and the other menus.
- Chests, teleporters, boss doors, the map, NPCs and gold are handled by Player 1.
- Player 2 can change class only in the starting camp.
- An idle Player 2 standing on a screen edge blocks Player 1 (arcade rule): Player 2 can leave with SELECT (3 s).
- Online play is not built in: use Parsec (the remote gamepad becomes gamepad 2).
