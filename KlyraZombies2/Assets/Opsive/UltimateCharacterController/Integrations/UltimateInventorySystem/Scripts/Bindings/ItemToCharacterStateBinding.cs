/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    
    using System.Collections.Generic;
    using Opsive.Shared.StateSystem;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;
    
    /// <summary>
    /// A component used to set states on the character when items are part of a specific ItemCollection.
    /// The state is defined in an Item attribute.
    /// </summary>
    public class ItemToCharacterStateBinding : MonoBehaviour
    {
        [Tooltip("The Character Inventory bridge.")]
        [SerializeField] protected CharacterInventoryBridge m_InventoryBridge;
        [Tooltip("The Item Collection Name that should be monitored.")]
        [SerializeField] protected string m_ItemCollectionName;
        [Header("Allow comma seperated for multiple states")]
        [Tooltip("The Item attribute name with the state name.")]
        [SerializeField] protected string m_StateAttributeName;

        protected ItemCollection m_ItemCollection;

        protected List<string> m_ItemStates = new List<string>();
        protected List<string> m_PreviousItemStates = new List<string>();

        public IReadOnlyList<string> ItemStates => m_ItemStates;

        /// <summary>
        /// Initialize the component and set the default states.
        /// </summary>
        private void Start()
        {
            if (m_InventoryBridge == null) {
                m_InventoryBridge = GetComponent<CharacterInventoryBridge>();
            }

            m_ItemCollection = m_InventoryBridge.Inventory.GetItemCollection(m_ItemCollectionName);
            if (m_ItemCollection == null) {
                Debug.LogError($"The ItemCollection named '{m_ItemCollectionName}' could not be found", gameObject);
                return;
            }

            m_ItemCollection.OnItemCollectionUpdate += OnItemCollectionUpdate;

            OnItemCollectionUpdate();
        }

        /// <summary>
        /// Check if the state changed when the item collection changed.
        /// </summary>
        protected virtual void OnItemCollectionUpdate()
        {
        
            m_ItemStates.Clear();
            var itemStacks = m_ItemCollection.GetAllItemStacks();
            for (int i = 0; i < itemStacks.Count; i++) {
                var itemStack = itemStacks[i];
                if (itemStack != null && itemStack.Item != null &&
                    itemStack.Item.TryGetAttributeValue(m_StateAttributeName, out string fullStateName)) {

                    var states = fullStateName.Split(',');

                    for (int j = 0; j < states.Length; j++) {
                        var stateName = states[j].Trim();
                        m_ItemStates.Add(stateName);
                        Debug.Log(stateName);
                        StateManager.SetState(m_InventoryBridge.gameObject, stateName, true);
                    }
                }
            }

            for (int i = 0; i < m_PreviousItemStates.Count; i++) {
                var stateName = m_PreviousItemStates[i];
                var activeState = m_ItemStates.Contains(m_PreviousItemStates[i]);
                StateManager.SetState(m_InventoryBridge.gameObject, stateName, activeState);
            }
        
            m_PreviousItemStates.Clear();
            m_PreviousItemStates.AddRange(m_ItemStates);
        }
    }
}