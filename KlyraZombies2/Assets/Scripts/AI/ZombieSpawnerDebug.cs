using UnityEngine;
using System.Linq;

/// <summary>
/// Runtime debug display for zombie spawners.
/// Add this component to a spawner to see why it's not spawning.
/// </summary>
[RequireComponent(typeof(ZombieSpawner))]
public class ZombieSpawnerDebug : MonoBehaviour
{
    private ZombieSpawner m_Spawner;
    private bool m_ShowDebug = true;

    private void Awake()
    {
        m_Spawner = GetComponent<ZombieSpawner>();
    }

    private void OnGUI()
    {
        if (!m_ShowDebug) return;

        // Find screen position of spawner
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

        // Only show if in front of camera and close enough
        if (screenPos.z > 0 && screenPos.z < 100f)
        {
            screenPos.y = Screen.height - screenPos.y; // Flip Y

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 10;
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(screenPos.x - 100, screenPos.y - 100, 200, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>{gameObject.name}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });

            // Scene
            GUILayout.Label($"Scene: {gameObject.scene.name}");

            // Active state
            GUI.color = gameObject.activeInHierarchy ? Color.green : Color.red;
            GUILayout.Label($"Active: {gameObject.activeInHierarchy}");
            GUI.color = Color.white;

            // Enabled state
            GUI.color = m_Spawner.enabled ? Color.green : Color.red;
            GUILayout.Label($"Enabled: {m_Spawner.enabled}");
            GUI.color = Color.white;

            // Player distance
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                GUILayout.Label($"Player Distance: {distance:F1}m");
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("Player: NOT FOUND");
                GUI.color = Color.white;
            }

            // Chunk status
            ChunkLoader loader = ChunkLoader.Instance;
            if (loader != null && loader.Config != null)
            {
                Vector2Int myChunk = loader.Config.WorldToChunk(transform.position);
                string chunkName = loader.Config.GetChunkSceneName(myChunk);
                bool isLoaded = loader.LoadedChunks.Contains(chunkName);

                GUI.color = isLoaded ? Color.green : Color.red;
                GUILayout.Label($"Chunk: {chunkName}");
                GUILayout.Label($"Loaded: {isLoaded}");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("Chunk: No ChunkLoader");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private void OnDrawGizmos()
    {
        // Draw spawner location
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 10f);
    }
}
