/// ---------------------------------------------
/// Ultimate Character Controller
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
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Use an item attribute value to increase/decrease the value of a character attribute.
    /// </summary>
    [System.Serializable]
    public class CharacterModifyAttributeItemAction : ItemAction
    {
        [FormerlySerializedAs("m_CharacterAttributeName")]
        [Tooltip("The character attribute name to increase/decrease.")]
        [SerializeField] protected string m_DefaultCharacterAttribute = "Health";
        [Tooltip("The value used to increase/decrease.")]
        [SerializeField] protected float m_DefaultValue = 0;
        [Tooltip("The item attribute name of the character attribute name.")]
        [SerializeField] protected string m_NameItemAttributeName = "Attribute";
        [FormerlySerializedAs("m_ItemAttributeName")]
        [Tooltip("The attribute name used to get the value.")]
        [SerializeField] protected string m_ValueItemAttributeName = "Heal";
        [Tooltip("Remove when used.")]
        [SerializeField] protected bool m_RemoveOnUse = true;
        [Tooltip("Can invoke will be false if the character attribute is maxed out.")]
        [SerializeField] protected bool m_CannotUseIfMax = true;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterModifyAttributeItemAction()
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

            var characterAttributeName = GetCharacterAttributeName(itemInfo);
            var characterAttribute = attributeManager.GetAttribute(characterAttributeName);

            if (characterAttribute == null) { return false; }

            if (m_CannotUseIfMax && characterAttribute.MaxValue == characterAttribute.Value) { return false;}

            return itemInfo.Item.HasAttribute(m_ValueItemAttributeName);
        }

        /// <summary>
        /// Move an item from one collection to another.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var attributeManager = itemUser.gameObject.GetCachedComponent<AttributeManager>();
            var characterAttributeName = GetCharacterAttributeName(itemInfo);
            
            var characterAttribute = attributeManager.GetAttribute(characterAttributeName);
            var value = GetItemAttributeValue(itemInfo);
            
            characterAttribute.Value += value;

            if (m_RemoveOnUse) {
                itemInfo.ItemCollection.RemoveItem((1, itemInfo));
            }
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>The value.</returns>
        private float GetItemAttributeValue(ItemInfo itemInfo)
        {
            return itemInfo.Item.GetAttribute<Attribute<float>>(m_ValueItemAttributeName)?.GetValue() ??
                   itemInfo.Item.GetAttribute<Attribute<int>>(m_ValueItemAttributeName)?.GetValue() ?? m_DefaultValue;
        }

        /// <summary>
        /// Get the name of the character attribute.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>The character attribute name.</returns>
        private string GetCharacterAttributeName(ItemInfo itemInfo)
        {
            return itemInfo.Item.GetAttribute<Attribute<string>>(m_NameItemAttributeName)?.GetValue() ??
                   m_DefaultCharacterAttribute;
        }
    }
}