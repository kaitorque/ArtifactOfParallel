# Changelog

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
- `ParallelApi` for other mods (MapChests) to check what the local player can see.
- Debug logging and the `parallel_test_chests` / `parallel_debug` console commands.
