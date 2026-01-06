/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Demo.References
{
    using System;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    [Serializable]
    public class InventoryItemReferenceObjects
    {
        [SerializeField] private ItemDefinition[] m_Definitions;
        [SerializeField] private ItemCategory[] m_Categories;
        
        public ItemDefinition[] Definitions => m_Definitions;
        public ItemCategory[] Categories => m_Categories;
    }
    
    /// <summary>
    /// Helper class which references the objects.
    /// </summary>
    public class InventoryItemReferences : MonoBehaviour
    {
        [Tooltip("The Inventories where these items should be removed.")]
        [SerializeField] protected Inventory[] m_Inventories;
        [Tooltip("A reference to the first person objects.")]
        [SerializeField] protected InventoryItemReferenceObjects m_FirstPersonObjects;
        [Tooltip("A reference to the third person objects.")]
        [SerializeField] protected InventoryItemReferenceObjects m_ThirdPersonObjects;
        [Tooltip("A reference to the shooter objects.")]
        [SerializeField] protected InventoryItemReferenceObjects m_ShooterObjects;
        [Tooltip("A reference to the melee objects.")]
        [SerializeField] protected InventoryItemReferenceObjects m_MeleeObjects;

        public Inventory[] Inventories { get => m_Inventories; set => m_Inventories = value; }
        public InventoryItemReferenceObjects FirstPersonObjects { get { return m_FirstPersonObjects; } set { m_FirstPersonObjects = value; } }
        public InventoryItemReferenceObjects ThirdPersonObjects { get { return m_ThirdPersonObjects; } set { m_ThirdPersonObjects = value; } }
        public InventoryItemReferenceObjects ShooterObjects { get { return m_ShooterObjects; } set { m_ShooterObjects = value; } }
        public InventoryItemReferenceObjects MeleeObjects { get { return m_MeleeObjects; } set { m_MeleeObjects = value; } }
    }
}