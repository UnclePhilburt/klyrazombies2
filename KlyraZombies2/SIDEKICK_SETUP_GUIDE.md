# Sidekick Character System Setup Guide

This guide walks you through setting up the complete Sidekick character creation and clothing system.

---

## Overview

The system has **two parts**:

1. **Character Creation (Main Menu)** - Player customizes their character before playing
2. **Game Scene Spawning** - Player spawns in-game with their customized appearance
3. **Clothing Equipment (Optional)** - Players find clothing that changes appearance

---

## PART 1: Create the Player Base Prefab

The Sidekick character needs a "shell" prefab that has all the Opsive components but NO character model. The Sidekick mesh gets added at runtime.

### Step 1.1: Duplicate Your Current Player Prefab

1. In Project window, find: `Assets/Prefabs/characters/SM_Chr_Biker_Male_01 1.prefab`
2. **Right-click → Duplicate**
3. Rename the duplicate to: `PlayerBase`
4. Move it to: `Assets/Prefabs/PlayerBase.prefab`

### Step 1.2: Strip the Visual Model

1. **Double-click `PlayerBase.prefab`** to open in Prefab Mode
2. In the Hierarchy, find the child object that contains the actual character mesh (probably named `SM_Chr_Biker_Male_01` or similar - it will have a SkinnedMeshRenderer)
3. **Delete this mesh child** - we want ONLY the Opsive components, not the visual
4. Your prefab should now have:
   - The root GameObject with CharacterLocomotion, Inventory, etc.
   - Camera/Spring objects
   - Colliders
   - But NO SkinnedMeshRenderer/character mesh

### Step 1.3: Create a Model Container

1. In the PlayerBase prefab hierarchy, **right-click the root → Create Empty**
2. Name it: `Models`
3. Reset its Transform (Position 0,0,0, Rotation 0,0,0, Scale 1,1,1)
4. This is where the Sidekick character mesh will be spawned

### Step 1.4: Save the Prefab

1. **Ctrl+S** to save the prefab
2. Exit Prefab Mode

---

## PART 2: Set Up the Main Menu Character Creation

### Step 2.1: Open the MainMenu Scene

1. **File → Open Scene**
2. Navigate to: `Assets/Scenes/MainMenu.unity`

### Step 2.2: Create the Character Preview Setup

We need a place to show the character while the player customizes it.

1. **Right-click in Hierarchy → Create Empty**
2. Name it: `CharacterCreator`
3. Position it where you want the character preview to appear (e.g., Position: 0, 0, 5)

4. **With CharacterCreator selected, right-click → Create Empty**
5. Name the child: `CharacterPreview`
6. This will hold the actual Sidekick character

### Step 2.3: Add SidekickPlayerController

1. **Select the `CharacterPreview` GameObject**
2. **Add Component → SidekickPlayerController**
3. Configure the component:

| Field | Value |
|-------|-------|
| Base Model Override | (leave empty - loads from Resources) |
| Base Material Override | (leave empty - loads from Resources) |
| Animator Controller | Drag any humanoid animator controller here (optional for preview) |
| Character Name | "PreviewCharacter" |
| Body Type | 0 |
| Muscles | 50 |
| Body Size | 0 |
| Debug Log | ✓ (check this for now) |
| Build On Start | ✓ (check this) |

### Step 2.4: Create the Character Spawn Point

1. **Right-click CharacterCreator → Create Empty**
2. Name it: `SpawnPoint`
3. Position it at: 0, 0, 0 (relative to CharacterCreator)
4. This is where the preview character appears

### Step 2.5: Set Up a Camera for the Preview

1. If you don't have a camera pointing at the character preview area, create one:
   - **Right-click Hierarchy → Camera**
   - Name it: `CharacterPreviewCamera`
   - Position it to look at the CharacterCreator area

### Step 2.6: Create the Character Creation UI

1. **Right-click Hierarchy → UI → Canvas** (if you don't have one already)
2. Inside your Canvas, create this UI structure:

```
Canvas
└── CharacterCreationPanel (Panel)
    ├── Title (TextMeshPro - "Create Your Character")
    │
    ├── HeadSection (Horizontal Layout Group)
    │   ├── PrevHeadBtn (Button) - "<"
    │   ├── HeadLabel (TextMeshPro) - "Head: 1/5"
    │   └── NextHeadBtn (Button) - ">"
    │
    ├── UpperBodySection (Horizontal Layout Group)
    │   ├── PrevUpperBtn (Button) - "<"
    │   ├── UpperLabel (TextMeshPro) - "Upper Body: 1/5"
    │   └── NextUpperBtn (Button) - ">"
    │
    ├── LowerBodySection (Horizontal Layout Group)
    │   ├── PrevLowerBtn (Button) - "<"
    │   ├── LowerLabel (TextMeshPro) - "Lower Body: 1/5"
    │   └── NextLowerBtn (Button) - ">"
    │
    ├── BodyTypeSlider (Slider) - Range: -100 to 100
    ├── BodyTypeLabel (TextMeshPro) - "Body Type: Neutral"
    │
    ├── MusclesSlider (Slider) - Range: 0 to 100
    ├── MusclesLabel (TextMeshPro) - "Build: Average"
    │
    ├── BodySizeSlider (Slider) - Range: -100 to 100
    ├── BodySizeLabel (TextMeshPro) - "Size: Average"
    │
    ├── ButtonsSection (Horizontal Layout Group)
    │   ├── RandomizeBtn (Button) - "Randomize"
    │   ├── BackBtn (Button) - "Back"
    │   └── ConfirmBtn (Button) - "Confirm"
```

### Step 2.7: Add CharacterCreationUI Component

1. **Select the CharacterCreationPanel GameObject**
2. **Add Component → CharacterCreationUI**
3. Wire up ALL the references:

| Field | Drag This Object |
|-------|------------------|
| Character Controller | CharacterPreview (the one with SidekickPlayerController) |
| Prev Head Button | PrevHeadBtn |
| Next Head Button | NextHeadBtn |
| Prev Upper Body Button | PrevUpperBtn |
| Next Upper Body Button | NextUpperBtn |
| Prev Lower Body Button | PrevLowerBtn |
| Next Lower Body Button | NextLowerBtn |
| Body Type Slider | BodyTypeSlider |
| Muscles Slider | MusclesSlider |
| Body Size Slider | BodySizeSlider |
| Head Label | HeadLabel |
| Upper Body Label | UpperLabel |
| Lower Body Label | LowerLabel |
| Body Type Label | BodyTypeLabel |
| Muscles Label | MusclesLabel |
| Body Size Label | BodySizeLabel |
| Randomize Button | RandomizeBtn |
| Confirm Button | ConfirmBtn |
| Back Button | BackBtn |
| Character Spawn Point | SpawnPoint |
| Rotation Speed | 100 |
| Save Key | "PlayerCharacter" |

### Step 2.8: Configure Sliders

For each slider, set these values:

**BodyTypeSlider:**
- Min Value: -100
- Max Value: 100
- Value: 0

**MusclesSlider:**
- Min Value: 0
- Max Value: 100
- Value: 50

**BodySizeSlider:**
- Min Value: -100
- Max Value: 100
- Value: 0

### Step 2.9: Wire Up Confirm Button to Load Game

In the CharacterCreationUI script, the OnConfirm() method just logs. You need to add scene loading:

1. Select ConfirmBtn
2. In the Button component's OnClick() section, add:
   - **+ button**
   - Drag the CharacterCreationPanel (with CharacterCreationUI)
   - Select: CharacterCreationUI → SaveCharacter()
3. Add another OnClick entry:
   - Drag an empty GameObject with a script that calls `SceneManager.LoadScene("MainMap")`
   - OR use the MainMenuUI if it has a LoadGame method

### Step 2.10: Save the Scene

**Ctrl+S** to save MainMenu.unity

---

## PART 3: Set Up the Game Scene Spawner

### Step 3.1: Open the Game Scene

1. **File → Open Scene**
2. Navigate to: `Assets/Scenes/MainMap.unity` (or your main game scene)

### Step 3.2: Create the Player Spawner

1. **Right-click Hierarchy → Create Empty**
2. Name it: `PlayerSpawner`
3. Position it where players should spawn (e.g., Position: 0, 1, 0)

### Step 3.3: Add SidekickCharacterSpawner Component

1. **Select PlayerSpawner**
2. **Add Component → SidekickCharacterSpawner**
3. Configure:

| Field | Value |
|-------|-------|
| Spawn Point | (leave empty to use this transform, OR create a child spawn point) |
| Player Prefab Base | Drag your `PlayerBase.prefab` here |
| Base Model Override | (leave empty) |
| Base Material Override | (leave empty) |
| Animator Controller | Drag the same animator controller your player uses |
| Save Key | "PlayerCharacter" (MUST match the CharacterCreationUI save key!) |
| Spawn On Awake | ✓ |
| Debug Log | ✓ (for testing) |

### Step 3.4: Create a Spawn Point (Optional)

If you want a specific spawn location:

1. **Right-click PlayerSpawner → Create Empty**
2. Name it: `SpawnPoint`
3. Position it where the player should appear
4. Drag SpawnPoint into the `Spawn Point` field of SidekickCharacterSpawner

### Step 3.5: Disable/Remove Old Player

If you have an existing player in the scene:

1. Find the old player GameObject
2. Either **Delete it** or **Disable it** (uncheck the checkbox)
3. The SidekickCharacterSpawner will create the player at runtime

### Step 3.6: Save the Scene

**Ctrl+S** to save

---

## PART 4: Add Clothing Equipment System (Optional)

This lets players find clothing that changes their appearance.

### Step 4.1: Generate Clothing Categories

1. **Menu: Project Klyra → Sidekick → Generate Clothing Items**
2. Click **"Create Clothing Categories"**
3. This creates:
   - Clothing (parent)
   - Headwear
   - Shirt
   - Pants

### Step 4.2: Generate Clothing ItemDefinitions

1. In the same window, click **"Load Sidekick Presets"**
2. You'll see lists of Head, UpperBody, and LowerBody presets
3. Check the ones you want as items (or click "All" for each section)
4. Click **"Generate X Clothing ItemDefinitions"**
5. Items are created in: `Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing/`

### Step 4.3: Configure Items in UIS Database Editor

The generated items need category and attribute assignment:

1. **Window → Opsive → Ultimate Inventory System → Database Editor**
2. For each clothing item:
   - Set the **Category** (Headwear, Shirt, or Pants)
   - Add **SidekickPartGroup** attribute (string): "Head", "UpperBody", or "LowerBody"
   - Add **SidekickPresetName** attribute (string): The exact preset name

### Step 4.4: Add SidekickClothingEquipHandler to Player

1. Open your `PlayerBase.prefab`
2. **Add Component → SidekickClothingEquipHandler**
3. Configure:

| Field | Value |
|-------|-------|
| Character Controller | (leave empty - auto-finds) |
| Inventory | (leave empty - auto-finds) |
| Equipped Collection Name | "Equipped" |
| Part Group Attribute | "SidekickPartGroup" |
| Preset Name Attribute | "SidekickPresetName" |
| Debug Log | ✓ |

### Step 4.5: Add Clothing to Loot Tables

1. Open your loot tables in: `Assets/Data/LootTables/`
2. Add the clothing items to appropriate tables
3. Players can now find clothing in containers!

---

## PART 5: Testing

### Test 1: Character Creation

1. Play the MainMenu scene
2. You should see a character appear at the preview position
3. Click the navigation buttons - character should change appearance
4. Move the sliders - body shape should change
5. Click Randomize - should randomize everything
6. Click Confirm - should save and (if wired up) load game scene

### Test 2: Game Spawning

1. Play the MainMap scene directly
2. Even without creating a character, it should spawn with defaults
3. Check Console for debug logs from SidekickCharacterSpawner

### Test 3: Full Flow

1. Play MainMenu scene
2. Customize character
3. Click Confirm
4. Game scene loads
5. Your customized character should appear!

---

## Troubleshooting

### "Could not load SK_BaseModel from Resources!"

The Sidekick base model isn't in the Resources folder.

**Fix:**
1. Find `SK_BaseModel` in: `Assets/Synty/SidekickCharacters/Resources/Meshes/`
2. Make sure the path is exactly: `Resources/Meshes/SK_BaseModel`
3. OR drag the prefab into the `Base Model Override` field

### "Could not load M_BaseMaterial from Resources!"

Same issue with material.

**Fix:**
1. Find the base material in Sidekick folder
2. Make sure path is: `Resources/Materials/M_BaseMaterial`
3. OR drag the material into `Base Material Override` field

### Character doesn't appear

1. Check Console for errors
2. Make sure `Build On Start` is checked
3. Verify the Animator Controller is assigned (or try without one first)

### Character appears but is T-posed

The Animator Controller isn't set up correctly.

**Fix:**
1. Assign a proper humanoid animator controller
2. Make sure the character has an Animator component

### Save/Load not working

The save keys don't match.

**Fix:**
1. CharacterCreationUI `Save Key` must be: "PlayerCharacter"
2. SidekickCharacterSpawner `Save Key` must be: "PlayerCharacter"
3. Both must match exactly!

### Clothing doesn't change appearance

1. Check that SidekickClothingEquipHandler is on the player
2. Verify item has `SidekickPartGroup` and `SidekickPresetName` attributes
3. Check that `Equipped Collection Name` matches your inventory setup

---

## Quick Reference: Component Locations

| Component | Where to Add It |
|-----------|-----------------|
| SidekickPlayerController | CharacterPreview object in MainMenu |
| CharacterCreationUI | UI Panel in MainMenu |
| SidekickCharacterSpawner | Empty GameObject in game scene |
| SidekickClothingEquipHandler | PlayerBase prefab |

---

## File Locations

| File | Path |
|------|------|
| PlayerBase Prefab | `Assets/Prefabs/PlayerBase.prefab` |
| Main Menu Scene | `Assets/Scenes/MainMenu.unity` |
| Game Scene | `Assets/Scenes/MainMap.unity` |
| Clothing Items | `Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing/` |
| Sidekick Database | `Assets/Synty/SidekickCharacters/Database/Side_Kick_Data.db` |
