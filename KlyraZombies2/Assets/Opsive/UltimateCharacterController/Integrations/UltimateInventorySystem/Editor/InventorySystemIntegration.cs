/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using Opsive.Shared.Editor.Managers;
    using UnityEngine.UIElements;
    using ManagerUtility = Opsive.UltimateCharacterController.Editor.Managers.ManagerUtility;

    /// <summary>
    /// Draws an inspector that can be used within the Inspector Manager.
    /// </summary>
    [OrderedEditorItem("Ultimate Inventory System", 1)]
    public class InventorySystemIntegration : IntegrationInspector
    {
        /// <summary>
        /// Draws the integration inspector.
        /// </summary>
        /// <param name="container">The parent VisualElement container.</param>
        public override void ShowIntegration(VisualElement container)
        {
            var imguiContainer = new IMGUIContainer(DrawImgui);
            container.Add(imguiContainer);
        }

        /// <summary>
        /// Draw the editor with imgui.
        /// </summary>
        private void DrawImgui()
        {
            ManagerUtility.DrawControlBox("Setup Location", null,
                "The integration is located within the Ultimate Inventory System Manager.",
                true, "Open",
                Opsive.UltimateInventorySystem.Editor.Managers.InventoryMainWindow.ShowIntegrationsManagerWindow, null);
        }
    }
}