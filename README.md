# KindaRpgGame
JuegoDemoRPG
Overview
Top-down 2D action RPG built in Unity 6 (URP). The player fights through waves of goblins across an elevation-based map, unloREADMEcks bridges by reaching kill thresholds, defeats a boss, and opens a final chest to win.

Controls
Action	Input
Move	WASD / Left Stick
Light attack	Left Click / South Button
Heavy attack	Right Click / East Button
Heal	F
Pause	Escape
Core Loop
Enemies spawn from goblin buildings spread across the map.
Each kill increments the global kill counter.
Reaching kill milestones unlocks bridges (blocking walls disappear).
Once all bridges are open, the Boss spawns.
Defeating the Boss reveals the Victory Chest — collecting it ends the game.
Player Systems
Movement — Velocity-based, physics-driven. No root motion.
Attacks — Two melee combos (light / heavy). Each has its own damage, range, knockback, and movement lock duration. Hit detection fires via Animation Events using Physics2D.OverlapCircleAll.
Health — 5 HP max. Takes damage from melee and explosions. Death triggers the Defeat screen.
Healing — Press F to consume 1 Meat and restore 1 HP.
Resources — Collect Money, Meat, and Wood from enemy drops. Shown in the HUD inventory panel.
Enemies
Type	Behavior
Goblin Torch	Melee. Wanders, chases player, attacks on contact.
TNT Goblin	Ranged. Throws parabolic TNT projectiles when in range.
Boss	Orbits the player, throws TNT and Barrels in combat phases.
Enemies spawn from buildings with a short "exit walk" phase. On death they drop loot (Meat / Money Bag) and register a kill in the GameManager.

Map & Elevations
The map has two elevation levels. Stairs connect them. Entering/leaving elevated areas:

Swaps which tilemap colliders are active (wall vs. boundary).
Adjusts the player sprite's sorting order so it renders correctly above terrain.
UI
HUD — Kill counter, player health bar, boss status notification.
Inventory Panel — Resource counts with flash feedback on collection or insufficient funds.
Screens — Start, Pause, Victory, Defeat. All managed via Time.timeScale control.
Audio
AudioManager (singleton) handles all music and SFX. Music tracks support an intro clip followed by a seamless loop. Footstep sounds play automatically when the player's speed exceeds a threshold.

Project Structure
Assets/
├── Scripts/
│   ├── Combat/       # Health, Knockback, PlayerAttack, Chest
│   ├── Core/         # GameManager, GameScreenManager, AudioManager, BridgeController
│   ├── Enemy/        # Enemy AI, Boss, SpawnPoint, LootDrop, EnemyDeath
│   ├── Gameplay/     # PickupSpawn
│   ├── Player/       # Movement, Attack, Death, Healing, ResourceCollector, Camera
│   ├── Tilemap Scripts/ # ElevationsEntry, ElevationsExit
│   └── UI/           # GameUI, UIManager, InventoryPanel, EnemyHealthBar
├── Scenes/
│   └── Level1.unity
├── Animations/
├── Images/           # Tilesets, sprites (Tiny Swords asset pack)
└── Sounds/
