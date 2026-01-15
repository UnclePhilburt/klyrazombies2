using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Pre-downloads Addressables chunks in the background during main menu.
/// Attach to Main Menu scene.
/// </summary>
public class AddressablesPreloader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject m_LoadingPanel;
    [SerializeField] private Slider m_ProgressBar;
    [SerializeField] private Text m_ProgressText;
    [SerializeField] private Button m_PlayButton;

    [Header("Settings")]
    [Tooltip("Pre-download all chunks on main menu?")]
    [SerializeField] private bool m_PreloadAllChunks = true;

    [Tooltip("Download silently (don't show loading UI)")]
    [SerializeField] private bool m_SilentDownload = false;

    [Tooltip("Chunks to pre-download (leave empty to download all)")]
    [SerializeField] private string[] m_ChunksToPreload;

    [Header("Config")]
    [SerializeField] private ChunkConfig m_Config;

    private bool m_IsDownloading = false;
    private bool m_DownloadComplete = false;
    private float m_TotalProgress = 0f;

    private void Start()
    {
        // Load config if not assigned
        if (m_Config == null)
        {
            m_Config = Resources.Load<ChunkConfig>("ChunkConfig");
        }

        // Disable play button until download complete
        if (m_PlayButton != null)
        {
            m_PlayButton.interactable = false;
        }

        // Hide loading panel initially if silent
        if (m_LoadingPanel != null && m_SilentDownload)
        {
            m_LoadingPanel.SetActive(false);
        }

        if (m_PreloadAllChunks)
        {
            StartCoroutine(PreloadChunks());
        }
        else
        {
            // No preload - enable play immediately
            OnDownloadComplete();
        }
    }

    private IEnumerator PreloadChunks()
    {
        m_IsDownloading = true;

        // Show loading UI if not silent
        if (m_LoadingPanel != null && !m_SilentDownload)
        {
            m_LoadingPanel.SetActive(true);
        }

        Debug.Log("[Preloader] Starting chunk pre-download...");

        // Get list of chunks to download
        List<string> chunksToDownload = GetChunkList();

        if (chunksToDownload.Count == 0)
        {
            Debug.LogWarning("[Preloader] No chunks to preload!");
            OnDownloadComplete();
            yield break;
        }

        // First, get download size
        var sizeHandle = Addressables.GetDownloadSizeAsync(chunksToDownload);
        yield return sizeHandle;

        if (sizeHandle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError($"[Preloader] Failed to get download size: {sizeHandle.OperationException}");
            OnDownloadComplete(); // Continue anyway
            yield break;
        }

        long totalSize = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        if (totalSize == 0)
        {
            Debug.Log("[Preloader] All chunks already cached!");
            OnDownloadComplete();
            yield break;
        }

        Debug.Log($"[Preloader] Downloading {FormatBytes(totalSize)} across {chunksToDownload.Count} chunks...");

        // Download chunks
        var downloadHandle = Addressables.DownloadDependenciesAsync(chunksToDownload, Addressables.MergeMode.Union);

        while (!downloadHandle.IsDone)
        {
            m_TotalProgress = downloadHandle.PercentComplete;

            // Update UI
            UpdateProgressUI(m_TotalProgress, totalSize);

            yield return null;
        }

        if (downloadHandle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError($"[Preloader] Download failed: {downloadHandle.OperationException}");
        }
        else
        {
            Debug.Log("[Preloader] All chunks downloaded successfully!");
        }

        // Don't release the handle - we want chunks to stay loaded
        // Addressables.Release(downloadHandle);

        OnDownloadComplete();
    }

    private List<string> GetChunkList()
    {
        List<string> chunks = new List<string>();

        // Use custom list if provided
        if (m_ChunksToPreload != null && m_ChunksToPreload.Length > 0)
        {
            chunks.AddRange(m_ChunksToPreload);
            return chunks;
        }

        // Otherwise, download ALL chunks
        if (m_Config != null)
        {
            for (int x = 0; x < m_Config.gridSizeX; x++)
            {
                for (int z = 0; z < m_Config.gridSizeZ; z++)
                {
                    string sceneName = m_Config.GetChunkSceneName(x, z);
                    chunks.Add(sceneName);
                }
            }
        }

        return chunks;
    }

    private void UpdateProgressUI(float progress, long totalBytes)
    {
        // Update progress bar
        if (m_ProgressBar != null)
        {
            m_ProgressBar.value = progress;
        }

        // Update text
        if (m_ProgressText != null)
        {
            long downloadedBytes = (long)(totalBytes * progress);
            m_ProgressText.text = $"Downloading... {FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)} ({progress * 100:F0}%)";
        }
    }

    private void OnDownloadComplete()
    {
        m_IsDownloading = false;
        m_DownloadComplete = true;

        Debug.Log("[Preloader] Download complete!");

        // Hide loading panel
        if (m_LoadingPanel != null)
        {
            m_LoadingPanel.SetActive(false);
        }

        // Enable play button
        if (m_PlayButton != null)
        {
            m_PlayButton.interactable = true;
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }

    // Public method to check if download is complete (for Play button logic)
    public bool IsReady()
    {
        return m_DownloadComplete;
    }

    public float GetProgress()
    {
        return m_TotalProgress;
    }
}
