/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Opsive.Shared.Game;
    using Opsive.Shared.StateSystem;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateCharacterController.Items.Actions.Bindings;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Storage;
    using UnityEngine;
    
    /// <summary>
    /// Attribute Binding is a class used to bind an attribute to a property.
    /// </summary>
    [Serializable]
    public struct AttributeBindingData
    {
        [Tooltip("The attribute name.")]
        [SerializeField] private string m_AttributeName;
        [Tooltip("The object path to the property bound to the attribute.")]
        [SerializeField] private string m_PropertyPath;
        
        public string AttributeName { get => m_AttributeName; set => m_AttributeName = value; }
        public string PropertyPath { get => m_PropertyPath; set => m_PropertyPath = value; }
    }
    
    /// <summary>
    /// A state Object binding used to bind Inventory Item Attributes to any property on a state Object.
    /// The state object property and the Attribute type must match.
    /// The Attribute to bind with will be found on the ItemObject.
    /// Assign an ItemCategory to make it easier for the editor to find a matching property.
    /// </summary>
    [Serializable]
    public class ItemObjectBinding : StateObjectBinding, IDatabaseSwitcher
    {
        [Tooltip("The category containing the Attribute that will be bound to some properties.")]
        [SerializeField] protected ItemCategory m_ItemCategory;
        [Tooltip("The name of the Attribute to bind.")]
        [SerializeField] protected AttributeBindingData[] m_AttributeBindingDataArray;
        
        [System.NonSerialized] protected AttributeBindingBase[] m_AttributeBindings;
        [System.NonSerialized] protected bool m_Initialized = false;
        [System.NonSerialized] protected ItemObject m_ItemObject;
        [System.NonSerialized] protected Item m_Item;

        public ItemCategory ItemCategory
        {
            get { return m_ItemCategory; }
            set { m_ItemCategory = value; }
        }

        public ItemObject ItemObject => m_ItemObject;
        public Item Item => m_Item;

        public AttributeBindingData[] AttributeBindingDataArray {
            get => m_AttributeBindingDataArray;
            set => m_AttributeBindingDataArray = value;
        }

        /// <summary>
        /// Initialize the state binding with the bound state object.
        /// </summary>
        protected override void InitializeInternal()
        {
            if (Application.isPlaying == false) { return; }
            if (m_Initialized) { return; }
            
            m_AttributeBindings = ConvertDataToAttributeBinding();

            
            var itemObject = m_BoundGameObject.GetCachedComponent<ItemObject>();
            if (itemObject != null) {
                SetItemObject(itemObject);
                m_Initialized = true;
            }
        }

        /// <summary>
        /// Convert the serialized data into bindings.
        /// </summary>
        /// <param name="dataArray">The data array to convert into bindings.</param>
        /// <returns>The bindings array.</returns>
        public AttributeBindingBase[] ConvertDataToAttributeBinding(AttributeBindingData[] dataArray = null)
        {
            if (dataArray == null) {
                dataArray = m_AttributeBindingDataArray;
            } else {
                m_AttributeBindingDataArray = dataArray;
            }

            if (dataArray == null || dataArray.Length == 0) {
                return Array.Empty<AttributeBindingBase>();
            }
            
            if (m_ItemCategory == null) {
                return Array.Empty<AttributeBindingBase>();
            }

            if (m_StateObject == null) {
                Debug.LogWarning("The State Object must be defined to convert the attribute bindings");
                return m_AttributeBindings;
            }

            var bindingList = new List<AttributeBindingBase>();
            for (int i = 0; i < dataArray.Length; i++) {
                var data = dataArray[i];

                if (string.IsNullOrWhiteSpace(data.PropertyPath)) {
                    continue;
                }

                var propertyInfo = m_StateObject.GetType().GetProperty(data.PropertyPath);
                if (propertyInfo == null) { continue; }

                var attribute = m_ItemCategory.GetAttribute(data.AttributeName);
                if (attribute == null) { continue; }
                
                // The attribute and property type must match.
                if (attribute.GetValueType() != propertyInfo.PropertyType) { continue; }
                
                var newAttributeBinding =
                    Activator.CreateInstance(
                        typeof(GenericAttributeBinding<>).MakeGenericType(propertyInfo.PropertyType)) as AttributeBindingBase;
                newAttributeBinding.AttributeName = data.AttributeName;
                newAttributeBinding.PropertyPath = data.PropertyPath;
                newAttributeBinding.CreatePropertyDelegates(m_StateObject);

                bindingList.Add(newAttributeBinding );
            }
            
            m_AttributeBindings = bindingList.ToArray();
            return m_AttributeBindings;
        }
        
        /// <summary>
        /// Convert the bindings into a serializable data format.
        /// </summary>
        /// <param name="bindingArray">The binding array to convert into a serialized data format.</param>
        /// <returns>The serializable data format.</returns>
        public AttributeBindingData[] ConvertAttributeBindingToData(AttributeBindingBase[] bindingArray = null)
        {
            if (bindingArray == null) {
                bindingArray = m_AttributeBindings;
            } else {
                m_AttributeBindings = bindingArray;
            }

            if (bindingArray == null || bindingArray.Length == 0) {
                return Array.Empty<AttributeBindingData>();
            }
            
            var dataList = new List<AttributeBindingData>();
            for (int i = 0; i < bindingArray.Length; i++) {
                var binding = bindingArray[i];
                var data = new AttributeBindingData();
                data.AttributeName = binding.AttributeName;
                data.PropertyPath = binding.PropertyPath;
                
                dataList.Add(data);
            }

            m_AttributeBindingDataArray = dataList.ToArray();
            return m_AttributeBindingDataArray;
        }
        
        /// <summary>
        /// Set the item object.
        /// </summary>
        /// <param name="itemObject">The item object.</param>
        public virtual void SetItemObject(ItemObject itemObject)
        {
            if (m_ItemObject != null) {
                Opsive.Shared.Events.EventHandler.UnregisterEvent(m_ItemObject, EventNames.c_ItemObject_OnItemChanged, SetItem);
                return;
            }

            m_ItemObject = itemObject;
            if (m_ItemObject == null) { return; }

            Opsive.Shared.Events.EventHandler.RegisterEvent(m_ItemObject, EventNames.c_ItemObject_OnItemChanged, SetItem);
            SetItem(m_ItemObject.Item);
        }

        /// <summary>
        /// Set the item from the ItemObject.
        /// </summary>
        private void SetItem()
        {
            SetItem(m_ItemObject.Item);
        }
        
        /// <summary>
        /// Set the item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetItem(Item item)
        {
            //Debug.Log("Set Item "+item);
            if (m_Item == item) {
                if (m_Item == null) { return; }

                // Refreshing the bindings will allow the new attribute to be set on bindings that aren't the main binding.
                for (int i = 0; i < m_AttributeBindings.Length; i++) {
                    m_AttributeBindings[i].Refresh();
                }
                return;
            }

            if (m_ItemCategory == null) {
                Debug.LogError($"Error: A category must be specified on the Item Binding {m_BoundGameObject}.", m_BoundGameObject);
                return;
            }
            
            // Always unbind attributes.
            UnbindAttributes();
            
            m_Item = item;
            if (m_Item == null || m_Item.IsInitialized == false) {
                m_Item = null;
                return;
            }

            if (m_ItemCategory.InherentlyContains(item) == false) {
                Debug.LogWarning($"The item specified ({item}) does not match item category that was set on the binding ({m_ItemCategory}).");
                m_Item = null;
                return;
            }

            var allAttributesCount = m_Item.GetAttributeCount();
            for (int i = 0; i < allAttributesCount; i++) {
                var attribute = m_Item.GetAttributeAt(i, true, true);
                BindAttribute(attribute);
                
                // This is a bit of a round about way to ensure this is a one-way binding.
                // It allow the attribute binding to still be referencing the Attribute without having the attribute reference the binding.
                attribute.Unbind(false);
            }
        }
        
        /// <summary>
        /// Unbind the attributes.
        /// </summary>
        protected virtual void UnbindAttributes()
        {
            for (int i = 0; i < m_AttributeBindings.Length; i++) {
                if(m_AttributeBindings[i] == null){ continue;}
                m_AttributeBindings[i].UnBindAttribute();
            }
        }

        /// <summary>
        /// Bind the attribute to a property.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        protected virtual void BindAttribute(AttributeBase attribute)
        {
            for (int i = 0; i < m_AttributeBindings.Length; i++) {
                if(m_AttributeBindings[i] == null){ continue;}
                if (m_AttributeBindings[i].AttributeName != attribute.Name) { continue; }
                m_AttributeBindings[i].BindAttribute(attribute);
            }
        }
        
        /// <summary>
        /// Check if the object contained by this component are part of the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>True if all the objects in the component are part of that database.</returns>
        bool IDatabaseSwitcher.IsComponentValidForDatabase(InventorySystemDatabase database)
        {
            if (database == null) { return false; }

            return database.Contains(m_ItemCategory);
        }

        /// <summary>
        /// Replace any object that is not in the database by an equivalent object in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>The objects that have been changed.</returns>
        ModifiedObjectWithDatabaseObjects IDatabaseSwitcher.ReplaceInventoryObjectsBySelectedDatabaseEquivalents(InventorySystemDatabase database)
        {
            if (database == null) { return null; }

            m_ItemCategory = database.FindSimilar(m_ItemCategory);

            return null;
        }
    }
}