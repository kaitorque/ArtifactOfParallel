# Changelog

## 1.0.1

- Fixed: on clients, a stage-placed chest that was copied per player (like the Large Chest on top of the ruins in Verdant Falls) left its original model behind. Clients saw an extra closed chest that nobody could open.
- Fixed: those per-player copies cost more than vanilla because the difficulty scaling was applied twice.

## 1.0.0

First full release.

- Fixed: adaptive chests are actually private now. 0.1.2 used a wrong asset key, so they stayed shared.
- README: added a compatibility note that other artifacts and loot mods haven't been tested with Parallel and may break the split.
- Removed the base-game Heretic + Remote Operation fix added in 0.1.2. This mod no longer changes base-game behaviour outside the artifact.

## 0.1.2

- Adaptive chests are now private (under the Chests setting); they were missing from the list and stayed shared.
- Fixed a base-game hang: a player with all four Heretic items (e.g. joining as Heretic with DropinMultiplayer) on a Remote Operation drone doubled a list on their character with every item picked up, until the host froze and crashed.

## 0.1.1

- Fixed: on the host, a chest, lunar pod or lockbox that went back to its owner (after they revived or took over a Remote Operation drone) stayed visible to others but couldn't be opened. Their models live outside the object and weren't hidden again.
- Debug logging: each stage lists everything the director spawned by type (shared and private, also with the artifact off for comparison), and which objects each player received or were removed.

## 0.1.0

First test version.

- Artifact of Parallel: splits chests, equipment barrels, lunar pods, void cradles, Shrines of Chance and multishops between players. Each player only sees and opens their own, and drops are shared.
- The split keeps each stage's total loot value about the same as vanilla multiplayer, and is seeded by the run so it's repeatable.
- Stage-placed objects are copied per player when they fit the budget, otherwise left shared.
- Rusted Key, Encrusted Key and Shipping Request Form boxes belong to the item's holder.
- A dead or disconnected player's unopened objects become shared, and go back to them on revival, reconnect or Remote Operation.
- Remote Operation drones can open their player's own objects; Drifter can't grab or Tinker other players' objects.
- On the host, other players' objects are fully hidden (models, lights, price labels, ping outlines).
- Per-category config, with an optional Risk Of Options page.
- `ParallelApi` for other mods (highlight or minimap mods) to check what the local player can see.
- Debug logging and the `parallel_test_chests` / `parallel_debug` console commands.
