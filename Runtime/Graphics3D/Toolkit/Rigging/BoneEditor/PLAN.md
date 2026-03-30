# BoneToolV2 — Implementation Plan

## Overview

A Unity editor rigging tool for creating skeletons, painting bone weights, and exporting skinned prefabs. Single EditorWindow built with VisualElement/UIToolkit. All state lives in an in-memory ScriptableObject (RigDocument) for full undo/redo. No GameObjects spawned — bones drawn via Handles API.

## Core Principles

- **Single window**, 3 modes: Skeleton Edit → Weight Paint → Test Pose
- **No GameObjects spawned** — all bone state in RigDocument, drawn via Handles
- **Full undo/redo** — via Undo.RecordObject on ScriptableObject
- **Vertex-centric weights** — per-vertex weight array, natural for painting + validation
- **Brush-based painting** — radius, strength, falloff, smooth, heatmap overlay
- **VisualElement throughout** — UIToolkit for all UI

## Window Layout

```
┌─ Toolbar ─────────────────────────────────────────────────────────┐
│ [Mesh ▼]  [Skeleton ▼]  │  [Skeleton] [Paint] [Test]  │ [Export ▼] │
├─ Left Sidebar (250px) ──┬─ Scene View ────────────────────────────┤
│ Bone Tree               │                                         │
│  ▸ Root                  │  (interactive scene editing)            │
│    ▸ Spine               │                                         │
│      ▸ Arm_L             │  Bones drawn as octahedral shapes       │
│      ▸ Arm_R             │  with Handles                           │
│    ▸ Leg_L               │                                         │
│    ▸ Leg_R               │  Weight heatmap overlay in Paint mode   │
│                          │                                         │
│ [+] Add  [×] Delete     │                                         │
│ Name: [________]         │                                         │
│ Parent: [dropdown]       │                                         │
│ Color: [■]               │                                         │
├──────────────────────────┴─────────────────────────────────────────┤
│ Brush: Radius [====] Strength [====] Falloff [====] [Add|Sub|Smooth] │
├────────────────────────────────────────────────────────────────────┤
│ Skeleton Mode | Bones: 8 | Verts: 1234 | ⚠ 12 unpainted           │
└────────────────────────────────────────────────────────────────────┘
```

## File Structure

```
GPUSkinning/BoneToolV2/
├── Model/
│   ├── RigDocument.cs              # ScriptableObject (in-memory, undo-enabled)
│   ├── BoneData.cs                 # name, parentIndex, bindpose, color
│   └── WeightData.cs               # per-vertex: BoneWeightPair[4]
├── Controllers/
│   ├── IToolMode.cs                # OnEnter, OnExit, OnSceneGUI, OnKeyDown
│   ├── SkeletonEditMode.cs         # Click-to-place, drag chains, position/rotation handles
│   ├── WeightPaintMode.cs          # Brush painting with radius/strength/falloff
│   ├── TestPoseMode.cs             # Rotate bones, live GPU deformation preview
│   └── BrushSettings.cs            # Shared brush state
├── SceneOverlay/
│   ├── BoneGizmoDrawer.cs          # Octahedral bone shapes, names, hierarchy lines
│   ├── WeightHeatmapDrawer.cs      # Vertex-colored mesh overlay (blue→red)
│   └── SceneInputHandler.cs        # Event routing to active mode
├── Services/
│   ├── AutoWeightService.cs        # Distance-based auto-weight assignment
│   ├── WeightMirrorService.cs      # L↔R mirror by name convention
│   ├── ValidationService.cs        # Unpainted verts, weight sum, orphaned bones
│   ├── PrefabBuilderService.cs     # Creates SkinnedMeshRenderer prefab
│   └── ExportService.cs            # SkeletonAsset, Mesh, prefab, one-click pipeline
├── View/
│   ├── BoneToolV2Window.cs         # EditorWindow entry point
│   ├── ModeToolbar.cs              # Segmented mode buttons
│   ├── BoneTreeView.cs             # Hierarchical bone list with drag-reparent
│   ├── BrushSettingsPanel.cs       # Radius/strength/falloff sliders
│   └── StatusBar.cs                # Validation warnings, counts
└── Utilities/
    ├── MeshQueryAccel.cs           # Spatial grid for fast vertex-in-sphere queries
    └── UndoHelper.cs               # Undo.RecordObject wrapper
```

## Data Model

### RigDocument (ScriptableObject, in-memory only)
- `Mesh sourceMesh` — the mesh being rigged
- `SkeletonAsset sourceSkeletonAsset` — optional, loaded skeleton for editing
- `List<BoneData> bones` — all bones in the rig
- `WeightData[] vertexWeights` — one entry per vertex
- `ToolModeEnum activeMode` — Skeleton, Paint, Test
- `int selectedBoneIndex` — currently active bone (-1 = none)

### BoneData (Serializable)
- `string name` — e.g. "spine_01", "arm_L"
- `int parentIndex` — -1 = root
- `Matrix4x4 bindpose` — world-to-bone at bind time
- `Color displayColor` — for visual distinction

### WeightData (Serializable, per-vertex)
- `BoneWeightPair[] influences` — up to 4 entries (boneIndex, weight), sorted by weight desc

## Modes

### Skeleton Edit Mode
- Left-click empty space: create bone at raycast hit
- Left-click existing bone: select (show position/rotation handle)
- Shift+click: create child of selected bone
- Delete key: remove bone (reparent children to grandparent)
- "Capture Bind Pose" button: current transforms → bindposes

### Weight Paint Mode
- Requires bone selected
- Mouse move: brush circle projected on mesh
- Left-drag: paint weight (additive, strength + falloff)
- Ctrl+drag: erase weight
- Shift+drag: smooth weights
- Auto-normalize after each stroke
- Heatmap: blue=0, green=0.5, red=1.0, magenta=unpainted

### Test Pose Mode
- Select bone → rotation handle
- Live GPU deformation via EditorSkinningPreview
- "Reset Pose" snaps to bind pose
- Optional animation scrubber if Animator available

## Services

### AutoWeightService
- For each vertex, find N nearest bones by distance to bone segment
- Weight = 1/distance², normalized, top 4 influences

### WeightMirrorService
- Detects mirror by name convention (_L↔_R, Left↔Right)
- Copies weights from source vertices to mirrored vertices, remapping bone indices

### ValidationService
- Unpainted vertices (total weight < epsilon)
- Overweight vertices (total weight > 1 + epsilon)
- Orphaned bones (no influenced vertices)

### PrefabBuilderService
- Creates GameObject hierarchy from bone tree
- Adds SkinnedMeshRenderer with mesh, bones[], bindposes
- Saves as prefab asset
- Optional auto-add Animator + AnimatorController

### ExportService
- Export SkeletonAsset
- Export Mesh (weights + bindposes)
- Build Skinned Prefab
- One-click pipeline: export → build prefab → open AnimationBakerWindow

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `1/2/3` | Switch mode (Skeleton/Paint/Test) |
| `[` `]` | Brush radius -/+ |
| `Shift+[` `]` | Brush strength -/+ |
| `X` | Delete bone |
| `Shift+D` | Duplicate bone |
| `N` | Rename bone |
| `A` | Auto-weight selected bone |
| `Ctrl+Z/Y` | Undo/Redo (free via Unity) |

## Implementation Phases

### Phase 1 — Skeleton Editor + Basic Weight Assign + Export
Model/*, SkeletonEditMode, WeightPaintMode (click-based), BoneGizmoDrawer, ExportService, Window + TreeView, UndoHelper

### Phase 2 — Brush Painting + Heatmap
BrushSettings, WeightHeatmapDrawer, MeshQueryAccel, BrushSettingsPanel, upgrade WeightPaintMode to brush

### Phase 3 — Auto-Weights + Validation
AutoWeightService, ValidationService, StatusBar

### Phase 4 — Test Pose + Live Preview
TestPoseMode, reuse EditorSkinningPreview

### Phase 5 — Prefab Builder + One-Click Pipeline
PrefabBuilderService, export dropdown

### Phase 6 — Mirror + Polish
WeightMirrorService, symmetry mode, tree drag-reparent

## Dependencies

- Reuses: `EditorSkinningPreview`, `GPUSkinUploader`, `SkeletonAsset`, `BoneWeightConverter`
- Namespace: `Snm.GPUSkinning.BoneToolV2`
- All files wrapped in `#if UNITY_EDITOR`
- No new `.asmdef`
