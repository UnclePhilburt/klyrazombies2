/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Inventory;
    using Opsive.UltimateInventorySystem.Core;
    using System;
    using UnityEngine;

    /// <summary>
    /// The AmmoData is used to contain the information of the ammo stored in a weapon.
    /// </summary>
    [Serializable]
    public struct AmmoData
    {
        [Tooltip("The Ammo ItemDefinition.")]
        [SerializeField] private DynamicItemDefinition m_ItemDefinition;
        [Tooltip("The size of the clip.")]
        [SerializeField] private int m_ClipSize;
        [Tooltip("The number of items remaining in the clip.")]
        [SerializeField] private int m_ClipRemaining;

        public ItemDefinition ItemDefinition => m_ItemDefinition;
        public int ClipSize => m_ClipSize;
        public int ClipRemaining => m_ClipRemaining;
        public static AmmoData None => new AmmoData(null, -1, -1);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="itemDefinition">The ammo item definition.</param>
        /// <param name="clipSize">The clip size.</param>
        /// <param name="clipRemaining">The remaining amount.</param>
        public AmmoData(ItemDefinition itemDefinition, int clipSize, int clipRemaining)
        {
            m_ItemDefinition = itemDefinition;
            m_ClipSize = clipSize;
            m_ClipRemaining = clipRemaining;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="itemDefinition">The ammo item definition.</param>
        /// <param name="clipSize">The clip size.</param>
        /// <param name="clipRemaining">The remaining amount.</param>
        public AmmoData(ItemDefinitionBase itemDefinition, int clipSize, int clipRemaining)
        {
            m_ItemDefinition = itemDefinition as ItemDefinition;
            m_ClipSize = clipSize;
            m_ClipRemaining = clipRemaining;
        }

        /// <summary>
        /// Constructor, copy.
        /// </summary>
        /// <param name="other">The other ammo data amount to copy.</param>
        public AmmoData(AmmoData other)
        {
            m_ItemDefinition = other.ItemDefinition;
            m_ClipSize = other.ClipSize;
            m_ClipRemaining = other.ClipRemaining;
        }

        /// <summary>
        /// The AmmoData the compare the value to.
        /// </summary>
        /// <param name="other">THe ammo data to compare the value to.</param>
        /// <returns>True if they are equivalent.</returns>
        public bool Equals(AmmoData other)
        {
            return m_ItemDefinition.Value == other.m_ItemDefinition.Value
                   && m_ClipSize == other.ClipSize
                   && m_ClipRemaining == other.ClipRemaining;
        }

        /// <summary>
        /// To string.
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            var originalName = m_ItemDefinition.OriginalSerializedName;

            string definitionName = m_ItemDefinition.HasValue ? m_ItemDefinition.Value.name :
                string.IsNullOrWhiteSpace(originalName) ? "NULL" :
                originalName;
            return $"{ClipRemaining}/{ClipSize} {definitionName}";
        }
    }
}