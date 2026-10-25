# My Workspace

> Crafting worlds, one block and one frame at a time.

Hi there! I'm a Minecraft modpack developer and Blender animator. This repository
serves as my personal workspace for experiments, tools, and creative projects.

## What I Do

### Minecraft Modpack Development

- Design and assemble curated modpacks with balanced progression, performance tuning, and immersive experiences
- Configure mod integration, quest lines, and custom resource packs
- Automate pack building, versioning, and release workflows
- Maintain launcher-side tweaks and player-friendly QoL improvements

### Blender Animation

- Create stylized character animations and cinematic camera sequences
- Model, rig, and animate low-poly scenes for game trailers and short films
- Motion studies and physics-based simulations for dynamic action shots

## Repository Layout

```
my-workspace/
├── modpacks/      # Minecraft modpack configs, scripts, and notes
├── blender/       # Blender projects, add-ons, and animation experiments
├── tools/         # Utility scripts and automation snippets
└── docs/          # Documentation and learning notes
```

## Getting Started

Clone the repository and explore the folders above. Each subfolder contains its
own README with setup instructions where needed.

```bash
git clone <your-repo-url> my-workspace
cd my-workspace
```

## Click Counter

`click-counter/` is a small WinForms counter app (net8.0-windows). Launch it
with the one-click startup script from the repository root:

```bat
start-click-counter.bat
```

The script tries the already-built executable first
(`click-counter\bin\...\click-counter.exe`) and falls back to `dotnet run`
from the project folder when the exe is missing. Build manually with:

```bash
cd click-counter
dotnet build
```

## Contact

- GitHub: [@your-name](https://github.com/your-name)
- Projects and updates are shared as they become ready.

---

*Made with a pickaxe and a timeline.*
