/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using System;
    using System.Reflection;
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Editor.Inspectors;
    using Opsive.UltimateInventorySystem.Editor.VisualElements;
    using Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Implements TypeControlBase for the ObjectAmounts ControlType.
    /// </summary>
    [ControlType(typeof(ItemObjectBinding))]
    public class ItemObjectBindingControl : ControlWithInventoryDatabase
    {

        /// <summary>
        /// Create the ObjectAmountsView.
        /// </summary>
        /// <param name="value">The default value.</param>
        /// <param name="onChangeEvent">The on change event function.</param>
        /// <param name="field"></param>
        /// <returns>The ObjectAmountsView.</returns>
        public override VisualElement CreateCustomControlVisualElement(object value, Func<object, bool> onChangeEvent,
            FieldInfo field)
        {
            var inventoryItemBinding = value as ItemObjectBinding;
            if (inventoryItemBinding == null) {
                inventoryItemBinding = new ItemObjectBinding();
            }

            var stateObject = inventoryItemBinding.StateObject;
            if (stateObject == null) {
                return new Label("The Bound StateObject was not found.");
            }

            var bindings = inventoryItemBinding.ConvertDataToAttributeBinding();
            if (bindings == null) {
                bindings = new AttributeBindingBase[0];
            }

            for (int i = 0; i < bindings.Length; i++) {
                bindings[i].BoundObject = stateObject;
            }
            inventoryItemBinding.ConvertAttributeBindingToData(bindings);
            
            var bindingView = new ItemCategoryAttributeBindingView(m_Database, null, stateObject);
            bindingView.SetItemCategory(inventoryItemBinding.ItemCategory);
            bindingView.SetAttributeBindings(bindings);
            bindingView.OnItemCategoryChanged += (newCategory) =>
            {
                inventoryItemBinding.ItemCategory = newCategory;
                var bindings = inventoryItemBinding.ConvertDataToAttributeBinding();
                for (int i = 0; i < bindings.Length; i++) {
                    bindings[i].BoundObject = stateObject;
                }
                inventoryItemBinding.ConvertAttributeBindingToData(bindings);
                onChangeEvent?.Invoke(inventoryItemBinding);
            };
            bindingView.OnAttributeBindingsChanged += (newBindings) =>
            {
                var bindings = newBindings.ToArray();
                for (int i = 0; i < bindings.Length; i++) {
                    bindings[i].BoundObject = stateObject;
                }
                inventoryItemBinding.ConvertAttributeBindingToData(bindings);
                onChangeEvent?.Invoke(inventoryItemBinding);
            };

            var container = new VisualElement();

            container.Add(bindingView);

            return container;
        }
    }
}