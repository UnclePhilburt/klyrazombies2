using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CrosshairSetup : EditorWindow
{
    [MenuItem("Project Klyra/Weapons/Create Crosshair")]
    public static void CreateCrosshair()
    {
        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");
        }

        // Check if crosshair already exists
        Crosshair existing = Object.FindFirstObjectByType<Crosshair>();
        if (existing != null)
        {
            Debug.Log("Crosshair already exists!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create crosshair
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = crosshairObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(6, 6);

        crosshairObj.AddComponent<Crosshair>();

        Undo.RegisterCreatedObjectUndo(crosshairObj, "Create Crosshair");
        Selection.activeGameObject = crosshairObj;

        Debug.Log("Crosshair created!");
    }
}
