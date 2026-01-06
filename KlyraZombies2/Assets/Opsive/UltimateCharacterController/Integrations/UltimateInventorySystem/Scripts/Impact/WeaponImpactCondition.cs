/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Impact
{
    using System;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Items.Actions.Impact;
    using Opsive.UltimateCharacterController.Objects;
    using Opsive.UltimateInventorySystem.Core;
    using UnityEngine;
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public class CheckWeaponSourceItemCategory : ImpactActionCondition
    {
        [Tooltip("Allow impact if the source does not have a character item action?")]
        [SerializeField] protected bool m_AllowNonWeaponImpact;
        [Tooltip("if false, then the impact will pass if the weapon inherits the category, if true it will fail.")]
        [SerializeField] protected bool m_ExcludeCategory;
        [Tooltip("The item categories to check.")]
        [SerializeField] protected DynamicItemCategoryArray m_ItemCategories;
        
        /// <summary>
        /// Internal, Can the impact proceed from the context.
        /// </summary>
        /// <param name="ctx">Context about the hit.</param>
        /// <returns>True if the impact should proceed.</returns>
        protected override bool CanImpactInternal(ImpactCallbackContext ctx)
        {
            var characterItem = ctx.CharacterItemAction?.CharacterItem;

            if (characterItem == null) {
                return m_AllowNonWeaponImpact;
            }

            var itemCategories = m_ItemCategories.Value;
            for (int i = 0; i < itemCategories.Length; i++) {
                if (itemCategories[i].InherentlyContains(characterItem.ItemIdentifier) == true) {
                    return !m_ExcludeCategory;
                }
            }

            return m_ExcludeCategory;
        }
    }
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public class CheckWeaponSourceItemDefinition : ImpactActionCondition
    {
        [Tooltip("Allow impact if the source does not have a character item action?")]
        [SerializeField] protected bool m_AllowNonWeaponImpact;
        [Tooltip("if false, then the impact will pass if the weapon inherits the definition, if true it will fail.")]
        [SerializeField] protected bool m_ExcludeDefinition;
        [Tooltip("The item definitions to check.")]
        [SerializeField] protected DynamicItemDefinitionArray m_ItemDefinitions;
        
        /// <summary>
        /// Internal, Can the impact proceed from the context.
        /// </summary>
        /// <param name="ctx">Context about the hit.</param>
        /// <returns>True if the impact should proceed.</returns>
        protected override bool CanImpactInternal(ImpactCallbackContext ctx)
        {
            var characterItem = ctx.CharacterItemAction?.CharacterItem;

            if (characterItem == null) {
                return m_AllowNonWeaponImpact;
            }

            var itemDefinitions = m_ItemDefinitions.Value;
            for (int i = 0; i < itemDefinitions.Length; i++) {
                if (itemDefinitions[i].InherentlyContains(characterItem.ItemIdentifier) == true) {
                    return !m_ExcludeDefinition;
                }
            }

            return m_ExcludeDefinition;
        }
    }

    [Serializable]
    public struct ItemAttributeValueComparator
    {
        public enum ValueComparision
        {
            Equal,
            NotEqual,
            Smaller,
            SmallerOrEqual,
            Bigger,
            BiggerOrEqual
        }
        
        [Tooltip("The attribute name to search for.")]
        [SerializeField] private bool m_PassIfAttributeNotFound;
        [Tooltip("The attribute name to search for.")]
        [SerializeField] private string m_AttributeName;
        [Tooltip("The comparision to do: attribute *op* amount.")] 
        [SerializeField] private ValueComparision m_AttributeIs;
        [Tooltip("The amount to compare against.")]
        [SerializeField] private float m_Value;

        private const float c_comparisionTolerance = 0.001f;
        
        public bool CompareItem(Item item)
        {
            var attribute = item.GetAttribute(m_AttributeName);
            if (attribute == null) {
                return m_PassIfAttributeNotFound;
            }

            var valueAsObject = attribute.GetValueAsObject();
            var foundMatch = false;
            var attributeValue = 0f;
            if (valueAsObject is int) {
                attributeValue = Convert.ToInt32(valueAsObject);
                foundMatch = true;
            }else if (valueAsObject is float) {
                attributeValue = Convert.ToSingle(valueAsObject);
                foundMatch = true;
            }else if (valueAsObject is Enum) {
                attributeValue = Convert.ToInt32(valueAsObject);
                foundMatch = true;
            }

            if (foundMatch == false) {
                return m_PassIfAttributeNotFound;
            }

            switch (m_AttributeIs) {
                case ValueComparision.Equal:
                    return Math.Abs(attributeValue - m_Value) < c_comparisionTolerance;
                case ValueComparision.NotEqual:
                    return Math.Abs(attributeValue - m_Value) > c_comparisionTolerance;
                case ValueComparision.Smaller:
                    return attributeValue < m_Value;
                case ValueComparision.SmallerOrEqual:
                    return attributeValue <= m_Value;
                case ValueComparision.Bigger:
                    return attributeValue > m_Value;
                case ValueComparision.BiggerOrEqual:
                    return attributeValue >= m_Value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public class CheckWeaponSourceItemAttributeAmountValue : ImpactActionCondition
    {
        
        
        [Tooltip("Allow impact if the source does not have a character item action?")]
        [SerializeField] protected bool m_AllowNonWeaponImpact;
        [Tooltip("Allow impact if the source does not have the item attribute?")]
        [SerializeField] protected bool m_AllowIfAttributeNotFound;
        [Tooltip("The item attribute comparator")]
        [SerializeField] protected ItemAttributeValueComparator m_AttributeComparator;
        
        /// <summary>
        /// Internal, Can the impact proceed from the context.
        /// </summary>
        /// <param name="ctx">Context about the hit.</param>
        /// <returns>True if the impact should proceed.</returns>
        protected override bool CanImpactInternal(ImpactCallbackContext ctx)
        {
            var characterItem = ctx.CharacterItemAction?.CharacterItem;

            if (characterItem == null) {
                return m_AllowNonWeaponImpact;
            }

            var item = characterItem.ItemIdentifier as Item;
            if (item == null) {
                return m_AllowIfAttributeNotFound;
            }

            return m_AttributeComparator.CompareItem(item);
        }
    }
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public abstract class ItemProjectileImpactActionCondition : ProjectileImpactActionCondition
    {
        [Tooltip("Allow impact if the source does not have a character item action?")]
        [SerializeField] protected bool m_AllowNonItemProjectileImpact;

        /// <summary>
        /// Can the projectile impact?
        /// </summary>
        /// <param name="ctx">The impact context.</param>
        /// <param name="projectile">The projectile.</param>
        /// <returns>True if the projectile can impact</returns>
        protected override bool CanProjectileImpact(ImpactCallbackContext ctx, Projectile projectile)
        {
            var result =  base.CanProjectileImpact(ctx, projectile);
            if (result == false) {
                return false;
            }
            
            var itemObject = projectile.gameObject.GetCachedComponent<ItemObject>();

            if (itemObject == null || itemObject.Item == null) {
                Debug.LogWarning($"The projectile object {projectile} does not have an ItemObject component.",
                    projectile.gameObject);
                return m_AllowNonItemProjectileImpact;
            }

            return CanItemProjectileImpact(ctx, projectile, itemObject.Item);
        }

        protected abstract bool CanItemProjectileImpact(ImpactCallbackContext ctx, Projectile projectile, Item item);
    }
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public class CheckProjectileItemCategoryDefinition : ItemProjectileImpactActionCondition
    {
        [Tooltip("if false, then the impact will pass if the weapon inherits the category, if true it will fail.")]
        [SerializeField] protected bool m_ExcludeCategory;
        [Tooltip("The item categories to check.")]
        [SerializeField] protected DynamicItemCategoryArray m_ItemCategories;
        [Tooltip("if false, then the impact will pass if the weapon inherits the definition, if true it will fail.")]
        [SerializeField] protected bool m_ExcludeDefinition;
        [Tooltip("The item definitions to check.")]
        [SerializeField] protected DynamicItemDefinitionArray m_ItemDefinitions;

        protected override bool CanItemProjectileImpact(ImpactCallbackContext ctx, Projectile projectile, Item item)
        {
            if (CheckItemCategories(item) == false) {
                return false;
            }

            if (CheckItemDefinitions(item) == false) {
                return false;
            }

            return true;
        }

        private bool CheckItemCategories(Item item)
        {
            var itemCategories = m_ItemDefinitions.Value;
            for (int i = 0; i < itemCategories.Length; i++) {
                if (itemCategories[i].InherentlyContains(item) == true) {
                    return !m_ExcludeCategory;
                }
            }

            return m_ExcludeCategory;
        }
        
        private bool CheckItemDefinitions(Item item)
        {
            var itemDefinitions = m_ItemDefinitions.Value;
            for (int i = 0; i < itemDefinitions.Length; i++) {
                if (itemDefinitions[i].InherentlyContains(item) == true) {
                    return !m_ExcludeDefinition;
                }
            }

            return m_ExcludeDefinition;
        }
    }
    
    /// <summary>
    /// Checks the ObjectIdentifier on the target gameobject.
    /// </summary>
    [Serializable]
    public class CheckProjectileItemAttributeAmountValue : ItemProjectileImpactActionCondition
    {
        [Tooltip("The item attribute comparator")]
        [SerializeField] protected ItemAttributeValueComparator m_AttributeComparator;

        protected override bool CanItemProjectileImpact(ImpactCallbackContext ctx, Projectile projectile, Item item)
        {
            return m_AttributeComparator.CompareItem(item);
        }
    }
   
}


