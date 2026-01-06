/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Demo
{
    using System.Collections.Generic;
#if THIRD_PERSON_CONTROLLER
    using Opsive.UltimateCharacterController.ThirdPersonController.Camera;
#endif
    using UnityEngine;

    /// <summary>
    /// Sets the color of the specified material.
    /// </summary>
    public class ColorSetter : MonoBehaviour
    {
        public Color Color {
            get { 
                if (m_LocalMaterials.Count == 0 && m_ItemMaterials.Count == 0) { return Color.white; }

                if (m_ItemMaterials.Count == 0) {
                    return m_LocalMaterials[0].color;
                }
                return m_ItemMaterials[0].color; 
            }
            set {
                for (int i = 0; i < m_LocalMaterials.Count; ++i) {
                    var material = m_LocalMaterials[i];
                    material.color = value;
                }
                for (int i = 0; i < m_ItemMaterials.Count; ++i) {
                    var material = m_ItemMaterials[i];
                    material.color = value;
                    
#if THIRD_PERSON_CONTROLLER
                    // The color must be updated on the Object fader otherwise the color will be reset to original.
                    if (m_ObjectFader != null) {
                        m_ObjectFader.UpdateMaterials(material,material);
                    }
#endif
                }
            }
        }

        private List<Material> m_ItemMaterials = new List<Material>();
        private List<Material> m_LocalMaterials = new List<Material>();
#if THIRD_PERSON_CONTROLLER
        private ObjectFader m_ObjectFader;
#endif

        /// <summary>
        /// Find the material that should be set.
        /// </summary>
        public void Awake()
        {
#if THIRD_PERSON_CONTROLLER
            var camera = Camera.main;
            if (camera != null) {
                m_ObjectFader = Camera.main.GetComponent<ObjectFader>();
            }
#endif

            var perspectiveItems = GetComponents<Items.PerspectiveItem>();
            for (int i = 0; i < perspectiveItems.Length; ++i) {
                var renderer = perspectiveItems[i].GetVisibleObject().GetComponent<MeshRenderer>();
                SetRendererMaterial(renderer, m_ItemMaterials);
            }

            var localRenderer = GetComponentInChildren<MeshRenderer>();
            SetRendererMaterial(localRenderer, m_LocalMaterials);
        }

        /// <summary>
        /// Set the material for the renderer.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="materials">The material list to add the material to.</param>
        protected void SetRendererMaterial(Renderer renderer, List<Material> materials)
        {
            if (renderer == null) { return; }

            materials.Add(renderer.material);
        }
    }
}