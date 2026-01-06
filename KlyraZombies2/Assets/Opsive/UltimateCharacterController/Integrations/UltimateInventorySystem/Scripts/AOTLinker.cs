/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
	using UnityEngine;

	/// <summary>
	/// Created class generated to avoid AOT linker errors.
	/// This class will not be called at runtime and does not affect performance.
	/// </summary>
	public class AOTLinker : MonoBehaviour
	{
		public void Linker()
		{
#pragma warning disable 0219
			new Opsive.UltimateInventorySystem.Core.AttributeSystem.Attribute<AmmoData>();
			new Opsive.UltimateInventorySystem.Core.AttributeBinding<AmmoData>();
			new Opsive.UltimateInventorySystem.Core.GenericAttributeBinding<AmmoData>();
			new Opsive.UltimateInventorySystem.Core.AttributeSystem.Attribute<AttributeBindingData>();
			new Opsive.UltimateInventorySystem.Core.AttributeBinding<AttributeBindingData>();
			new Opsive.UltimateInventorySystem.Core.GenericAttributeBinding<AttributeBindingData>();
			new Opsive.Shared.StateSystem.Preset.GenericDelegate<AmmoData>();
			new Opsive.UltimateInventorySystem.Core.AttributeBinding<AmmoData>();
			new Opsive.UltimateInventorySystem.Core.GenericAttributeBinding<AmmoData>();
			var ammoDataFuncDelegate = new System.Func<AmmoData>(() => { return new AmmoData(); });
			var ammoDataActionDelegate = new System.Action<AmmoData>((AmmoData value) => { });
			new Opsive.Shared.StateSystem.Preset.GenericDelegate<AttributeBindingData>();
			new Opsive.UltimateInventorySystem.Core.AttributeBinding<AttributeBindingData>();
			new Opsive.UltimateInventorySystem.Core.GenericAttributeBinding<AttributeBindingData>();
			var ammoBindingDataFuncDelegate = new System.Func<AttributeBindingData>(() => { return new AttributeBindingData(); });
			var ammoBindingDataActionDelegate = new System.Action<AttributeBindingData>((AttributeBindingData value) => { });
#pragma warning restore 0219
		}
	}
}