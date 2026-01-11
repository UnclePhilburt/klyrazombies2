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

## Git / Version Control

### Git LFS Required
This project uses **Git LFS** for large files. WebGL builds produce files over 100MB which exceed GitHub's limit.

**LFS-tracked file types:**
- `*.unityweb` - Compressed WebGL build files

**Before pushing:**
```bash
git lfs install  # One-time setup
git lfs track "*.unityweb"  # Already in .gitattributes
```

**If push fails with "file too large" error:**
1. Track the file type with LFS: `git lfs track "*.extension"`
2. Remove from git cache: `git rm --cached "path/to/large/file"`
3. Re-add: `git add "path/to/large/file"`
4. Amend commit: `git commit --amend --no-edit`
5. Push again

**Files to NOT commit:**
- `mono_crash*.json` - Unity crash logs
- `Assets/_Recovery/*` - Unity recovery scenes

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

### Input Bindings
The project uses **both** input systems (`activeInputHandler: 2` in ProjectSettings).

**Key Bindings:**
- **Tab** = Open/Close Inventory Panel (Main Menu)
- **T** = Toggle Item Equip (holster/unholster)
- **1/2/3** = Equip First/Second/Third weapon
- **R** = Reload
- **F** = Interact with lootable objects
- **Escape** = Close Panel

**Input Configuration Files:**
1. **New Input System**: `Packages/com.opsive.shared/Runtime/Input/InputSystem/CharacterInput.inputactions`
   - Used by `Player Input` component on character
   - This is where keyboard bindings like Tab for inventory are set
2. **Legacy Input Manager**: `ProjectSettings/InputManager.asset`
   - Also has "Open Panel" mapped to Tab
   - Both systems are active, so both need matching bindings

**To change a key binding:**
1. Edit `CharacterInput.inputactions` for the new Input System
2. Edit Input Manager (`Edit > Project Settings > Input Manager`) for legacy system

### Item Icon Override System
Custom system to override item icons at runtime (since UIS Icon attribute doesn't work well with generated icons).

**Components:**
1. **ItemIconDatabase** (`Assets/Scripts/Inventory/ItemIconDatabase.cs`)
   - ScriptableObject mapping ItemDefinitions to Sprites
   - Must be placed in `Assets/Resources/` and named `ItemIconDatabase`

2. **IconDatabaseItemView** (`Assets/Scripts/UI/IconDatabaseItemView.cs`)
   - ItemViewModule that displays icons from the database
   - Add to ItemView prefabs, assign the Icon Image field
   - Falls back to UIS Icon attribute if no database entry

**Editor Tools:**
- **Tools > Icon Generator** - Generates icon PNGs from 3D prefabs (SM_Item_*, SM_Wep_*, SM_Prop_*)
- **Tools > Item Icon Assigner** - Auto-matches icons to ItemDefinitions by name, builds the database

## Loot System

### LootTable System
Custom loot table system for container spawning:
- **LootTable** (`Assets/Scripts/Loot/LootTable.cs`) - ScriptableObject defining possible loot
- **LootableContainer** - Component for interactive loot containers
- **SimpleLootUI** - Tarkov-style loot UI with search mechanics

**LootTable Properties:**
- `possibleItems` - List of items with weight, minAmount, maxAmount
- `minItemTypes` / `maxItemTypes` - How many different items spawn
- `emptyChance` - Percentage chance container is empty

**LootTable Editor Features:**
- **Add Items from Category** - Auto-populate from a UIS category
- **Add ALL Items** - Add all non-weapon items to table (skips weapons, ammo, backpacks)
- **Test Roll Loot** - Preview random loot rolls
- **Drop Chances** - Shows percentage chance for each item

**Location:** `Assets/Data/LootTables/`

### SimpleLootUI (Tarkov-style Search)
The loot UI features a search mechanic where players must search containers before seeing contents:
- Progress circle animation during search
- Items revealed one-by-one
- **Container memory** - Already-searched containers don't need re-searching
- Sound effects for searching, item reveals, and inventory actions
- Drag-and-drop between container and player inventory
- Equipment slot support with swap functionality
- **Same-frame close prevention** - 0.1s delay after opening to prevent F key from instantly closing

**Key Controls:**
- **F** = Interact with lootable (open container/corpse)
- **F/Escape/Tab** = Close loot UI

### InteractionHighlight System
Visual feedback for lootable objects (magnifying glass icon above objects):
- **InteractionHighlight** (`Assets/Scripts/Loot/InteractionHighlight.cs`) - Shows icon when player looks at lootable
- **LootableInteraction** (`Assets/Scripts/Loot/LootableInteraction.cs`) - Raycast-based detection for lootables

**Features:**
- Procedural magnifying glass icon (no sprite asset needed)
- Auto-creates icon if Start() hasn't run when ForceShowIcon() is called
- Falls back to circle sprite if SearchIcon not in Resources
- Icon always faces camera (billboard)

### ZombieLootable System
Makes zombie corpses lootable after death:
- **ZombieLootable** (`Assets/Scripts/Loot/ZombieLootable.cs`) - Add to dead zombies
- Uses prefab from `Resources/Prefabs/ZombieLootContainer` for inventory setup
- Auto-adds InteractionHighlight and collider
- Call `ZombieLootable.MakeZombieLootable(zombieGameObject)` to make a corpse lootable

### Office Item Categories
Items are organized into categories under the `Office` parent:
- **Documents and Papers** - Documents, Manila Folder, Notebook, Book, Magazine, Clipboard
- **Office Supplies** - Pen, Pencil, Scissors, Stapler, Tape Roll, Rubber Bands, Paper Clips
- **Electronics** - Batteries, Flashlight, USB Drive, Calculator, Laptop, Headphones, Smartphone, Walkie Talkie, SD Card
- **Valuables** - Cash, Watch, Keys, ID Card, Briefcase, Trophy
- **Food and Drink** - Candy Bar, Chips, Soda Can, Energy Drink, Water Bottle, Coffee Mug, Donut, Sandwich, Canned Food, Alcohol
- **Office Medical** - Bandages, Painkillers, Hand Sanitizer, Pills
- **Office Misc** - Lighter, Matches, String, Cloth Rag, Duct Tape, Cigarette

**Editor Tools:**
- **Tools > Generate Office Items** - Creates ItemDefinitions with proper category assignments

## Backpack Equipment System

Backpacks provide extra inventory slots when equipped:
- **Small Backpack** - 5 extra slots
- **Backpack** - 10 extra slots
- **Large Backpack** - 15 extra slots

**Key Components:**
- `BackpackEquipHandler` - Spawns visual backpack model when equipped
- `BackpackAttachmentHandler` - Manages attachment points by size
- `HolsterPositionAdjuster` - Moves rifle holster based on equipped backpack size
- Uses UIS `DynamicInventorySize` for slot management
- `m_PreventRemoveBagItemIfWouldOverflow` prevents unequipping if items would be lost

**Attachment Points:** Small/Medium/Large backpacks each have their own spine attachment point

## Equipment/Clothing System

Visual equipment system for clothing, helmets, masks, and accessories that appear on the character.

### Core Components

1. **EquipmentSlotType** (`Assets/Scripts/Character/EquipmentSlot.cs`)
   - Enum defining all equipment slots: Head, Face, Hair, Torso, Hands, Legs, Feet, Back, Shoulders, Knees, Belt

2. **EquipmentVisualHandler** (`Assets/Scripts/Character/EquipmentVisualHandler.cs`)
   - Listens to UIS inventory for equip/unequip events
   - Spawns/destroys visual prefabs at attachment points
   - Supports both prefab spawning and Sidekick part swapping

3. **SidekickCharacterManager** (`Assets/Scripts/Character/SidekickCharacterManager.cs`)
   - Bridges UIS equipment with Synty Sidekick Character Creator
   - Maps EquipmentSlotType to Sidekick CharacterPartType
   - Supports body shape customization (masculine/feminine, muscles, weight)

### Equipment Categories (UIS)
Under the `Equippable` parent category:
- **Headwear** - Helmets, hats
- **Facewear** - Masks, goggles, glasses
- **Torso** - Shirts, jackets (future: Sidekick integration)
- **Gloves** - Hand protection
- **Pants** - Leg clothing (future: Sidekick integration)
- **Footwear** - Shoes, boots (future: Sidekick integration)
- **Backpack** - Already exists, provides inventory slots

### Editor Tools
All under **Project Klyra > Equipment** menu:
- **Generate Equipment Categories** - Creates UIS categories for equipment slots
- **Equipment Slot Setup** - Finds bones and creates attachment points on character
- **Generate Equipment Items** - Scans Synty attachments and creates ItemDefinitions

### Attachment Prefab Naming (Synty)
Equipment attachments use the `SM_Chr_Attach_*` pattern:
- `SM_Chr_Attach_Helmet_*` - Helmets (Head slot)
- `SM_Chr_Attach_Hat_*` - Hats (Head slot)
- `SM_Chr_Attach_Mask_*` - Masks (Face slot)
- `SM_Chr_Attach_Goggles_*` - Goggles (Face slot)
- `SM_Chr_Attach_Glasses_*` - Glasses (Face slot)
- `SM_Chr_Attach_Armour_Shoulder_*` - Shoulder armor
- `SM_Chr_Attach_Armour_Knee_*` - Knee pads
- `SM_Chr_Attach_Pouch_*` - Belt pouches

### Sidekick Character Creator System (Primary Character System)
The game uses Synty's Sidekick Character Creator for fully customizable player characters.

**Database Location:** `Assets/Synty/SidekickCharacters/Database/Side_Kick_Data.db` (SQLite)

**Sidekick Resources:**
- **Base Model**: `Resources/Meshes/SK_BaseModel`
- **Material**: `Resources/Materials/M_BaseMaterial`

#### Species in Database
| ID | Name | Code | Notes |
|----|------|------|-------|
| 1 | Human | HN | Primary player species |
| 2 | Goblin | GO | Not used |
| 4 | Skeleton | SN | Not used |
| 5 | Elves | EV | Not used |
| 6 | Zombie | ZB | For zombie NPCs |
| 7 | Unrestricted | UR | Parts usable by all |

#### Part Groups (PartGroup enum)
Parts are organized into 3 groups that form complete presets:
- **Head (1)** - Face, hair, facial features
- **UpperBody (2)** - Torso, arms, hands
- **LowerBody (3)** - Hips, legs, feet

#### Character Part Types (CharacterPartType enum)
| Type | Name | Group | Human Variations | Notes |
|------|------|-------|------------------|-------|
| 1 | Head | Head | 3 | Base face mesh |
| 2 | Hair | Head | 70 | Hairstyles |
| 3 | EyebrowLeft | Head | 43 | Left eyebrow |
| 4 | EyebrowRight | Head | 43 | Right eyebrow |
| 5 | EyeLeft | Head | 1 | Left eye |
| 6 | EyeRight | Head | 1 | Right eye |
| 7 | EarLeft | Head | 40 | Left ear shape |
| 8 | EarRight | Head | 40 | Right ear shape |
| 9 | FacialHair | Head | 61 | Beards, mustaches (optional) |
| 35 | Nose | Head | 41 | Nose shapes |
| 36 | Teeth | Head | 40 | Teeth styles |
| 37 | Tongue | Head | 1 | Tongue |
| 22 | AttachmentHead | Head | 105 | Hats, helmets (optional) |
| 23 | AttachmentFace | Head | 69 | Masks, glasses (optional) |
| 10 | Torso | Upper | 89 | Chest/shirt |
| 11 | ArmUpperLeft | Upper | 65 | Left upper arm |
| 12 | ArmUpperRight | Upper | 65 | Right upper arm |
| 13 | ArmLowerLeft | Upper | 72 | Left forearm |
| 14 | ArmLowerRight | Upper | 72 | Right forearm |
| 15 | HandLeft | Upper | 50 | Left hand |
| 16 | HandRight | Upper | 50 | Right hand |
| 24 | AttachmentBack | Upper | 97 | Backpacks (optional) |
| 17 | Hips | Lower | 82 | Waist/pants |
| 18 | LegLeft | Lower | 71 | Left leg |
| 19 | LegRight | Lower | 71 | Right leg |
| 20 | FootLeft | Lower | 68 | Left foot/shoe |
| 21 | FootRight | Lower | 68 | Right foot/shoe |

#### Body Shape System (Blend Shapes)
Body shape is controlled by **blend shapes**, NOT separate meshes. One base body morphs between all shapes.

**Blend Shape Sliders:**
| Slider | Range | Effect |
|--------|-------|--------|
| `masculineFeminine` | -100 to 100 | -100=Masculine, 100=Feminine |
| `defaultBuff` | -100 to 100 | Muscle definition |
| `defaultHeavy` | 0 to 100 | Body weight (heavier) |
| `defaultSkinny` | 0 to 100 | Body weight (thinner) |

**Body Shape Presets (in database):**
- Feminine: Super Slim, Slim, Medium, Heavy, Super Heavy, Muscular
- Masculine: Super Slim, Slim, Medium, Heavy, Super Heavy, Muscular

#### Color System
**Color Groups (ColorGroup enum):**
| ID | Name | Use |
|----|------|-----|
| 1 | Species | Skin tones |
| 2 | Outfits | Clothing colors |
| 3 | Attachments | Accessory colors |
| 4 | Materials | Metal, leather, etc. |
| 5 | Elements | Fire, ice, etc. |

**Skin Color Properties:**
- Skin 01, 02, 03 (body skin tones)
- Lips, Scar, Nose, Mouth
- Eye Color Left/Right, Eye Highlight, Eye Edge, Eye Inner, Eye Outer
- Eyebrow Left/Right
- Hair 01, 02, Head Stubble
- Facial Hair 01, 02, Facial Stubble
- Teeth 01, 02, Gums, Tongue
- Fingernails, Toenails, Eyelids

**Human Skin Tone Presets (10):**
Pale White, White, Light Tan, Tan, Dark Tan, Light Brown, Medium Brown, Dark Brown, Very Dark Brown, Black

#### Available Human Presets
**Apocalypse Themed (for our game):**
| Preset Family | Head | Upper | Lower | Notes |
|---------------|------|-------|-------|-------|
| Apocalypse Outlaws | 01-20 | 01-10 | 01-10 | Raiders/bandits |
| Apocalypse Survivor | 01-05 | 01-05 | 01-05 | **Main player clothing** |
| Modern Civilians | 01-18 | 01-12 | 01-11 | Casual clothes |
| Species Humans | 01-10 | - | - | **Head-only presets** |
| Base Body | - | 1 | 1 | **Bare skin (underwear)** |

**Other Themes (not used):**
- Fantasy Knights, Fantasy Villagers
- Pirate Captains, Viking Warriors, Samurai Warriors
- Sci-fi Civilians, Sci-fi Soldiers

#### Key Presets for Character Creation

**"Base Body" Preset (ID 1024, 1025)** - Bare skin body we created:
- Upper Body (ID 1024): `SK_HUMN_BASE_01_*` parts for torso, arms, hands
- Lower Body (ID 1025): `SK_HUMN_BASE_01_*` parts for hips, legs, feet

**"Species Humans" Presets (ID 439-448)** - Head-only presets with varied faces:
Each contains: Head, Hair, EyebrowL/R, EyeL/R, EarL/R, Nose, Teeth, Tongue
Optional: FacialHair, AttachmentHead, AttachmentFace

#### Part Naming Convention
Parts follow this pattern: `SK_[OUTFIT]_[NUM]_[PARTTYPE]_[SPECIES]`

Examples:
- `SK_HUMN_BASE_01_01HEAD_HU01` - Human Base Head 01
- `SK_APOC_SURV_01_02HAIR_HU01` - Apocalypse Survivor 01 Hair
- `SK_HUMN_BASE_01_10TORS_HU01` - Human Base Torso (bare skin)

#### Character Creation Plan

**In Main Menu (Character Creator):**
1. **Body**: Always use "Base Body" preset (bare skin)
2. **Gender**: Body shape slider (`masculineFeminine` blend shape)
3. **Face Customization**:
   - Hair (70 options)
   - Eyebrows (43 options, matched L/R)
   - Facial Hair (61 options, optional)
   - Nose (41 options)
   - Ears (40 options, matched L/R)
4. **Colors**:
   - Skin tone (10 presets)
   - Hair color
   - Eye color

**In-Game (Clothing System):**
- Player starts in underwear (Base Body)
- Finds clothing items that apply Apocalypse Survivor/Modern Civilian presets
- Equipping shirt → changes UpperBody preset
- Equipping pants → changes LowerBody preset
- Face/head remains as created

#### Core Components
- **SidekickPlayerController** (`Assets/Scripts/Character/SidekickPlayerController.cs`)
  - Runtime component that manages the player's Sidekick character
  - Handles part presets (Head, UpperBody, LowerBody)
  - Body shape controls (masculine/feminine, muscles, weight)
  - Save/load character appearance

- **SidekickCharacterSpawner** (`Assets/Scripts/Character/SidekickCharacterSpawner.cs`)
  - Spawns player in game scene with saved appearance
  - Loads character data from PlayerPrefs
  - Integrates with Opsive player prefab

- **CharacterCreationUI** (`Assets/Scripts/UI/CharacterCreationUI.cs`)
  - Main menu character creator interface
  - Navigate presets with prev/next buttons
  - Body shape sliders
  - Randomize and save character

**Content Packs Installed:**
- **ApocalypseZombies** - Zombie character parts
- **ApocalypseSurvivors** - Human survivor clothing/parts (shirts, pants, shoes)

**Character Save Data** (PlayerPrefs key: "PlayerCharacter"):
```json
{
  "headPresetName": "Species Humans 01",
  "upperBodyPresetName": "Base Body",
  "lowerBodyPresetName": "Base Body",
  "bodyType": 0,
  "muscles": 50,
  "bodySize": 0
}
```

### Sidekick Clothing Equipment (UIS Integration)
Players can find clothing items in-game that change their character's appearance:

**Components:**
- **SidekickClothingEquipHandler** (`Assets/Scripts/Character/SidekickClothingEquipHandler.cs`)
  - Listens to UIS inventory for clothing equip/unequip events
  - Updates Sidekick character appearance when clothing is equipped
  - Reverts to saved appearance when clothing is unequipped

**Item Attributes Required:**
- `SidekickPartGroup` (string) - "Head", "UpperBody", or "LowerBody"
- `SidekickPresetName` (string) - Name of the Sidekick preset to apply

**Clothing Categories (UIS):**
- **Clothing** - Parent category for all wearable clothes
  - **Headwear** - Hats, helmets (changes Head parts)
  - **Shirt** - Shirts, jackets (changes UpperBody parts)
  - **Pants** - Pants, shoes (changes LowerBody parts)

**Editor Tool:**
- **Project Klyra > Sidekick > Generate Clothing Items**
  - Creates clothing categories if needed
  - Loads all Sidekick presets
  - Generates ItemDefinitions with proper attributes
  - Items go in `Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing/`

**How It Works:**
1. Player creates character in main menu (saved to PlayerPrefs)
2. Player spawns in game with saved appearance
3. Player finds clothing items in loot containers
4. Equipping clothing item changes Sidekick character parts
5. Unequipping reverts to original saved appearance

### Sidekick + Opsive Integration (Game Scene Spawning)

Integrating dynamically-created Sidekick characters with Opsive's character controller requires special handling because:
- Opsive controls animations via `AnimatorMonitor` component
- Sidekick creates characters with their own skeleton and `Animator`
- Both systems need to coexist without breaking physics or animations

**The Solution: AnimatorSync Pattern**

Instead of trying to make Opsive control the Sidekick character directly, we:
1. Keep the original Opsive character **active** (for physics, colliders, input)
2. **Hide its meshes** (disable SkinnedMeshRenderers, NOT the GameObject)
3. Create the Sidekick character as a visual overlay
4. Use `AnimatorSync` to copy animation state from Opsive's animator to Sidekick's animator

**Why hide meshes instead of disabling GameObjects?**
- Disabling the GameObject also disables CapsuleCollider, Rigidbody, etc.
- This causes the character to fall through the map
- Hiding just the meshes keeps physics working while making the old model invisible

**Key Components:**

1. **AnimatorSync** (`Assets/Scripts/Character/AnimatorSync.cs`)
   - Copies animator parameters (floats, ints, bools) every LateUpdate
   - Syncs layer weights
   - Syncs animation states using CrossFade with normalized time
   - Syncs transitions when they're happening
   - Source: Opsive's animator (controlled by AnimatorMonitor)
   - Target: Sidekick character's animator

2. **SidekickPlayerController** (`Assets/Scripts/Character/SidekickPlayerController.cs`)
   - `m_BuildFromScratch = true` for game scene spawning
   - `ApplyAppearanceBuildFromScratch()` handles the integration:
     - Hides old model meshes (doesn't disable GameObjects)
     - Creates Sidekick character with `SidekickRuntime.CreateCharacter()`
     - Sets up AnimatorSync between animators

3. **SidekickCharacterSpawner** (`Assets/Scripts/Character/SidekickCharacterSpawner.cs`)
   - Instantiates player prefab at spawn point
   - Configures SidekickPlayerController for build-from-scratch mode
   - Calls Initialize() and LoadAppearance()

**Architecture Diagram:**
```
Player Root (Opsive components: UltimateCharacterLocomotion, CapsuleCollider, etc.)
├── Original Model (meshes HIDDEN, GameObject ACTIVE)
│   ├── Animator (controlled by Opsive AnimatorMonitor)
│   ├── SkinnedMeshRenderers (disabled)
│   └── Skeleton bones (still animating)
│
└── SidekickCharacter (visual only)
    ├── Animator (receives synced state via AnimatorSync)
    ├── SkinnedMeshRenderers (visible - custom appearance)
    └── Own skeleton bones (driven by synced animator)
```

**Troubleshooting:**
- **T-posing**: AnimatorSync not initialized, or animator controller not assigned
- **Falling through map**: Old model's GameObject was disabled instead of just hiding meshes
- **No animations**: Check that both animators have the same RuntimeAnimatorController

### Legacy Character System (Deprecated)
The old character selection system using baked Synty characters is deprecated:
- **CharacterModelSwitcher** - Old toggle system for SM_Chr_* models
- **CharacterSpawner** - Old spawner for preset characters
- **CharacterDatabase** - Old character selection database

## Synty Asset Packs

Imported packs with lootable content:
- **PolygonApocalypse** - Base apocalypse assets, weapons, backpacks
- **PolygonOffice** - Office building props (pens, laptops, donuts, etc.)
- **PolygonPoliceStation** - Police gear, weapons, snacks
- **PolygonMapsPlaza** - Mall/shopping items
- **PolygonNatureBiomes** - Camping/outdoor items
- **PolygonGeneric** - General purpose items
- **SidekickCharacters** - Modular character system (zombie variants, future: survivor customization)
- **InterfaceApocalypseHUD** - UI icons for inventory items

**Prefab Naming:**
- `SM_Prop_*` - Props (office items, furniture)
- `SM_Item_*` - Pickup items
- `SM_Wep_*` - Weapons
- `SM_Chr_Attach_*` - Character attachments (backpacks)

## UI Systems

### MainMenuUI
Main menu controller for game start:
- **MainMenuUI** (`Assets/Scripts/UI/MainMenuUI.cs`) - Handles Play, Settings, Quit
- Settings panel with volume sliders, quality dropdown, fullscreen toggle
- Saves preferences to PlayerPrefs
- Scene to load configured via `m_GameSceneName` field

**Main Menu Scene:** TBD (user creating scene in Unity Editor)

## Hosting Options

**itch.io:**
- Simple HTML/WebGL hosting
- Basic analytics (views, plays)
- Community features built-in

**Netlify/GitHub Pages:**
- Host from git repo
- Custom domain support
- More control over deployment

## Open Questions
- Player count per session?
- Networking solution? (Photon Fusion, Unity NGO, etc.)
- Map size / streaming approach?
- Persistent world or session-based?
- Main menu design (style, features)?
