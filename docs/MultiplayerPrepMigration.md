# Multiplayer Prep Migration Notes

This document records the multiplayer-prep refactor. The goal was to keep single-player behavior intact while removing the most important single-player assumptions.

## Added Files

- `Core/PlayerInputIntent.cs`: local player input command boundary.
- `Core/PlayerRegistry.cs`: tracks players by stable local `PlayerId`.
- `Core/LocalPlayerContext.cs`: owns the local presentation player for UI, camera, and input.
- `Core/WorldMutationService.cs`: central entry point for world mutations such as building placement.

## Changed Files

### `Core/GameConstants.cs`

Added constants for:

- `LocalPlayerGroupName`
- `InvalidPlayerId`
- `DefaultLocalPlayerId`
- `KeyboardAndMouseDeviceId`

### `Entities/Characters/Player/Player.cs`

Added multiplayer-prep metadata:

- `PlayerId`
- `IsLocalPlayer`
- `InputDeviceId`

Added local input guards so non-local players do not consume `_UnhandledInput` events in this process. Placeable item selection now calls `BuildingSystem.SetBuildingTargetForPlayer(...)` so build requests carry the initiating player id.

### `Components/PlayerInputComponent.cs`

The component now produces a `PlayerInputIntent` before writing movement to the actor blackboard. The current implementation still reads the existing Godot input map, but the call boundary is ready for per-device or peer-provided intent sources.

### `Core/GameManager.cs`

`GameManager` now owns:

- `PlayerRegistry`
- `LocalPlayerContext`
- `GameManager.Instance`

Player registration happens when `Player` nodes are discovered. Respawn keeps the current local player's identity, input device, inventory snapshot, collision settings, and camera binding.

### `UI/Inventory/GameInventoryUI.cs`

The inventory UI now binds to `GameManager.Instance.LocalPlayerContext.CurrentPlayer` instead of the first node in the `Player` group.

### `Entities/Enemies/Enemy.cs`

Enemies now query `PlayerRegistry` for target candidates and choose the closest valid live player. This removes the old dependency on scene tree group ordering.

### `Core/BuildingSystem.cs`

Building placement now creates a `PlaceObjectRequest` and passes it to `WorldMutationService.TryPlaceObject(...)`. Placement still happens locally and immediately.

## Behavior Kept Intact

- Single-player still defaults to `PlayerId = 1`.
- Existing movement blackboard keys are still written for compatibility.
- Existing attack, hotbar, inventory, and building flows remain local.
- The Phantom Camera still follows the local player after respawn.

## Remaining Single-Player Assumptions

- `Player._UnhandledInput` still owns attack and hotbar commands directly.
- `BuildingSystem` still uses the local mouse position for preview placement.
- `SelectionManager` still uses the active viewport camera and mouse position.
- `GameFlow` pause is still global via `GetTree().Paused`.
- Item drops and tree drops still instantiate directly in the local scene.
- There are no stable network/world entity ids yet.

## Recommended Next Steps

1. Expand `PlayerInputIntent` to include aim, attack, hotbar selection, build, cancel build, and interact commands.
2. Move attack and inventory mutations behind authority-friendly services.
3. Add a `WorldEntityId` for spawned actors, placed objects, and item drops.
4. Create a session abstraction that maps `PlayerId` to peer id or local input device id.
5. Add world save/load using stable entity ids and existing item ids.
6. Add a small two-player local test scene before starting LAN networking.
