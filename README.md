# RogueTrader.Overwatch

Overwatch for Warhammer 40,000: Rogue Trader. Every hero and enemy holding a ranged weapon gets
Overwatch buttons on that weapon's action bar, working like built-in abilities. They use the
game's own overwatch engine, which ships in the game unused.

| Ability | What it does |
|---|---|
| **Reaction Shot** | XCOM 2 overwatch: fire once at the first enemy that moves or acts within range around you, then it ends. |
| **Overwatch** | Drag a cone: moving the cursor farther makes it longer and narrower, closer makes it shorter and wider. One shot per AP you had left, spread across the enemies that move or act in it. |
| **Suppression** | Pin one enemy: it can stay put and fight, but every move draws a ranged attack of opportunity. Your hero keeps up visible fire that always misses to sell the threat. |

All three end your turn. Shots use your weapon's normal attack and hit chance, which drops with
distance. The full rules and how each one maps onto the engine are in
[docs/DESIGN.md](docs/DESIGN.md).

## Status

In development; nothing is released yet. Work is tracked as RTO tickets
(see [docs/USER_STORIES.md](docs/USER_STORIES.md)).
