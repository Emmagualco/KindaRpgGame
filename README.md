# 🎮 KindaRpgGame

**JuegoDemoRPG** - A top-down 2D action RPG built in Unity 6 (URP)

---

## 📋 Overview

A dynamic action RPG where players fight through waves of goblins across an elevation-based map. Unlock bridges by reaching kill thresholds, defeat the boss, and claim victory by collecting the treasure chest!

---

## 🎮 Controls

| Action | Input |
|--------|-------|
| **Move** | WASD / Left Stick |
| **Light Attack** | Left Click / South Button |
| **Heavy Attack** | Right Click / East Button |
| **Heal** | F |
| **Pause** | Escape |

---

## 🔄 Core Loop

1. 👹 Enemies spawn from goblin buildings spread across the map
2. ⚔️ Each kill increments the global kill counter
3. 🌉 Reaching kill milestones unlocks bridges (blocking walls disappear)
4. 👑 Once all bridges are open, the Boss spawns
5. 🏆 Defeating the Boss reveals the Victory Chest — collect it to win!

---

## 👤 Player Systems

- **Movement** — Velocity-based, physics-driven (no root motion)
- **Attacks** — Two melee combos (light/heavy) with unique damage, range, knockback, and movement lock
- **Hit Detection** — Animation Events + Physics2D.OverlapCircleAll
- **Health** — 5 HP max. Damage from melee and explosions triggers Defeat screen
- **Healing** — Press F to consume 1 Meat and restore 1 HP
- **Resources** — Collect Money, Meat, and Wood from enemy drops (visible in HUD)

---

## 👾 Enemies

| Type | Behavior |
|------|----------|
| **Goblin Torch** | Melee. Wanders, chases player, attacks on contact |
| **TNT Goblin** | Ranged. Throws parabolic TNT projectiles when in range |
| **Boss** | Orbits player, throws TNT and Barrels in combat phases |

**Spawn Mechanics:**
- Spawn from buildings with short "exit walk" phase
- Drop loot (Meat/Money Bag) on death
- Register kill in GameManager

---

## 🗺️ Map & Elevations

The map features **two elevation levels** connected by stairs.

**Elevation Changes:**
- 🔄 Swaps which tilemap colliders are active (wall vs. boundary)
- 📊 Adjusts player sprite sorting order for correct rendering

---

## 🖼️ UI

- **HUD** — Kill counter, player health bar, boss status notification
- **Inventory Panel** — Resource counts with flash feedback on collection/insufficient funds
- **Screens** — Start, Pause, Victory, Defeat (controlled via Time.timeScale)

---

## 🔊 Audio

**AudioManager (Singleton)**
- Handles all music and SFX
- Music tracks: intro clip + seamless loop
- Footstep sounds: automatic when player speed exceeds threshold

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Combat/          # Health, Knockback, PlayerAttack, Chest
│   ├── Core/            # GameManager, GameScreenManager, AudioManager, BridgeController
│   ├── Enemy/           # Enemy AI, Boss, SpawnPoint, LootDrop, EnemyDeath
│   ├── Gameplay/        # PickupSpawn
│   ├── Player/          # Movement, Attack, Death, Healing, ResourceCollector, Camera
│   ├── Tilemap Scripts/ # ElevationsEntry, ElevationsExit
│   └── UI/              # GameUI, UIManager, InventoryPanel, EnemyHealthBar
├── Scenes/
│   └── Level1.unity
├── Animations/
├── Images/              # Tilesets, sprites (Tiny Swords asset pack)
└── Sounds/
```

---

## 🛠️ Built With

- **Engine:** Unity 6
- **Rendering:** Universal Render Pipeline (URP)
- **Graphics:** Tiny Swords asset pack

---

## 📝 License

*Add your license information here*

---

**Happy Gaming! 🎯**
