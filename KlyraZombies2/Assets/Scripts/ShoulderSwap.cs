using UnityEngine;
using Opsive.UltimateCharacterController.Camera;

public class ShoulderSwap : MonoBehaviour
{
    [Tooltip("The camera controller to modify.")]
    [SerializeField] private CameraController m_CameraController;

    [Tooltip("The key to swap shoulders.")]
    [SerializeField] private KeyCode m_SwapKey = KeyCode.X;

    private bool m_IsLeftShoulder;

    private void Start()
    {
        if (m_CameraController == null)
        {
            m_CameraController = GetComponent<CameraController>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_SwapKey))
        {
            m_IsLeftShoulder = !m_IsLeftShoulder;
        }
    }

    private void LateUpdate()
    {
        EnforceShoulderSide();
    }

    private void EnforceShoulderSide()
    {
        if (m_CameraController == null) return;

        var viewType = m_CameraController.ActiveViewType;
        var lookOffsetProperty = viewType.GetType().GetProperty("LookOffset");

        if (lookOffsetProperty != null)
        {
            var currentOffset = (Vector3)lookOffsetProperty.GetValue(viewType);

            bool needsFlip = (m_IsLeftShoulder && currentOffset.x > 0) ||
                             (!m_IsLeftShoulder && currentOffset.x < 0);

            if (needsFlip)
            {
                currentOffset.x = -currentOffset.x;
                lookOffsetProperty.SetValue(viewType, currentOffset);
            }
        }
    }
}
