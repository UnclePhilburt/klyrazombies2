# Project Memory - Co-op Zombie Survival Game

## Overview
- **Engine**: Unity 6.3
- **Platform**: WebGL (browser-based)
- **Genre**: Co-op zombie survival (Surroundead-style)
- **Perspective**: Third-person
- **Players**: Multiplayer co-op

## Core Features (Surroundead-inspired)
- Open world survival
- Scavenging and looting
- Crafting system
- Base building
- Day/night cycle
- Zombie hordes

## Third-Party Assets
- **Opsive Ultimate Character Controller** - Character locomotion
- **Opsive Ultimate Inventory System** - Inventory management
- **Synty Studios Assets** - Art assets (stylized low-poly)
- **Cozy: Stylized Weather System** - Dynamic weather and time of day

## Technical Decisions
- Networking solution: TBD

## Development Guidelines

### Use Opsive Built-in Systems - DO NOT Build Custom
For any gameplay systems that Opsive already provides, **use the built-in Opsive systems** rather than creating custom scripts. This includes:
- **Equipping/Unequipping** - Use `EquipUnequip` ability & `ItemSetManager`
- **Weapon Switching** - Use built-in input bindings (Equip First, Equip Second, etc.)
- **Holstering** - Use `Holster Target` with `Objects` component IDs
- **Reloading** - Use `GenericReloader` module with `InventoryItemAmmo`
- **Ammo Management** - Use `InventoryItemAmmo` module (UIS integration)
- **Item Actions** - Use `CategoryItemActionSet` and built-in item actions
- **Inventory UI** - Use UIS UI Designer schemas

**Custom scripts are OK for:**
- New weapon prefabs/items (content)
- Game-specific features not covered by Opsive
- Integration with other systems (networking, save/load, etc.)

**Reference the Opsive Integration Demo** for proper setup patterns:
`Assets/Opsive/UltimateCharacterController/Integrations/UltimateInventorySystem/Demo/`

## Opsive UCC + UIS Integration Setup

### Weapon Configuration (for UIS integration)
When creating weapons that work with the Ultimate Inventory System:

1. **Use `InventoryItemAmmo` module** (NOT `ItemAmmo`)
   - Path: `Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem`
   - Set `m_AmmoItemCollectionNames: ["Default"]` to look in the Default inventory collection
   - Set `m_UseAmmoData: 0` if using AmmoItemDefinition directly

2. **ItemDefinition Setup**
   - Weapon ItemDefinition needs a `Prefabs` attribute pointing to the UCC weapon prefab
   - The prefab must have `CharacterItem`, `ShootableAction`, and perspective items configured

3. **Prefab Structure**
   - Weapon prefab's `ThirdPersonPerspectiveItem.m_Object` must reference an **internal child**, not an external prefab
   - Remove any `MeshCollider` from visual meshes to prevent physics collision with character

### Ammo/Consumable Item Categories
**IMPORTANT**: For ammo and consumable items to work with the ammo system:
- Category must have `m_IsMutable: 0` (items are identical/stackable)
- Category must have `m_IsUnique: 0` (items can stack)
- If these are set to 1, `StackableEquivalentTo` returns false and weapons won't find ammo

### UI Configuration
- `CategoryItemActionSet` in UI prefabs must reference `IntegrationDemoCategoryItemActions.asset` for equip/unequip actions
- Key UI prefabs: `Inventory Mirror Hotbar Panel.prefab`, `RPG Schema Full Layout.prefab`

### CharacterInventoryBridge Collections
- `Default` - Main inventory (where ammo/consumables go)
- `Equippable Slots` - Equipped weapons
- `Equippable` - Equippable items
- `Loadout` - Starting loadout items

### Holster System Setup
Holstering uses a **matching ID system** between the weapon and character:

1. **On the Character** - Create holster GameObjects (e.g., "PistolHolster" at hip, "RifleHolster" on back):
   - Parent them to the appropriate bone (pelvis for hip, spine for back)
   - Add `ObjectIdentifier` component (Opsive.UltimateCharacterController.Objects)
   - Set a unique **ID** (e.g., 1002 for pistol holster, 1003 for rifle holster)

2. **On the Weapon Prefab** - In `ThirdPersonPerspectiveItem` component:
   - Set `m_HolsterTarget.m_ID` to match the character's holster ID
   - Example: Pistol uses ID 1002, Rifle uses ID 1003

3. **Input Bindings** (already configured in CharacterInput.inputactions):
   - **T** = Toggle Item Equip (holster/unholster current weapon)
   - **1/2/3** = Equip First/Second/Third Item (switch between ItemSets)

4. **How it works**:
   - When you press T, Opsive looks at the weapon's HolsterTarget ID
   - Finds the ObjectIdentifier on the character with matching ID
   - Parents the weapon to that holster position

**If holstering doesn't work**: Check that weapon's HolsterTarget ID matches a holster ObjectIdentifier ID on the character.

### ItemSet Rules (for weapon switching with 1/2/3)
- Located in: `Assets/Data/InventoryDatabase/InventoryDatabase/ItemSetRules/`
- `FixedRangedWeaponItemSetRule` - Category-based rule that auto-creates ItemSets for all RangedWeapon items
- Add rules to `InventoryItemSetManager` component on character
- Order of rules determines which key equips which weapon (Rule 0 = key 1, Rule 1 = key 2, etc.)

### Multiple Weapons Visible (Both Holstered)
To have multiple weapons stay visible on the character when switching between them:
- In the ItemSetRule (e.g., `FixedRangedWeaponItemSetRule.asset`), set:
  - `m_DoNotShareItemBetweenSet: 0` (allows items to stay spawned across ItemSet switches)
- Each weapon needs a unique holster target ID matching an ObjectIdentifier on the character
- When you switch weapons with 1/2/3, the previous weapon goes to its holster instead of disappearing

## Open Questions
- Player count per session?
- Networking solution? (Photon Fusion, Unity NGO, etc.)
- Map size / streaming approach?
- Persistent world or session-based?
