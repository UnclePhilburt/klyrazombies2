/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Impact
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Traits.Damage;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    [CreateAssetMenu(fileName = "ItemNeutralizingDamageProcessor",
        menuName = "Ultimate Character Controller/Damage Processors/Item Neutralizing Damage Processor")]
    public class ItemNeutralizingDamageProcessor : DamageProcessor
    {
        [SerializeField] protected string[] m_ItemCollectionNames;
        [SerializeField] protected DynamicItemDefinition[] m_NeutralizingItemDefinitions;

        public override void Process(IDamageTarget target, DamageData damageData)
        {
            damageData.Amount *= 1;
            var targetInventory = target.Owner.GetCachedComponent<Inventory>();


            if (targetInventory == null) {
                target.Damage(damageData);
                return;
            }

            for (int i = 0; i < m_ItemCollectionNames.Length; i++) {
                var itemCollection = targetInventory.GetItemCollection(m_ItemCollectionNames[i]);
                if (itemCollection == null) { continue; }

                for (int j = 0; j < m_NeutralizingItemDefinitions.Length; j++) {
                    if (itemCollection.HasItem((1, m_NeutralizingItemDefinitions[j]), false, false)) {
                        //No Damage.
                        return;
                    }
                }
            }

            target.Damage(damageData);
        }
    }
}