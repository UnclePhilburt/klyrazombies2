/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Demo
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Traits;
    using Opsive.UltimateCharacterController.Traits.Damage;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using UnityEngine;

    /// <summary>
    /// This damage processor is an example of how one can make a custom damage processor to customize how damage is computed.
    /// In the demo we use the DamageProcessor on the Character using the DamageProcessorModule component.
    /// The SimpleDamage impact action will grab it from the character (if one is not specified on the weapon) and use it.
    ///
    /// This Damage Processor will use a Character Attribute on the Character to modify the damage.
    /// </summary>
    [CreateAssetMenu(fileName = "Integration DemoDamage Processor",
        menuName = "Ultimate Character Controller/Damage Processors/Integration Demo Damage Processor")]
    public class IntegrationDemoDamageProcessor : DamageProcessor
    {
        [SerializeField] protected bool m_Enable = true;
        [SerializeField] protected string DamageMultiplierCharacterAttributeName = "DamageMultiplier";
        [SerializeField] protected string DamageMultiplierItemAttributeName = "DamageMultiplier";
        [SerializeField] protected float m_RandomMultiplierOffsetPercentage = 15f;

        /// <summary>
        /// Processes the DamageData on the DamageTarget.
        /// </summary>
        /// <param name="target">The object receiving the damage.</param>
        /// <param name="damageData">The damage data to be applied to the target.</param>
        public override void Process(IDamageTarget target, DamageData damageData)
        {
            // Ignore if disabled.
            if (m_Enable == false) {
                base.Process(target, damageData);
                return;
            }
            
            // The Damage Data coming in will always be in it's default state.
            // That means in here we recompute the damage on each hit.
            // That's the purpose the the damage processor.
            

            // Check that the source owner is a character for a damage multiplier.
            var characterGameObject = damageData.DamageSource.SourceOwner;
            var multiplier =
                GetCharacterAttributeValue(characterGameObject, DamageMultiplierCharacterAttributeName);


            // Check that the item for a damage multiplier.
            var itemGameObject = damageData.ImpactContext.CharacterItemAction?.gameObject;
            var itemDamageMultiplier = GetItemAttributeValue(itemGameObject, DamageMultiplierItemAttributeName);

            multiplier *= itemDamageMultiplier;

            // Set the random damage offset
            var randomOffsetPercentage =
                Random.Range(-m_RandomMultiplierOffsetPercentage, m_RandomMultiplierOffsetPercentage) / 100f;
            multiplier += (randomOffsetPercentage * multiplier);

            // Use the multiplier to change the damageAmount.
            // The damage amount coming in from damageData will usually be the one set in the SimpleDamage ImpactAction.
            // Although it could be some other value depending of if the contextData was changed before SimpleDamage is executed.
            // Here we do not care was the damage amount really is or where it came from, all we want is to multiply it.
            // (It is totally valid to ignore the damage amount and set your own value from scratch here if you wanted to).
            damageData.Amount *= multiplier;

            // process that damage as expected
            // This is equivalent to calling:  "target.Damage(damageData);"
            base.Process(target, damageData);

            // NOTE: if you prefer you could have a component on your player and have that process the damage.
            // Simply use this scriptable object to find that component on the player.
            // (the same way we found the AttributeManager in the function below).
            // And call a function on it to process the damage.
        }

        /// <summary>
        /// Get the attribute value on the character gameobject.
        /// </summary>
        /// <param name="characterGameObject">The Character GameObject.</param>
        /// <param name="attributeName">The character Attribute Name.</param>
        /// <returns></returns>
        protected float GetCharacterAttributeValue(GameObject characterGameObject, string attributeName)
        {
            //Check that the source owner is a character
            if (characterGameObject == null) {
                // Make sure to damage normally.
                return 1;
            }

            var attributeManager = characterGameObject.GetCachedComponent<AttributeManager>();
            if (attributeManager == null) {
                // Make sure to damage normally.
                return 1;
            }

            var multiplierAttribute = attributeManager.GetAttribute(attributeName);
            if (multiplierAttribute == null) {
                // Make sure to damage normally.
                return 1;
            }

            return multiplierAttribute.Value;
        }

        /// <summary>
        /// Get the item attribute value.
        /// </summary>
        /// <param name="itemGameObject">The Item GameObject.</param>
        /// <param name="attributeName">The Item Attribute Name.</param>
        /// <returns>The damage multiplier.</returns>
        protected float GetItemAttributeValue(GameObject itemGameObject, string attributeName)
        {
            //Check that the source owner is a character
            if (itemGameObject == null) {
                // Make sure to damage normally.
                return 1;
            }

            var itemObject = itemGameObject.GetCachedComponent<ItemObject>();
            if (itemObject == null) {
                // Make sure to damage normally.
                return 1;
            }

            var multiplierAttribute = itemObject.Item.GetAttribute<Attribute<float>>(attributeName);
            if (multiplierAttribute == null) {
                // Make sure to damage normally.
                return 1;
            }

            return multiplierAttribute.GetValue();
        }
    }
}