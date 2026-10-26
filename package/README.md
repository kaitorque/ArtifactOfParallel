# ArtifactOfParallel

An artifact that splits a stage's chests, Shrines of Chance, multishops and similar loot between players. Each player only sees and opens their own; anything that drops is shared. The stage's total loot stays the same as a normal multiplayer run.

Everyone in the lobby needs this mod, with the same version.

Work in progress: the artifact can be toggled in the lobby but doesn't change anything yet.

## Config (`kaitorque.ArtifactOfParallel.cfg`)

| Section | Setting | Default | Description |
|---|---|---|---|
| Private | Chests, EquipmentBarrels, LunarPods, VoidCradles, ShrineOfChance, Multishops | true | Which interactables are split between players |
| Private | Printers, Scrappers, Drones | false | Same, for interactables that are shared by default |
| Debug | LogSpawns | false | Log each stage's loot budget and who got which object |
