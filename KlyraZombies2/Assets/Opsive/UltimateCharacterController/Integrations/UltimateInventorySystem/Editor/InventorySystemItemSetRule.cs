/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using Opsive.Shared.Editor.Managers;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Inventory;
    using Opsive.UltimateInventorySystem.Editor.Styles;
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>
    /// Custom inspector for the ItemSetRule with Inventory System objects.
    /// </summary>
    [CustomEditor(typeof(InventorySystemItemSetRule))]
    public class InventorySystemItemSetRule : ItemSetRuleInspector
    {
        /// <summary>
        /// Add the styles to the container.
        /// </summary>
        /// <param name="container">The container to add styles to.</param>
        protected override void AddStyleSheets(VisualElement container)
        {
            base.AddStyleSheets(container);
            container.styleSheets.Add(Shared.Editor.Utility.EditorUtility.LoadAsset<StyleSheet>("e70f56fae2d84394b861a2013cb384d0")); // Shared stylesheet.
            container.styleSheets.Add(CommonStyles.StyleSheet);
            container.styleSheets.Add(ManagerStyles.StyleSheet);
            container.styleSheets.Add(ControlTypeStyles.StyleSheet);
            container.styleSheets.Add(InventoryManagerStyles.StyleSheet);
            container.styleSheets.Add(InventoryManagerStyles.StyleSheet);
        }
    }
}