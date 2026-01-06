/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using System;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Inventory;
    using Opsive.UltimateInventorySystem.Core;
    using UnityEditor;

    /// <summary>
    /// Custom inspector for the ItemSetManager component.
    /// </summary>
    [CustomEditor(typeof(InventoryItemSetManager))]
    public class InventoryItemSetManagerInspector : ItemSetManagerBaseInspector
    {
        public override Type ItemCategoryType => typeof(ItemCategory);
    }
}