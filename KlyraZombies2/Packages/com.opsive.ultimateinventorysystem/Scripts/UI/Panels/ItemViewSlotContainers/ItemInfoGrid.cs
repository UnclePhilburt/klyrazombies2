/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Grid;

    /// <summary>
    /// The inventory grid  UI.
    /// </summary>
    public class ItemInfoGrid : GridGeneric<ItemInfo>
    {
        /// <summary>
        /// Click the box at the index.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override void ViewClicked(int index)
        {
            if (m_Elements.Count <= StartIndex + index) {
                NotifyEmptyClicked(index);
                return;
            }

            var selectedElements = GetElementAt(StartIndex + index);

            NotifyElementClicked(index, selectedElements);
        }
    }
}
