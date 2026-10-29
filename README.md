# ArtifactOfParallel

An artifact that splits a stage's chests, Shrines of Chance, multishops and similar loot between players. Each player only sees and opens their own; anything that drops out is shared. Added together, everyone's share is about what the stage would have in a normal multiplayer run.

Everyone in the lobby needs this mod, with the same version.

See [CHANGELOG.md](package/CHANGELOG.md) for version history.

## How it works

- At the start of each stage, the game places loot as usual. The artifact then hands each private object to one player, so everyone gets roughly the same total value. Objects that would push someone well over their share are removed.
- Stage-placed objects (like the legendary chest on Abyssal Depths) are copied once per player when that fits the budget; otherwise they stay shared.
- Rusted Key lockboxes, Encrusted Key caches and Shipping Request Form chests belong to the player whose item spawned them.
- If a player dies or disconnects, their unopened objects become shared until they come back (revive, reconnect, or take over a Remote Operation drone).
- A Remote Operation drone can open its player's own private objects.
- Drifter can't grab, throw or Tinker another player's private objects.
- Printers, scrappers, drones, Void Seed contents and everything else stay shared.
- With one player the artifact does nothing.

## Config (`kaitorque.ArtifactOfParallel.cfg`)

The host's settings apply, read at the start of each stage. With [Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) installed, they can be changed in game.

| Section | Setting | Default | Description |
|---|---|---|---|
| Private | Chests, EquipmentBarrels, LunarPods, VoidCradles, ShrineOfChance, Multishops | true | Which interactables are split between players. Chests covers small, large, legendary, category and adaptive chests. |
| Private | ItemBoxes | true | Item-spawned boxes belong to the item's holder |
| Debug | Logging | false | Log the loot split, ownership changes and interaction checks |

## Console commands (for testing)

- `parallel_test_chests` (host): spawns one small chest per player in front of you.
- `parallel_debug`: lists private objects on your game and their owners in the BepInEx log.

## Compatibility

> **Other artifacts and loot mods:** Parallel has only been tested on its own and with Artifact of Potential. Other artifacts or mods that change chests, shrines or shops, or how a stage spawns them (for example Sacrifice, Command, Delusion, or mods that add their own chests), haven't been tested with it. They may break the split: objects staying shared, a player getting too much or too little, or someone seeing another player's object. If you hit a problem, try without them first, and report it with your log.

- **MapChests:** highlights and minimap icons only show your own private objects. Other highlight or minimap mods may still show other players' objects on the host; mods can use `ArtifactOfParallel.ParallelApi` to check visibility.
- Players joining mid-stage (DropinMultiplayer) get a share from the next stage.
- **TorqueFixes** (optional, host only) fixes two base-game bugs found while testing this mod: a host hang with Heretic on a Remote Operation drone, and constant lag from Gorag's Opus with Gesture of the Drowned or on an Equipment Drone.

## Known issues

- On the host, ambient sounds of other players' hidden objects may still play.
- Pinging where another player's object is shows the ping marker (but not the object).

## Building

Requires the .NET SDK. Paths to the game and the r2modman profile are set in `ArtifactOfParallel.csproj` (`GameManaged` and `Profile`).

```powershell
.\package.ps1
```

This builds the plugin and creates a Thunderstore/r2modman-importable zip next to the repo folder. Add `-Install` to also copy it into the r2modman profile.

## License

[MIT](LICENSE)
