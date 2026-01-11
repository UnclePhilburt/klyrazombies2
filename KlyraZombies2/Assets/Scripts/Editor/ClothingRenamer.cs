using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Renames clothing items to use working Apocalypse preset names.
/// Run from: Tools > Inventory > Rename Clothing to Apocalypse Presets
/// </summary>
public class ClothingRenamer : EditorWindow
{
    private const string CLOTHING_FOLDER = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";

    [MenuItem("Tools/Inventory/Rename Clothing to Apocalypse Presets")]
    public static void RenameClothingItems()
    {
        string[] assetFiles = Directory.GetFiles(CLOTHING_FOLDER, "*.asset", SearchOption.TopDirectoryOnly);

        int renamedCount = 0;
        int survivorCount = 0;
        int outlawCount = 0;

        foreach (string filePath in assetFiles)
        {
            if (filePath.EndsWith(".meta")) continue;

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string newName = null;

            // Check if it's a Modern Civilians item
            var match = Regex.Match(fileName, @"Modern Civilians (\d+)");
            if (match.Success)
            {
                int num = int.Parse(match.Groups[1].Value);

                // Map to Apocalypse presets:
                // 01-05 -> Apocalypse Survivor 01-05
                // 06-10 -> Apocalypse Outlaws 01-05
                // 11-15 -> Apocalypse Outlaws 06-10
                if (num <= 5)
                {
                    newName = $"Apocalypse Survivor {num:D2}";
                    survivorCount++;
                }
                else if (num <= 15)
                {
                    int outlawNum = ((num - 1) % 10) + 1;
                    newName = $"Apocalypse Outlaws {outlawNum:D2}";
                    outlawCount++;
                }
                else
                {
                    // Wrap around for higher numbers
                    int outlawNum = ((num - 1) % 10) + 1;
                    newName = $"Apocalypse Outlaws {outlawNum:D2}";
                    outlawCount++;
                }
            }

            if (newName != null && newName != fileName)
            {
                // Update the m_Name field inside the asset file
                string content = File.ReadAllText(filePath);
                content = Regex.Replace(content, @"m_Name: .*", $"m_Name: {newName}");
                File.WriteAllText(filePath, content);

                // Rename the file itself
                string directory = Path.GetDirectoryName(filePath);
                string newPath = Path.Combine(directory, newName + ".asset");

                // Check if target already exists
                if (File.Exists(newPath))
                {
                    Debug.LogWarning($"[ClothingRenamer] Skipping {fileName} - {newName} already exists");
                    continue;
                }

                // Rename asset file and meta file
                string metaPath = filePath + ".meta";
                string newMetaPath = newPath + ".meta";

                File.Move(filePath, newPath);
                if (File.Exists(metaPath))
                {
                    File.Move(metaPath, newMetaPath);
                }

                Debug.Log($"[ClothingRenamer] Renamed: {fileName} -> {newName}");
                renamedCount++;
            }
        }

        AssetDatabase.Refresh();

        string message = $"Renamed {renamedCount} items:\n" +
                        $"- {survivorCount} to Apocalypse Survivor\n" +
                        $"- {outlawCount} to Apocalypse Outlaws";

        Debug.Log($"[ClothingRenamer] {message}");
        EditorUtility.DisplayDialog("Clothing Renamed", message, "OK");
    }
}
