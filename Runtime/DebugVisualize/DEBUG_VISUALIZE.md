# Debug Visualize System

## Overview

A runtime debug visualization system for Unity that displays text, stats, and shapes at world positions. Designed for development builds with runtime toggle capability and automatic removal in release builds.

## Architecture

```
DebugVisualizeManager (Singleton)
├── Settings (ScriptableObject)
│   └── ColorPalette
├── Systems
│   ├── TextDisplaySystem
│   ├── StatsDisplaySystem
│   └── ShapeDrawerSystem
├── Utilities
│   └── ObjectPool<T>
└── Components
    └── PhysicsDebugVisualizerMB
```

## Features

### Text Display
- Billboard text that follows Transform targets
- TextMeshPro-based rendering
- Automatic off-screen culling
- Configurable offset, color, font size

### Stats Display
- Lambda-based dynamic updates
- Progress bar support (current/max)
- Auto-refresh on interval or manual

### Shape Drawing
- Line, Ray/Arrow, Sphere, Circle, Box, Frustum
- Duration-based auto-cleanup
- Configurable thickness and color

### Technical
- Pool-based (500 pre-warmed)
- Persists across scenes (DontDestroyOnLoad)
- Runtime toggle enabled/disabled

## Usage

```csharp
// Toggle
DebugVisualize.Enabled = false;

// Text
DebugVisualize.ShowText("Label", target);
DebugVisualize.ShowText("Speed: 12.5", target, offset: Vector3.up);

// Stats
DebugVisualize.ShowStat("HP", () => player.Health);
DebugVisualize.ShowStat("Stamina", current: 75, max: 100, showBar: true);

// Shapes
DebugVisualize.Draw.Line(start, end);
DebugVisualize.Draw.Sphere(pos, radius);
DebugVisualize.Draw.Box(bounds);
DebugVisualize.Draw.Arrow(origin, direction, length: 5);
DebugVisualize.Draw.Circle(center, normal, radius);
```

## Configuration

Edit `DebugVisualizeSettings` (create via Assets > Create > DebugVisualize > Settings) to customize:
- Default colors per category
- Pool sizes
- Default duration
- Font settings
