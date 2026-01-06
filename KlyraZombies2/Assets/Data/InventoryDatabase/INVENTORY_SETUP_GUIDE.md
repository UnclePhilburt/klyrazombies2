# Survival Game Inventory Setup Guide

Open Unity and go to: **Tools > Opsive > Ultimate Inventory System > Main Manager**

---

## STEP 1: Create Item Categories

In the **Categories** tab, create these categories in order (child categories need parents first):

### Base Categories (No Parent)
| Category Name | Is Abstract | Notes |
|--------------|-------------|-------|
| All | Yes | Root category - already exists |
| Equippable | No | Items that can be equipped |
| Consumable | No | Items that are used up |
| Material | No | Crafting materials |
| Ammo | No | Ammunition |

### Child Categories
| Category Name | Parent | Notes |
|--------------|--------|-------|
| Weapon | Equippable | All weapons |
| Tool | Equippable | Usable tools |
| MeleeWeapon | Weapon | Melee weapons |
| RangedWeapon | Weapon | Guns, bows |
| Food | Consumable | Restores hunger/health |
| Medical | Consumable | Heals damage |
| BuildingMaterial | Material | For base building |
| CraftingComponent | Material | General crafting |

---

## STEP 2: Category Attributes

For each category, add these attributes in the **Category Attributes** section:

### All (Root) - Already has Description, Icon
Add:
- `MaxStackSize` (int) - Default: 1
- `Weight` (float) - Default: 1.0

### Consumable Category
Add:
- `HealthRestore` (int) - Default: 0
- `HungerRestore` (int) - Default: 0
- `ThirstRestore` (int) - Default: 0
- `UseTime` (float) - Default: 1.0

### Weapon Category
Add:
- `Damage` (int) - Default: 10
- `AttackSpeed` (float) - Default: 1.0
- `Durability` (int) - Default: 100

### RangedWeapon Category
Add:
- `AmmoType` (string) - Default: ""
- `ClipSize` (int) - Default: 30
- `FireRate` (float) - Default: 0.1

### Material Category
Add:
- `MaxStackSize` override - Default: 99

---

## STEP 3: Create Item Definitions

In the **Items** tab, create these items:

### WEAPONS (RangedWeapon Category)

| Item Name | Damage | ClipSize | AmmoType | MaxStackSize |
|-----------|--------|----------|----------|--------------|
| AK-47 | 35 | 30 | 7.62mm | 1 |
| SR-9 | 25 | 15 | 9mm | 1 |
| Shotgun | 80 | 8 | 12gauge | 1 |
| Hunting Rifle | 75 | 5 | 7.62mm | 1 |

### WEAPONS (MeleeWeapon Category)

| Item Name | Damage | AttackSpeed | Durability | MaxStackSize |
|-----------|--------|-------------|------------|--------------|
| Baseball Bat | 20 | 1.2 | 50 | 1 |
| Fire Axe | 35 | 0.8 | 100 | 1 |
| Crowbar | 25 | 1.0 | 150 | 1 |
| Combat Knife | 15 | 1.5 | 80 | 1 |
| Machete | 30 | 1.0 | 80 | 1 |

### AMMO (Ammo Category)

| Item Name | MaxStackSize | Weight |
|-----------|--------------|--------|
| 7.62mm Rounds | 120 | 0.01 |
| 9mm Rounds | 150 | 0.008 |
| 12 Gauge Shells | 40 | 0.03 |
| Arrows | 30 | 0.05 |

### FOOD (Food Category)

| Item Name | HealthRestore | HungerRestore | ThirstRestore | MaxStackSize |
|-----------|---------------|---------------|---------------|--------------|
| Canned Beans | 5 | 30 | 0 | 10 |
| Canned Soup | 10 | 25 | 10 | 10 |
| Energy Bar | 0 | 15 | 0 | 20 |
| Bottled Water | 0 | 0 | 50 | 10 |
| Soda | 5 | 5 | 40 | 10 |
| Raw Meat | -5 | 20 | 0 | 5 |
| Cooked Meat | 15 | 40 | 0 | 5 |
| Apple | 5 | 10 | 5 | 15 |

### MEDICAL (Medical Category)

| Item Name | HealthRestore | UseTime | MaxStackSize |
|-----------|---------------|---------|--------------|
| Bandage | 15 | 2.0 | 10 |
| First Aid Kit | 50 | 4.0 | 5 |
| Painkillers | 25 | 1.0 | 10 |
| Antibiotics | 10 | 1.0 | 5 |
| Adrenaline Shot | 0 | 0.5 | 3 |

### BUILDING MATERIALS (BuildingMaterial Category)

| Item Name | MaxStackSize | Weight |
|-----------|--------------|--------|
| Wood Plank | 50 | 2.0 |
| Metal Scrap | 50 | 3.0 |
| Nails | 100 | 0.1 |
| Rope | 20 | 0.5 |
| Duct Tape | 10 | 0.2 |
| Cloth | 30 | 0.3 |

### CRAFTING COMPONENTS (CraftingComponent Category)

| Item Name | MaxStackSize | Weight |
|-----------|--------------|--------|
| Gun Parts | 10 | 1.0 |
| Electronics | 20 | 0.5 |
| Chemicals | 15 | 0.3 |
| Herbs | 30 | 0.1 |
| Empty Bottle | 10 | 0.2 |

### TOOLS (Tool Category)

| Item Name | Durability | MaxStackSize |
|-----------|------------|--------------|
| Flashlight | 100 | 1 |
| Lockpick | 5 | 10 |
| Binoculars | 999 | 1 |
| Repair Kit | 3 | 5 |

---

## STEP 4: Link to Character Item Prefabs

For weapons, you need to link the Item Definition to the Character Item prefab:

1. Select your weapon Item Definition
2. Add attribute: `CharacterItem` (GameObject type)
3. Assign your weapon prefab (e.g., AK-47 prefab)

This connects the inventory item to the actual equippable weapon.

---

## STEP 5: Set Up Item Actions (Optional)

For consumables to work, add Item Actions:

1. In Item Definition, expand **Item Actions**
2. Add **Simple Item Action**
3. Configure to heal/restore attributes on use

---

## Quick Start Checklist

- [ ] Create base categories (Equippable, Consumable, Material, Ammo)
- [ ] Create child categories (Weapon, Food, Medical, etc.)
- [ ] Add category attributes (MaxStackSize, Weight, etc.)
- [ ] Create at least one weapon item
- [ ] Create ammo items for that weapon
- [ ] Create a few consumables for testing
- [ ] Link weapon to Character Item prefab
- [ ] Test picking up and equipping items in Play mode
