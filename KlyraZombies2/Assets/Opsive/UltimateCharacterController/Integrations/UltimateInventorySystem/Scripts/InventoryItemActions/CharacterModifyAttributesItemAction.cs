/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Traits;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.ItemActions;
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    [Serializable]
    public class CharacterItemAttributePair
    {
        [Tooltip("The character attribute name to increase/decrease.")]
        [SerializeField] protected string m_DefaultCharacterAttribute = "Health";
        [Tooltip("The value used to increase/decrease.")]
        [SerializeField] protected float m_DefaultValue = 0;
        [Tooltip("The item attribute name of the character attribute name.")]
        [SerializeField] protected string m_NameItemAttributeName = "Attribute";
        [FormerlySerializedAs("m_ItemAttributeName")]
        [Tooltip("The attribute name used to get the value.")]
        [SerializeField] protected string m_ValueItemAttributeName = "Heal";

        public string DefaultCharacterAttribute { get => m_DefaultCharacterAttribute; set => m_DefaultCharacterAttribute = value; }
        public float DefaultValue { get => m_DefaultValue; set => m_DefaultValue = value; }
        public string NameItemAttributeName { get => m_NameItemAttributeName; set => m_NameItemAttributeName = value; }
        public string ValueItemAttributeName { get => m_ValueItemAttributeName; set => m_ValueItemAttributeName = value; }
    }

    /// <summary>
    /// Use an item attribute value to increase/decrease the value of a character attribute.
    /// </summary>
    [System.Serializable]
    public class CharacterModifyAttributesItemAction : ItemAction
    {
        [Tooltip("The character attribute name to increase/decrease.")]
        [SerializeField] protected CharacterItemAttributePair[] m_CharacterItemAttributePairs;
        [Tooltip("Remove when used.")]
        [SerializeField] protected bool m_RemoveOnUse = true;
        [Tooltip("Can invoke will be false if the character attribute is maxed out.")]
        [SerializeField] protected bool m_CannotUseIfMax = true;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterModifyAttributesItemAction()
        {
            m_Name = "Heal";
        }
        
        /// <summary>
        /// Returns true if the item action be invoked.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The inventory user.</param>
        /// <returns>True if it can be invoked.</returns>
        protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var attributeManager = itemUser.gameObject.GetCachedComponent<AttributeManager>();
            if (attributeManager == null) {
                return false;
            }

            var allMaxed = true;
            for (int i = 0; i < m_CharacterItemAttributePairs.Length; i++) {
                var characterAttributeName = GetCharacterAttributeName(itemInfo, m_CharacterItemAttributePairs[i]);
                var characterAttribute = attributeManager.GetAttribute(characterAttributeName);

                if (characterAttribute == null) { continue; }

                if (characterAttribute.MaxValue != characterAttribute.Value) { allMaxed = false;}
            }

            if (m_CannotUseIfMax && allMaxed) {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Move an item from one collection to another.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var attributeManager = itemUser.gameObject.GetCachedComponent<AttributeManager>();

            for (int i = 0; i < m_CharacterItemAttributePairs.Length; i++) {
                var characterAttributeName = GetCharacterAttributeName(itemInfo, m_CharacterItemAttributePairs[i]);

                var characterAttribute = attributeManager.GetAttribute(characterAttributeName);
                if(characterAttribute == null){ continue; }
                
                var value = GetItemAttributeValue(itemInfo, m_CharacterItemAttributePairs[i]);

                characterAttribute.Value += value;
            }

            if (m_RemoveOnUse) {
                itemInfo.ItemCollection.RemoveItem((1, itemInfo));
            }
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>The value.</returns>
        private float GetItemAttributeValue(ItemInfo itemInfo, CharacterItemAttributePair characterItemAttributePair)
        {
            var valueItemAttributeName = characterItemAttributePair.ValueItemAttributeName;
            var defaultValue = characterItemAttributePair.DefaultValue;
            
            return itemInfo.Item.GetAttribute<Attribute<float>>(valueItemAttributeName)?.GetValue() ??
                   itemInfo.Item.GetAttribute<Attribute<int>>(valueItemAttributeName)?.GetValue() ?? defaultValue;
        }

        /// <summary>
        /// Get the name of the character attribute.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>The character attribute name.</returns>
        private string GetCharacterAttributeName(ItemInfo itemInfo, CharacterItemAttributePair characterItemAttributePair)
        {
            var nameItemAttributeName = characterItemAttributePair.NameItemAttributeName;
            var defaultCharacterAttribute = characterItemAttributePair.DefaultCharacterAttribute;
            return itemInfo.Item.GetAttribute<Attribute<string>>(nameItemAttributeName)?.GetValue() ??
                   defaultCharacterAttribute;
        }
    }
}