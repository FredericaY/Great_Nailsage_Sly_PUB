# Great_Nailsage_Sly

## Project Directory Structure
````markdown
Assets/
├── Game/
│   ├── Animation/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   └── Textures/
│
├── Resources/
├── ThirdParty/
└── Packages/
````

### Assets/Game/

Main game content created and maintained by this project.
All core gameplay assets, scripts, and scenes live under this folder.

#### Game/Animation/

Animation clips and animator-related assets.

* `Player/` – Player character animations
* `Enemies/` – Enemy animations
* `Environment/` – Environmental or object animations

#### Game/Audio/

Audio assets used in the game.

* `BGM/` – Background music
* `SFX/` – Sound effects

#### Game/Prefabs/

Reusable prefab assets.

* `Enemies/` – Enemy prefabs
* `Environment/` – Level/environment prefabs
* `VFX/` – Visual effects prefabs

#### Game/Scenes/

Unity scenes.

* `_Bootstrap/` – Entry/bootstrap scenes for initialization
* `Levels/` – Gameplay levels

#### Game/Scripts/

All gameplay-related C# scripts, organized by responsibility.

* `Core/` – Core interfaces, shared logic, and project-wide abstractions
* `Player/` – Player-related logic (Root, Controller, modules)
* `Enemies/` – Enemy logic and AI
* `Systems/` – World-level systems (audio, camera, save, etc.)
* `UI/` – UI logic and controllers
* `Utils/` – General-purpose utility code

#### Game/Textures/

Texture assets used by sprites, animations, and UI.

* `Player/` – Player textures
* `Enemies/` – Enemy textures
* `Environment/` – Environment textures
* `FX/` – Visual effects textures
* `UI/` – UI textures

---

### Assets/Resources/

Unity `Resources` folder.
Reserved for assets that must be loaded dynamically at runtime.
(Used sparingly and intentionally.)

---

### Assets/ThirdParty/

Third-party assets, plugins, or external tools not developed in this project.

---

### Packages/

Unity Package Manager dependencies.
Automatically managed by Unity.

