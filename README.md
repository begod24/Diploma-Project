# Diploma-Project

## Thesis Title
**Procedural Generation of Modular Levels with Seed-Based Reproducibility and Simplified Wave Function Collapse in Unity**

---

## Overview
This project implements a procedural level generator in Unity 6 (URP) that:
- Assembles pre-made modular assets (rooms, corridors, doors) into a grid.
- Uses a lightweight rule-based “WFC-lite” algorithm to ensure logical connections.
- Supports seed-based generation: save a seed to JSON, reload to recreate the same level.
- Automatically places lights at key positions (room centers, corridor junctions).
- Allows switching between a top-down overview and a third-person walkthrough.

This system demonstrates both technical algorithms (WFC-lite, seed persistence) and practical game design (collision, lighting, camera control) in a sci-horror style inspired by the VOID concept.

---

## Key Features
- **Modular WFC-Lite:**  
  Each prefab carries “socket” data (Door/Wall) on its four sides. The generator picks only compatible modules next to each neighbor.
- **Seed Persistence:**  
  A numeric seed controls Random.InitState(seed). Save/Load your world parameters to `/save.json` and instantly regenerate the same layout.
- **Automatic Lighting:**  
  A simple LightPlacer script spawns point lights at the center of each generated module.
- **Dual Cameras:**  
  - **Top-Down Camera** (orthographic) for level-structure demonstration  
  - **Third-Person Camera** for exploring the generated world  
  Press **Tab** to toggle between views.
- **Ready for Defense & Portfolio:**  
  - Demo scene with UI for Generate/Save/Load  
  - Configurable width, height, seed  
  - Clean Git workflow with `.gitignore` & `.gitattributes`

---
