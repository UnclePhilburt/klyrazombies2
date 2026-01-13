using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.Shared.Input;
using TMPro;

/// <summary>
/// Self-contained simple inventory UI. Generates everything through code.
/// Just add this to an empty GameObject - it creates all UI automatically.
/// </summary>
public class SimpleInventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode m_ToggleKey = KeyCode.Tab;
    [SerializeField] private int m_BaseSlots = 4; // Slots without backpack

    [Header("Inventory Grid Layout")]
    [SerializeField] private int m_GridColumns = 4;
    [SerializeField] private int m_MaxGridRows = 4;
    [SerializeField] private float m_SlotSize = 79.2f;
    [SerializeField] private float m_SlotSpacing = 16.3f;
    [SerializeField] private float m_NameHeight = 18.8f;

    [Header("Inventory Grid Position")]
    [SerializeField] private Vector2 m_GridOffset = new Vector2(-20f, 0f);

    [Header("Equipment Slots Layout")]
    [SerializeField] private float m_EquipSlotSize = 79.2f;
    [SerializeField] private float m_EquipSlotSpacing = 16.3f;
    [SerializeField] private int m_EquipPanelPadding = 12;
    [SerializeField] private int m_EquipPanelPaddingVertical = 10;

    [Header("Equipment Panel Position")]
    [SerializeField] private Vector2 m_EquipPanelOffset = new Vector2(15f, 0f);

    [Header("UI Scale")]
    [SerializeField] private float m_UIScaleMultiplier = 1.0f;
    [SerializeField] private string m_EquippableCollectionName = "Equippable";
    [SerializeField] private string m_RifleCategory = "RangedWeapon";
    [SerializeField] private string m_PistolCategory = "RangedWeapon";

    [Header("Colors - Inventory Grid")]
    [SerializeField] private Color m_OverlayColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color m_SlotColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color m_SlotHoverColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
    [SerializeField] private Color m_SlotBorderColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    [SerializeField] private Color m_TextColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float m_IconPadding = 6f;
    [SerializeField] private float m_AmountFontSize = 14f;
    [SerializeField] private float m_NameFontSize = 11f;

    [Header("Colors - Equipment Slots")]
    [SerializeField] private Color m_EquipPanelColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
    [SerializeField] private Color m_EquipBorderColor = new Color(0.5f, 0.42f, 0.25f, 0.9f);
    [SerializeField] private Color m_EquipBackgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
    [SerializeField] private Color m_EquipInnerColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);
    [SerializeField] private Color m_EquipLabelColor = new Color(0.55f, 0.5f, 0.4f, 0.9f);
    [SerializeField] private Color m_EquipNameColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    [SerializeField] private Color m_EquipHoverColor = new Color(0.2f, 0.2f, 0.25f, 0.98f);

    [Header("References (Auto-found if empty)")]
    [SerializeField] private Inventory m_PlayerInventory;

    [Header("Settings UI")]
    [SerializeField] private Sprite m_SettingsIcon; // Assign T_23_gear_.png from Clean Vector Icons

    // Generated UI elements
    private Canvas m_Canvas;
    private GameObject m_OverlayPanel;
    private GameObject m_ContentPanel;
    private GameObject m_GridContainer;
    private GameObject m_EquipmentContainer;
    private List<InventorySlot> m_Slots = new List<InventorySlot>();
    private bool m_IsOpen = false;
    private bool m_Initialized = false;

    // Equipment slots
    private EquipmentSlot m_RifleHolsterSlot;
    private EquipmentSlot m_PistolHolsterSlot;
    private EquipmentSlot m_BackpackSlot;
    private EquipmentSlot m_FlashlightSlot;

    // For item actions
    private GameObject m_ContextMenu;
    private ItemInfo m_SelectedItem;
    private int m_SelectedSlotIndex = -1;
    private EquipmentSlot m_SelectedEquipSlot = null;

    // For drag and drop
    private GameObject m_DragIcon;
    private Image m_DragIconImage;
    private bool m_IsDragging = false;
    private int m_DragSourceInventoryIndex = -1;
    private string m_DragSourceEquipSlot = null;
    private ItemInfo m_DraggedItem;
    public bool IsDragging => m_IsDragging;

    // For runtime color updates
    private Image m_EquipPanelImage;

    // Settings panel
    private GameObject m_SettingsButton;
    private GameObject m_SettingsPanel;
    private Slider m_SensitivitySlider;
    private TextMeshProUGUI m_SensitivityValueText;
    private PlayerInput m_PlayerInput;
    private const string SENSITIVITY_PREF_KEY = "MouseSensitivity";
    private bool m_SensitivityApplied = false;

    private class InventorySlot
    {
        public GameObject SlotObject;
        public Image Background;
        public Image Border;
        public Image IconImage;
        public TextMeshProUGUI AmountText;
        public TextMeshProUGUI NameText;
        public Button Button;
        public int Index;
    }

    private class EquipmentSlot
    {
        public GameObject SlotObject;
        public Image Background;
        public Image Border;
        public Image InnerHighlight;
        public Image IconImage;
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI NameText;
        public Button Button;
        public string SlotType; // "Rifle", "Pistol", "Backpack"
        public ItemInfo EquippedItem;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (m_Initialized) return;

        FindPlayerInventory();
        CreateUI();
        SetMenuOpen(false);

        m_Initialized = true;
    }

    private void FindPlayerInventory()
    {
        if (m_PlayerInventory != null) return;

        // Find player inventory - look for one with CharacterInventoryBridge or on player tagged object
        var inventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        foreach (var inv in inventories)
        {
            // Skip pickup inventories
            if (inv.GetComponent<Opsive.UltimateInventorySystem.DropsAndPickups.InventoryPickup>() != null)
                continue;

            // Prefer player tagged
            if (inv.CompareTag("Player"))
            {
                m_PlayerInventory = inv;
                return;
            }

            // Or one with CharacterInventoryBridge
            if (inv.GetComponent("CharacterInventoryBridge") != null)
            {
                m_PlayerInventory = inv;
                return;
            }

            // Fallback to first non-pickup inventory
            if (m_PlayerInventory == null)
            {
                m_PlayerInventory = inv;
            }
        }
    }

    private void CreateUI()
    {
        // Always create our own canvas with proper scaling
        var canvasObj = new GameObject("InventoryCanvas");
        m_Canvas = canvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Use a fixed reference resolution for consistent sizing
        // Adjust m_UIScaleMultiplier to make UI larger/smaller
        float baseRef = 1080f / Mathf.Max(0.5f, m_UIScaleMultiplier);
        scaler.referenceResolution = new Vector2(baseRef * 16f / 9f, baseRef);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // Create dark overlay
        CreateOverlay();

        // Create content panel
        CreateContentPanel();

        // Create title
        CreateTitle();

        // Create equipment slots (left side)
        CreateEquipmentSlots();

        // Create grid
        CreateGrid();

        // Create context menu (for item actions)
        CreateContextMenu();

        // Create drag icon (hidden by default)
        CreateDragIcon();

        // Create settings button and panel
        CreateSettingsButton();
        CreateSettingsPanel();

        // Find player input for sensitivity control
        FindPlayerInput();
    }

    private void CreateOverlay()
    {
        m_OverlayPanel = new GameObject("InventoryOverlay");
        m_OverlayPanel.transform.SetParent(m_Canvas.transform, false);

        var rect = m_OverlayPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = m_OverlayPanel.AddComponent<Image>();
        image.color = m_OverlayColor;
        image.raycastTarget = true;

        // Click overlay to close
        var button = m_OverlayPanel.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => SetMenuOpen(false));
    }

    private void CreateContentPanel()
    {
        m_ContentPanel = new GameObject("InventoryContent");
        m_ContentPanel.transform.SetParent(m_OverlayPanel.transform, false);

        var rect = m_ContentPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Calculate size based on grid + equipment slots on left (use max rows)
        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
        float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;

        // Add space for equipment slots on left (2 columns now)
        float equipWidth = 2 * m_EquipSlotSize + m_EquipSlotSpacing + m_EquipSlotSpacing; // 2 cols + spacing between cols + gap to grid
        float totalWidth = equipWidth + gridWidth;

        rect.sizeDelta = new Vector2(totalWidth, gridHeight);

        // No background image - just a container for the grid
    }

    private void CreateTitle()
    {
        // No title - clean look like iPhone home screen
    }

    private void CreateEquipmentSlots()
    {
        // Create a panel background for equipment slots
        var panelObj = new GameObject("EquipmentPanel");
        panelObj.transform.SetParent(m_OverlayPanel.transform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.5f);
        panelRect.anchorMax = new Vector2(0, 0.5f);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.anchoredPosition = m_EquipPanelOffset;

        // Panel background
        m_EquipPanelImage = panelObj.AddComponent<Image>();
        m_EquipPanelImage.color = m_EquipPanelColor;

        // Equipment container inside panel
        m_EquipmentContainer = new GameObject("EquipmentSlots");
        m_EquipmentContainer.transform.SetParent(panelObj.transform, false);

        var rect = m_EquipmentContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // Use VerticalLayoutGroup to automatically pack slots
        var layout = m_EquipmentContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = m_EquipSlotSpacing;
        layout.padding = new RectOffset(m_EquipPanelPadding, m_EquipPanelPadding, m_EquipPanelPaddingVertical, m_EquipPanelPaddingVertical);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Add ContentSizeFitter to auto-size the container
        var fitter = m_EquipmentContainer.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create equipment slots - layout group handles positioning
        m_BackpackSlot = CreateEquipmentSlotSimple("Backpack");
        m_RifleHolsterSlot = CreateEquipmentSlotSimple("Rifle");
        m_PistolHolsterSlot = CreateEquipmentSlotSimple("Pistol");
        m_FlashlightSlot = CreateEquipmentSlotSimple("Flashlight");

        // Size the panel to fit content
        var panelFitter = panelObj.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add layout element to panel for sizing
        var panelLayout = panelObj.AddComponent<LayoutElement>();
        panelLayout.preferredWidth = m_EquipSlotSize + 24;
    }

    private EquipmentSlot CreateEquipmentSlotSimple(string slotType)
    {
        var slot = new EquipmentSlot();
        slot.SlotType = slotType;

        // Slot container
        slot.SlotObject = new GameObject($"EquipSlot_{slotType}");
        slot.SlotObject.transform.SetParent(m_EquipmentContainer.transform, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(m_EquipSlotSize, m_EquipSlotSize + m_NameHeight + 4);

        // Add LayoutElement for layout group
        var layoutElement = slot.SlotObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = m_EquipSlotSize;
        layoutElement.preferredHeight = m_EquipSlotSize + m_NameHeight + 4;

        // Outer border (golden/bronze for equipment)
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slot.SlotObject.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0, 1);
        borderRect.anchorMax = new Vector2(1, 1);
        borderRect.pivot = new Vector2(0.5f, 1);
        borderRect.anchoredPosition = Vector2.zero;
        borderRect.sizeDelta = new Vector2(0, m_EquipSlotSize);
        slot.Border = borderObj.AddComponent<Image>();
        slot.Border.color = m_EquipBorderColor;

        // Background (darker inside)
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slot.SlotObject.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = new Vector2(0, -2);
        bgRect.sizeDelta = new Vector2(-4, m_EquipSlotSize - 4);
        slot.Background = bgObj.AddComponent<Image>();
        slot.Background.color = m_EquipBackgroundColor;

        // Inner highlight/bevel (subtle)
        var innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(slot.SlotObject.transform, false);
        var innerRect = innerObj.AddComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0, 1);
        innerRect.anchorMax = new Vector2(1, 1);
        innerRect.pivot = new Vector2(0.5f, 1);
        innerRect.anchoredPosition = new Vector2(0, -3);
        innerRect.sizeDelta = new Vector2(-6, m_EquipSlotSize - 6);
        slot.InnerHighlight = innerObj.AddComponent<Image>();
        slot.InnerHighlight.color = m_EquipInnerColor;
        slot.InnerHighlight.raycastTarget = false;

        // Icon (centered in slot)
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.SlotObject.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 1);
        iconRect.anchorMax = new Vector2(1, 1);
        iconRect.pivot = new Vector2(0.5f, 1);
        iconRect.anchoredPosition = new Vector2(0, -8);
        iconRect.sizeDelta = new Vector2(-16, m_EquipSlotSize - 16);
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;
        slot.IconImage.enabled = false;

        // Slot type label (small, at bottom of slot area)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(slot.SlotObject.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1);
        labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.pivot = new Vector2(0.5f, 0);
        labelRect.anchoredPosition = new Vector2(0, -m_EquipSlotSize + 4);
        labelRect.sizeDelta = new Vector2(m_EquipSlotSize - 8, 14);
        slot.LabelText = labelObj.AddComponent<TextMeshProUGUI>();
        slot.LabelText.text = slotType;
        slot.LabelText.fontSize = 9;
        slot.LabelText.fontStyle = FontStyles.Italic;
        slot.LabelText.color = m_EquipLabelColor;
        slot.LabelText.alignment = TextAlignmentOptions.Center;
        slot.LabelText.raycastTarget = false;

        // Item name text (below slot, shows when item equipped)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 0);
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(m_EquipSlotSize + 20, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = 11;
        slot.NameText.color = m_EquipNameColor;
        slot.NameText.alignment = TextAlignmentOptions.Center;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button for interaction
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        string capturedType = slotType;
        slot.Button.onClick.AddListener(() => OnEquipmentSlotClicked(capturedType));

        // Hover effect (brighten background)
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, m_EquipBackgroundColor, m_EquipHoverColor);

        // Drag handler
        var dragHandler = slot.SlotObject.AddComponent<EquipmentSlotDragHandler>();
        dragHandler.Initialize(this, slotType, slot.IconImage);

        // Drop handler (highlight border on drag over)
        var dropHandler = slot.SlotObject.AddComponent<EquipmentSlotDropHandler>();
        dropHandler.Initialize(this, slotType, slot.Border);

        return slot;
    }

    private EquipmentSlot CreateEquipmentSlotAt(string slotType, int col, int row)
    {
        var slot = new EquipmentSlot();
        slot.SlotType = slotType;

        float equipTotalHeight = m_EquipSlotSize + m_NameHeight;
        float x = col * (m_EquipSlotSize + m_EquipSlotSpacing);
        float y = -row * (equipTotalHeight + m_EquipSlotSpacing);

        // Slot container
        slot.SlotObject = new GameObject($"EquipSlot_{slotType}");
        slot.SlotObject.transform.SetParent(m_EquipmentContainer.transform, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(m_EquipSlotSize, m_EquipSlotSize);

        // Border (slightly different color for equipment)
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slot.SlotObject.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        slot.Border = borderObj.AddComponent<Image>();
        slot.Border.color = new Color(0.6f, 0.5f, 0.3f, 0.8f); // Goldenish border for equipment

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slot.SlotObject.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        slot.Background = bgObj.AddComponent<Image>();
        slot.Background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f); // Slightly blue tint

        // Slot type label (top of slot)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(slot.SlotObject.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1);
        labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, -4);
        labelRect.sizeDelta = new Vector2(m_EquipSlotSize, 16);
        slot.LabelText = labelObj.AddComponent<TextMeshProUGUI>();
        slot.LabelText.text = slotType;
        slot.LabelText.fontSize = 10;
        slot.LabelText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        slot.LabelText.alignment = TextAlignmentOptions.Top;
        slot.LabelText.raycastTarget = false;

        // Icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.SlotObject.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8, 8);
        iconRect.offsetMax = new Vector2(-8, -18); // Leave room for label at top
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;
        slot.IconImage.enabled = false;

        // Item name text (below the slot)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -2);
        nameRect.sizeDelta = new Vector2(m_EquipSlotSize + 10, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = 10;
        slot.NameText.color = m_TextColor;
        slot.NameText.alignment = TextAlignmentOptions.Top;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button for interaction
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        string capturedType = slotType;
        slot.Button.onClick.AddListener(() => OnEquipmentSlotClicked(capturedType));

        // Hover effect
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, new Color(0.15f, 0.15f, 0.2f, 0.9f), new Color(0.25f, 0.25f, 0.3f, 0.95f));

        // Drag handler (to drag equipped items out)
        var dragHandler = slot.SlotObject.AddComponent<EquipmentSlotDragHandler>();
        dragHandler.Initialize(this, slotType, slot.IconImage);

        // Drop handler (to drop items into equipment slot)
        var dropHandler = slot.SlotObject.AddComponent<EquipmentSlotDropHandler>();
        dropHandler.Initialize(this, slotType, slot.Border);

        return slot;
    }

    private EquipmentSlot CreateEquipmentSlot(string slotType, int index)
    {
        var slot = new EquipmentSlot();
        slot.SlotType = slotType;

        float equipTotalHeight = m_EquipSlotSize + m_NameHeight;
        float y = -index * (equipTotalHeight + m_EquipSlotSpacing);

        // Slot container
        slot.SlotObject = new GameObject($"EquipSlot_{slotType}");
        slot.SlotObject.transform.SetParent(m_EquipmentContainer.transform, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(m_EquipSlotSize, m_EquipSlotSize);

        // Border (slightly different color for equipment)
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slot.SlotObject.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        slot.Border = borderObj.AddComponent<Image>();
        slot.Border.color = new Color(0.6f, 0.5f, 0.3f, 0.8f); // Goldenish border for equipment

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slot.SlotObject.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        slot.Background = bgObj.AddComponent<Image>();
        slot.Background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f); // Slightly blue tint

        // Slot type label (top of slot)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(slot.SlotObject.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1);
        labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, -4);
        labelRect.sizeDelta = new Vector2(m_EquipSlotSize, 16);
        slot.LabelText = labelObj.AddComponent<TextMeshProUGUI>();
        slot.LabelText.text = slotType;
        slot.LabelText.fontSize = 10;
        slot.LabelText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        slot.LabelText.alignment = TextAlignmentOptions.Top;
        slot.LabelText.raycastTarget = false;

        // Icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.SlotObject.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8, 8);
        iconRect.offsetMax = new Vector2(-8, -18); // Leave room for label at top
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;
        slot.IconImage.enabled = false;

        // Item name text (below the slot)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -2);
        nameRect.sizeDelta = new Vector2(m_EquipSlotSize + 10, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = 10;
        slot.NameText.color = m_TextColor;
        slot.NameText.alignment = TextAlignmentOptions.Top;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button for interaction
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        string capturedType = slotType;
        slot.Button.onClick.AddListener(() => OnEquipmentSlotClicked(capturedType));

        // Hover effect
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, new Color(0.15f, 0.15f, 0.2f, 0.9f), new Color(0.25f, 0.25f, 0.3f, 0.95f));

        // Drag handler (to drag equipped items out)
        var dragHandler = slot.SlotObject.AddComponent<EquipmentSlotDragHandler>();
        dragHandler.Initialize(this, slotType, slot.IconImage);

        // Drop handler (to drop items into equipment slot)
        var dropHandler = slot.SlotObject.AddComponent<EquipmentSlotDropHandler>();
        dropHandler.Initialize(this, slotType, slot.Border);

        return slot;
    }

    private void CreateGrid()
    {
        m_GridContainer = new GameObject("Grid");
        m_GridContainer.transform.SetParent(m_OverlayPanel.transform, false); // Direct child of overlay

        var rect = m_GridContainer.AddComponent<RectTransform>();
        // Anchor to right side of screen, centered vertically
        rect.anchorMin = new Vector2(1, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = m_GridOffset;

        // Account for name text below each slot - use max size
        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
        float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;
        rect.sizeDelta = new Vector2(gridWidth, gridHeight);

        // Create max possible slots (they'll be shown/hidden dynamically)
        int totalSlots = m_GridColumns * m_MaxGridRows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }

        // Add invisible image for raycast detection
        var gridImage = m_GridContainer.AddComponent<Image>();
        gridImage.color = new Color(0, 0, 0, 0); // Fully transparent

        // Add drop handler to grid for unequipping items
        var gridDropHandler = m_GridContainer.AddComponent<InventoryDropHandler>();
        gridDropHandler.Initialize(this);
    }

    private int GetAvailableSlotCount()
    {
        int slots = m_BaseSlots;

        // Check if backpack is equipped
        if (m_BackpackSlot != null && m_BackpackSlot.EquippedItem.Item != null)
        {
            var backpack = m_BackpackSlot.EquippedItem.Item;

            // Try to get BagSize attribute
            if (backpack.TryGetAttributeValue<int>("BagSize", out int bagSize))
            {
                slots += bagSize;
            }
            else
            {
                // Default backpack bonus if no BagSize attribute
                slots += 8; // Default to 8 extra slots
            }
        }

        // Cap at max slots
        int maxSlots = m_GridColumns * m_MaxGridRows;
        return Mathf.Min(slots, maxSlots);
    }

    private void CreateSlot(int index)
    {
        int row = index / m_GridColumns;
        int col = index % m_GridColumns;

        // Account for slot size + name height when calculating vertical spacing
        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float x = col * (m_SlotSize + m_SlotSpacing) - (m_GridColumns - 1) * (m_SlotSize + m_SlotSpacing) / 2f;
        float y = -row * (slotTotalHeight + m_SlotSpacing) + (m_MaxGridRows - 1) * (slotTotalHeight + m_SlotSpacing) / 2f;

        var slot = new InventorySlot();
        slot.Index = index;

        // Slot container
        slot.SlotObject = new GameObject($"Slot_{index}");
        slot.SlotObject.transform.SetParent(m_GridContainer.transform, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(m_SlotSize, m_SlotSize);

        // Border
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slot.SlotObject.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        slot.Border = borderObj.AddComponent<Image>();
        slot.Border.color = m_SlotBorderColor;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slot.SlotObject.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        slot.Background = bgObj.AddComponent<Image>();
        slot.Background.color = m_SlotColor;

        // Icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.SlotObject.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(m_IconPadding, m_IconPadding);
        iconRect.offsetMax = new Vector2(-m_IconPadding, -m_IconPadding);
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;

        // Amount text
        var amountObj = new GameObject("Amount");
        amountObj.transform.SetParent(slot.SlotObject.transform, false);
        var amountRect = amountObj.AddComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1, 0);
        amountRect.anchorMax = new Vector2(1, 0);
        amountRect.pivot = new Vector2(1, 0);
        amountRect.anchoredPosition = new Vector2(-4, 4);
        amountRect.sizeDelta = new Vector2(40, 20);
        slot.AmountText = amountObj.AddComponent<TextMeshProUGUI>();
        slot.AmountText.fontSize = m_AmountFontSize;
        slot.AmountText.fontStyle = FontStyles.Bold;
        slot.AmountText.color = m_TextColor;
        slot.AmountText.alignment = TextAlignmentOptions.BottomRight;
        slot.AmountText.raycastTarget = false;

        // Item name text (below the slot)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -2); // Just below the slot
        nameRect.sizeDelta = new Vector2(m_SlotSize + m_SlotSpacing, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = m_NameFontSize;
        slot.NameText.color = m_TextColor;
        slot.NameText.alignment = TextAlignmentOptions.Top;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button for interaction
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        int slotIndex = index; // Capture for closure
        slot.Button.onClick.AddListener(() => OnSlotClicked(slotIndex));

        // Hover effect
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, m_SlotColor, m_SlotHoverColor);

        // Drag handler
        var dragHandler = slot.SlotObject.AddComponent<InventorySlotDragHandler>();
        dragHandler.Initialize(this, index, slot.IconImage);

        m_Slots.Add(slot);
    }

    private void CreateContextMenu()
    {
        m_ContextMenu = new GameObject("ContextMenu");
        m_ContextMenu.transform.SetParent(m_Canvas.transform, false);

        var rect = m_ContextMenu.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 80);

        var image = m_ContextMenu.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        var layout = m_ContextMenu.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.spacing = 2;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Use button
        CreateContextButton("Use", OnUseItem);

        // Drop button
        CreateContextButton("Drop", OnDropItem);

        m_ContextMenu.SetActive(false);
    }

    private void CreateContextButton(string text, UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(m_ContextMenu.transform, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110, 30);

        var image = btnObj.AddComponent<Image>();
        image.color = m_SlotColor;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        button.onClick.AddListener(() => m_ContextMenu.SetActive(false));

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = m_TextColor;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateDragIcon()
    {
        m_DragIcon = new GameObject("DragIcon");
        m_DragIcon.transform.SetParent(m_Canvas.transform, false);

        var rect = m_DragIcon.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(m_SlotSize * 0.8f, m_SlotSize * 0.8f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        m_DragIconImage = m_DragIcon.AddComponent<Image>();
        m_DragIconImage.preserveAspect = true;
        m_DragIconImage.raycastTarget = false; // Don't block raycasts

        // Add a slight transparency
        var canvasGroup = m_DragIcon.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        m_DragIcon.SetActive(false);
    }

    private void CreateSettingsButton()
    {
        m_SettingsButton = new GameObject("SettingsButton");
        m_SettingsButton.transform.SetParent(m_OverlayPanel.transform, false);

        var rect = m_SettingsButton.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1); // Top-right
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(40, 40);

        // Background
        var image = m_SettingsButton.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        // Button
        var button = m_SettingsButton.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ToggleSettingsPanel);

        // Gear icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(m_SettingsButton.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6, 6);
        iconRect.offsetMax = new Vector2(-6, -6);

        var iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        // Use assigned sprite or try to load from Resources
        if (m_SettingsIcon != null)
        {
            iconImage.sprite = m_SettingsIcon;
            iconImage.color = Color.white;
        }
        else
        {
            // Try to load from Resources as fallback
            var loadedIcon = Resources.Load<Sprite>("Icons/T_23_gear_");
            if (loadedIcon != null)
            {
                iconImage.sprite = loadedIcon;
                iconImage.color = Color.white;
            }
            else
            {
                // Last fallback - just show a white square with "S" text
                iconImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);

                var textObj = new GameObject("FallbackText");
                textObj.transform.SetParent(iconObj.transform, false);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                var tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "S";
                tmp.fontSize = 20;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }
        }
    }

    private void CreateSettingsPanel()
    {
        m_SettingsPanel = new GameObject("SettingsPanel");
        m_SettingsPanel.transform.SetParent(m_OverlayPanel.transform, false);

        var rect = m_SettingsPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1); // Top-right
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -70);
        rect.sizeDelta = new Vector2(280, 120);

        // Background
        var image = m_SettingsPanel.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(m_SettingsPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(0, 25);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Settings";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        // Sensitivity label
        var labelObj = new GameObject("SensitivityLabel");
        labelObj.transform.SetParent(m_SettingsPanel.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(15, -40);
        labelRect.sizeDelta = new Vector2(200, 20);

        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "Mouse Sensitivity";
        labelText.fontSize = 14;
        labelText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.raycastTarget = false;

        // Sensitivity value display
        var valueObj = new GameObject("SensitivityValue");
        valueObj.transform.SetParent(m_SettingsPanel.transform, false);
        var valueRect = valueObj.AddComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1, 1);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.pivot = new Vector2(1, 1);
        valueRect.anchoredPosition = new Vector2(-15, -40);
        valueRect.sizeDelta = new Vector2(50, 20);

        m_SensitivityValueText = valueObj.AddComponent<TextMeshProUGUI>();
        m_SensitivityValueText.text = "1.0";
        m_SensitivityValueText.fontSize = 14;
        m_SensitivityValueText.color = Color.white;
        m_SensitivityValueText.alignment = TextAlignmentOptions.Right;
        m_SensitivityValueText.raycastTarget = false;

        // Slider
        CreateSensitivitySlider();

        m_SettingsPanel.SetActive(false);
    }

    private void CreateSensitivitySlider()
    {
        var sliderObj = new GameObject("SensitivitySlider");
        sliderObj.transform.SetParent(m_SettingsPanel.transform, false);

        var sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 1);
        sliderRect.anchorMax = new Vector2(1, 1);
        sliderRect.pivot = new Vector2(0.5f, 1);
        sliderRect.anchoredPosition = new Vector2(0, -70);
        sliderRect.sizeDelta = new Vector2(-30, 30);

        m_SensitivitySlider = sliderObj.AddComponent<Slider>();
        m_SensitivitySlider.minValue = 0.1f;
        m_SensitivitySlider.maxValue = 3.0f;
        m_SensitivitySlider.wholeNumbers = false;

        // Load saved sensitivity
        float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_PREF_KEY, 1.0f);
        m_SensitivitySlider.value = savedSensitivity;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Fill area
        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.4f, 0.6f, 0.8f, 1f);

        m_SensitivitySlider.fillRect = fillRect;

        // Handle slide area
        var handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        var handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);

        var handleImage = handleObj.AddComponent<Image>();
        handleImage.color = Color.white;

        m_SensitivitySlider.targetGraphic = handleImage;
        m_SensitivitySlider.handleRect = handleRect;

        // Add listener
        m_SensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        // Update initial display
        UpdateSensitivityDisplay(savedSensitivity);
    }

    private void FindPlayerInput()
    {
        // Find PlayerInput on the player character
        if (m_PlayerInventory != null)
        {
            m_PlayerInput = m_PlayerInventory.GetComponent<PlayerInput>();
        }

        if (m_PlayerInput == null)
        {
            // Try to find on player tagged object
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                m_PlayerInput = player.GetComponent<PlayerInput>();
            }
        }

        if (m_PlayerInput == null)
        {
            // Last resort - find any PlayerInput
            m_PlayerInput = FindFirstObjectByType<PlayerInput>();
        }

        // Apply saved sensitivity
        if (m_PlayerInput != null)
        {
            float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_PREF_KEY, 1.0f);
            m_PlayerInput.LookSensitivityMultiplier = savedSensitivity;
            m_PlayerInput.LookSensitivity = new Vector2(2f * savedSensitivity, 2f * savedSensitivity);
        }
    }

    private void ToggleSettingsPanel()
    {
        if (m_SettingsPanel != null)
        {
            m_SettingsPanel.SetActive(!m_SettingsPanel.activeSelf);
        }
    }

    private void OnSensitivityChanged(float value)
    {
        // Try to find PlayerInput if we don't have it
        if (m_PlayerInput == null)
        {
            FindPlayerInput();
        }

        // Apply to player input
        if (m_PlayerInput != null)
        {
            // Set both the multiplier AND the base sensitivity
            m_PlayerInput.LookSensitivityMultiplier = value;
            m_PlayerInput.LookSensitivity = new Vector2(2f * value, 2f * value);
            Debug.Log($"[Sensitivity] Set to {value}, LookSensitivity={m_PlayerInput.LookSensitivity}, Multiplier={m_PlayerInput.LookSensitivityMultiplier}");
        }
        else
        {
            Debug.LogWarning("[Sensitivity] PlayerInput not found!");
        }

        // Save preference
        PlayerPrefs.SetFloat(SENSITIVITY_PREF_KEY, value);
        PlayerPrefs.Save();

        // Update display
        UpdateSensitivityDisplay(value);
    }

    private void UpdateSensitivityDisplay(float value)
    {
        if (m_SensitivityValueText != null)
        {
            m_SensitivityValueText.text = value.ToString("F1");
        }
    }

    private void Update()
    {
        // Apply saved sensitivity once player is found
        if (!m_SensitivityApplied)
        {
            if (m_PlayerInput == null)
            {
                FindPlayerInput();
            }
            if (m_PlayerInput != null)
            {
                float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_PREF_KEY, 1.0f);
                m_PlayerInput.LookSensitivityMultiplier = savedSensitivity;
                m_PlayerInput.LookSensitivity = new Vector2(2f * savedSensitivity, 2f * savedSensitivity);
                m_SensitivityApplied = true;
                Debug.Log($"[Sensitivity] Applied saved sensitivity: {savedSensitivity}");
            }
        }

        if (Input.GetKeyDown(m_ToggleKey))
        {
            // Don't open inventory if loot screen is open (it will close itself)
            var lootUI = FindFirstObjectByType<SimpleLootUI>();
            if (lootUI != null && lootUI.IsOpen)
                return;

            ToggleMenu();
        }

        if (m_IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuOpen(false);
        }

        // Close context menu on click outside
        if (m_ContextMenu.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                m_ContextMenu.GetComponent<RectTransform>(),
                Input.mousePosition,
                null))
            {
                m_ContextMenu.SetActive(false);
            }
        }
    }

    public void ToggleMenu()
    {
        SetMenuOpen(!m_IsOpen);
    }

    public void SetMenuOpen(bool open)
    {
        m_IsOpen = open;

        if (m_OverlayPanel != null)
            m_OverlayPanel.SetActive(open);

        if (m_ContextMenu != null)
            m_ContextMenu.SetActive(false);

        // Close settings panel when closing inventory
        if (!open && m_SettingsPanel != null)
            m_SettingsPanel.SetActive(false);

        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        if (m_PlayerInventory != null)
        {
            Opsive.Shared.Events.EventHandler.ExecuteEvent(
                m_PlayerInventory.gameObject,
                "OnEnableGameplayInput",
                !open
            );
        }

        if (open)
        {
            RefreshUI();

            // Refresh sensitivity display from current value
            if (m_PlayerInput != null && m_SensitivitySlider != null)
            {
                m_SensitivitySlider.SetValueWithoutNotify(m_PlayerInput.LookSensitivityMultiplier);
                UpdateSensitivityDisplay(m_PlayerInput.LookSensitivityMultiplier);
            }
        }
    }

    /// <summary>
    /// Show or hide the entire inventory UI (used by SimpleLootUI to prevent overlap)
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (m_Canvas != null)
        {
            m_Canvas.gameObject.SetActive(visible);
        }
    }

    public void RefreshUI()
    {
        // Check if UI was destroyed and needs rebuild
        if (m_Slots.Count > 0 && (m_Slots[0].SlotObject == null || m_Slots[0].IconImage == null))
        {
            Debug.Log("[SimpleInventoryUI] UI elements were destroyed, rebuilding...");
            m_Initialized = false;
            m_Slots.Clear();
            Initialize();
        }

        if (m_PlayerInventory == null)
        {
            FindPlayerInventory();
            if (m_PlayerInventory == null) return;
        }

        // Refresh equipment slots first
        RefreshEquipmentSlots();

        // Calculate available slots based on equipped backpack
        int availableSlots = GetAvailableSlotCount();

        // Get all items from Default collection
        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        if (defaultCollection == null)
        {
            defaultCollection = m_PlayerInventory.MainItemCollection;
        }

        var allItems = defaultCollection?.GetAllItemStacks();

        // Clear and show/hide inventory slots
        for (int i = 0; i < m_Slots.Count; i++)
        {
            var slot = m_Slots[i];
            if (slot.SlotObject == null || slot.IconImage == null) continue;
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
            if (slot.AmountText != null) slot.AmountText.text = "";
            if (slot.NameText != null) slot.NameText.text = "";

            // Show or hide slot based on available count
            slot.SlotObject.SetActive(i < availableSlots);
        }

        if (allItems == null) return;

        // Fill slots with items (only up to available slots)
        int slotIndex = 0;
        for (int i = 0; i < allItems.Count && slotIndex < availableSlots; i++)
        {
            var itemStack = allItems[i];
            if (itemStack?.Item == null) continue;

            var slot = m_Slots[slotIndex];

            // Get icon from item
            Sprite icon = null;

            // Try IconDatabaseItemView system first (custom system)
            var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");
            if (iconDb != null)
            {
                icon = iconDb.GetIcon(itemStack.Item.ItemDefinition);
            }

            // Fallback to Icon attribute
            if (icon == null && itemStack.Item.TryGetAttributeValue<Sprite>("Icon", out var attrIcon))
            {
                icon = attrIcon;
            }

            if (icon != null)
            {
                slot.IconImage.sprite = icon;
                slot.IconImage.enabled = true;
            }

            // Show item name
            slot.NameText.text = itemStack.Item.name;

            // Show amount if more than 1
            if (itemStack.Amount > 1)
            {
                slot.AmountText.text = itemStack.Amount.ToString();
            }

            slotIndex++;
        }
    }

    /// <summary>
    /// Apply equipment slot styling at runtime. Call this after changing values in the Inspector.
    /// </summary>
    public void ApplyEquipmentStyling()
    {
        // Apply panel color and position
        if (m_EquipPanelImage != null)
        {
            m_EquipPanelImage.color = m_EquipPanelColor;

            // Update panel position
            var panelRect = m_EquipPanelImage.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchoredPosition = m_EquipPanelOffset;
            }
        }

        // Apply layout group settings
        if (m_EquipmentContainer != null)
        {
            var layout = m_EquipmentContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = m_EquipSlotSpacing;
                layout.padding = new RectOffset(m_EquipPanelPadding, m_EquipPanelPadding, m_EquipPanelPaddingVertical, m_EquipPanelPaddingVertical);
            }
        }

        // Apply slot styling
        ApplySlotStyling(m_BackpackSlot);
        ApplySlotStyling(m_RifleHolsterSlot);
        ApplySlotStyling(m_PistolHolsterSlot);
        ApplySlotStyling(m_FlashlightSlot);

        // Force layout rebuild
        if (m_EquipmentContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_EquipmentContainer.GetComponent<RectTransform>());

            // Also rebuild parent panel
            var parentPanel = m_EquipmentContainer.transform.parent;
            if (parentPanel != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentPanel.GetComponent<RectTransform>());
            }
        }
    }

    private void ApplySlotStyling(EquipmentSlot slot)
    {
        if (slot == null || slot.SlotObject == null) return;

        // Update slot size
        var slotRect = slot.SlotObject.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.sizeDelta = new Vector2(m_EquipSlotSize, m_EquipSlotSize + m_NameHeight + 4);
        }

        // Update layout element
        var layoutElement = slot.SlotObject.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = m_EquipSlotSize;
            layoutElement.preferredHeight = m_EquipSlotSize + m_NameHeight + 4;
        }

        // Update border size
        if (slot.Border != null)
        {
            var borderRect = slot.Border.GetComponent<RectTransform>();
            if (borderRect != null)
            {
                borderRect.sizeDelta = new Vector2(0, m_EquipSlotSize);
            }
            slot.Border.color = m_EquipBorderColor;
        }

        // Update background size
        if (slot.Background != null)
        {
            var bgRect = slot.Background.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                bgRect.anchoredPosition = new Vector2(0, -2);
                bgRect.sizeDelta = new Vector2(-4, m_EquipSlotSize - 4);
            }
            slot.Background.color = m_EquipBackgroundColor;
        }

        // Update inner highlight size
        if (slot.InnerHighlight != null)
        {
            var innerRect = slot.InnerHighlight.GetComponent<RectTransform>();
            if (innerRect != null)
            {
                innerRect.anchoredPosition = new Vector2(0, -3);
                innerRect.sizeDelta = new Vector2(-6, m_EquipSlotSize - 6);
            }
            slot.InnerHighlight.color = m_EquipInnerColor;
        }

        // Update icon size
        if (slot.IconImage != null)
        {
            var iconRect = slot.IconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = new Vector2(0, -8);
                iconRect.sizeDelta = new Vector2(-16, m_EquipSlotSize - 16);
            }
        }

        // Update label position and color
        if (slot.LabelText != null)
        {
            var labelRect = slot.LabelText.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchoredPosition = new Vector2(0, -m_EquipSlotSize + 4);
                labelRect.sizeDelta = new Vector2(m_EquipSlotSize - 8, 14);
            }
            slot.LabelText.color = m_EquipLabelColor;
        }

        // Update name text size and color
        if (slot.NameText != null)
        {
            var nameRect = slot.NameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.sizeDelta = new Vector2(m_EquipSlotSize + 20, m_NameHeight);
            }
            slot.NameText.color = m_EquipNameColor;
        }

        // Update hover handler colors
        var hoverHandler = slot.SlotObject.GetComponent<SlotHoverHandler>();
        if (hoverHandler != null)
        {
            hoverHandler.Initialize(slot.Background, m_EquipBackgroundColor, m_EquipHoverColor);
        }
    }

    // Keep old method name for compatibility
    public void ApplyEquipmentColors() => ApplyEquipmentStyling();

    /// <summary>
    /// Apply inventory grid styling at runtime.
    /// </summary>
    public void ApplyInventoryGridStyling()
    {
        // Update overlay color
        if (m_OverlayPanel != null)
        {
            var overlayImage = m_OverlayPanel.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.color = m_OverlayColor;
            }
        }

        // Update grid container size and position
        if (m_GridContainer != null)
        {
            var gridRect = m_GridContainer.GetComponent<RectTransform>();
            if (gridRect != null)
            {
                float slotTotalHeight = m_SlotSize + m_NameHeight;
                float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
                float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;
                gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
                gridRect.anchoredPosition = m_GridOffset;
            }
        }

        // Update each slot
        for (int i = 0; i < m_Slots.Count; i++)
        {
            ApplyInventorySlotStyling(m_Slots[i], i);
        }
    }

    private void ApplyInventorySlotStyling(InventorySlot slot, int index)
    {
        if (slot == null || slot.SlotObject == null) return;

        int row = index / m_GridColumns;
        int col = index % m_GridColumns;

        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float x = col * (m_SlotSize + m_SlotSpacing) - (m_GridColumns - 1) * (m_SlotSize + m_SlotSpacing) / 2f;
        float y = -row * (slotTotalHeight + m_SlotSpacing) + (m_MaxGridRows - 1) * (slotTotalHeight + m_SlotSpacing) / 2f;

        // Update slot position and size
        var slotRect = slot.SlotObject.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.anchoredPosition = new Vector2(x, y);
            slotRect.sizeDelta = new Vector2(m_SlotSize, m_SlotSize);
        }

        // Update border color
        if (slot.Border != null)
        {
            slot.Border.color = m_SlotBorderColor;
        }

        // Update background color
        if (slot.Background != null)
        {
            slot.Background.color = m_SlotColor;
        }

        // Update icon size
        if (slot.IconImage != null)
        {
            var iconRect = slot.IconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.offsetMin = new Vector2(m_IconPadding, m_IconPadding);
                iconRect.offsetMax = new Vector2(-m_IconPadding, -m_IconPadding);
            }
        }

        // Update amount text
        if (slot.AmountText != null)
        {
            slot.AmountText.fontSize = m_AmountFontSize;
            slot.AmountText.color = m_TextColor;
        }

        // Update name text
        if (slot.NameText != null)
        {
            var nameRect = slot.NameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.sizeDelta = new Vector2(m_SlotSize + m_SlotSpacing, m_NameHeight);
            }
            slot.NameText.fontSize = m_NameFontSize;
            slot.NameText.color = m_TextColor;
        }

        // Update hover handler
        var hoverHandler = slot.SlotObject.GetComponent<SlotHoverHandler>();
        if (hoverHandler != null)
        {
            hoverHandler.Initialize(slot.Background, m_SlotColor, m_SlotHoverColor);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Apply styling when changed in Inspector during play mode
        if (Application.isPlaying && m_Initialized)
        {
            ApplyEquipmentStyling();
            ApplyInventoryGridStyling();
        }
    }
#endif

    private void RefreshEquipmentSlots()
    {
        // Clear all equipment slots
        ClearEquipmentSlot(m_BackpackSlot);
        ClearEquipmentSlot(m_RifleHolsterSlot);
        ClearEquipmentSlot(m_PistolHolsterSlot);
        ClearEquipmentSlot(m_FlashlightSlot);

        // Get items from Equippable collection
        var equipCollection = m_PlayerInventory.GetItemCollection(m_EquippableCollectionName);
        if (equipCollection == null) return;

        var equippedItems = equipCollection.GetAllItemStacks();
        if (equippedItems == null) return;

        var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");

        foreach (var itemStack in equippedItems)
        {
            if (itemStack?.Item == null) continue;

            var item = itemStack.Item;
            var category = item.Category;
            if (category == null) continue;

            string categoryName = category.name;
            string itemName = item.name.ToLower();

            // Determine which slot this item goes in
            EquipmentSlot targetSlot = null;

            // Check for backpack
            if (categoryName.Contains("Backpack") || categoryName.Contains("Bag"))
            {
                targetSlot = m_BackpackSlot;
            }
            // Check for flashlight
            else if (categoryName.Contains("Flashlight") || categoryName.Contains("Light"))
            {
                targetSlot = m_FlashlightSlot;
            }
            // Check for ranged weapons - try to distinguish rifle vs pistol by name
            else if (categoryName.Contains("Ranged") || categoryName.Contains("Weapon"))
            {
                if (itemName.Contains("pistol") || itemName.Contains("handgun") ||
                    itemName.Contains("revolver") || itemName.Contains("sr-9") ||
                    itemName.Contains("9mm") || itemName.Contains("glock"))
                {
                    targetSlot = m_PistolHolsterSlot;
                }
                else
                {
                    // Default ranged weapons to rifle slot
                    targetSlot = m_RifleHolsterSlot;
                }
            }
            // Check by item name for flashlight (in case category doesn't match)
            else if (itemName.Contains("flashlight") || itemName.Contains("headlamp") || itemName.Contains("torch"))
            {
                targetSlot = m_FlashlightSlot;
            }

            if (targetSlot == null) continue;

            // Only fill if slot is empty (first come first served)
            if (targetSlot.EquippedItem.Item != null) continue;

            // Store item reference
            targetSlot.EquippedItem = (ItemInfo)itemStack;

            // Get icon
            Sprite icon = null;
            if (iconDb != null)
            {
                icon = iconDb.GetIcon(item.ItemDefinition);
            }
            if (icon == null && item.TryGetAttributeValue<Sprite>("Icon", out var attrIcon))
            {
                icon = attrIcon;
            }

            if (icon != null)
            {
                targetSlot.IconImage.sprite = icon;
                targetSlot.IconImage.enabled = true;
            }

            targetSlot.NameText.text = item.name;
        }
    }

    private void ClearEquipmentSlot(EquipmentSlot slot)
    {
        if (slot == null) return;
        if (slot.IconImage == null || slot.NameText == null) return;
        slot.IconImage.sprite = null;
        slot.IconImage.enabled = false;
        slot.NameText.text = "";
        slot.EquippedItem = default;
    }

    private EquipmentSlot GetEquipmentSlotByType(string slotType)
    {
        if (slotType == "Backpack") return m_BackpackSlot;
        if (slotType == "Rifle") return m_RifleHolsterSlot;
        if (slotType == "Pistol") return m_PistolHolsterSlot;
        if (slotType == "Flashlight") return m_FlashlightSlot;
        return null;
    }

    #region Drag and Drop

    public void BeginDragFromInventory(int slotIndex, Sprite icon)
    {
        if (m_PlayerInventory == null) return;

        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        if (defaultCollection == null)
            defaultCollection = m_PlayerInventory.MainItemCollection;

        var allItems = defaultCollection?.GetAllItemStacks();
        if (allItems == null || slotIndex >= allItems.Count) return;

        var itemStack = allItems[slotIndex];
        if (itemStack?.Item == null) return;

        m_DraggedItem = (ItemInfo)itemStack;
        m_DragSourceInventoryIndex = slotIndex;
        m_DragSourceEquipSlot = null;
        m_IsDragging = true;

        // Show drag icon
        m_DragIconImage.sprite = icon;
        m_DragIcon.SetActive(true);
    }

    public void BeginDragFromEquipment(string slotType, Sprite icon)
    {
        EquipmentSlot slot = GetEquipmentSlotByType(slotType);
        if (slot == null || slot.EquippedItem.Item == null) return;

        m_DraggedItem = slot.EquippedItem;
        m_DragSourceInventoryIndex = -1;
        m_DragSourceEquipSlot = slotType;
        m_IsDragging = true;

        // Show drag icon
        m_DragIconImage.sprite = icon;
        m_DragIcon.SetActive(true);
    }

    public void UpdateDragPosition(Vector2 position)
    {
        if (!m_IsDragging || m_DragIcon == null) return;
        m_DragIcon.GetComponent<RectTransform>().position = position;
    }

    public void EndDrag()
    {
        m_IsDragging = false;
        m_DragIcon?.SetActive(false);
        m_DraggedItem = default;
        m_DragSourceInventoryIndex = -1;
        m_DragSourceEquipSlot = null;
    }

    public void DropOnEquipmentSlot(string slotType)
    {
        if (!m_IsDragging || m_DraggedItem.Item == null) return;

        // If dragging from inventory, equip the item
        if (m_DragSourceInventoryIndex >= 0)
        {
            // Check if item can go in this slot
            var category = m_DraggedItem.Item.Category;
            if (category == null) return;

            string categoryName = category.name;
            string itemName = m_DraggedItem.Item.name.ToLower();
            bool canEquip = false;

            if (slotType == "Backpack" && (categoryName.Contains("Backpack") || categoryName.Contains("Bag")))
            {
                canEquip = true;
            }
            else if (slotType == "Flashlight")
            {
                // Check if item is a flashlight
                if (categoryName.Contains("Flashlight") || categoryName.Contains("Light") ||
                    itemName.Contains("flashlight") || itemName.Contains("headlamp") || itemName.Contains("torch"))
                {
                    canEquip = true;
                }
            }
            else if (slotType == "Rifle" || slotType == "Pistol")
            {
                if (categoryName.Contains("Ranged") || categoryName.Contains("Weapon"))
                {
                    // Check if pistol should go to pistol slot
                    bool isPistol = itemName.Contains("pistol") || itemName.Contains("handgun") ||
                                    itemName.Contains("revolver") || itemName.Contains("sr-9") ||
                                    itemName.Contains("9mm") || itemName.Contains("glock");

                    if (slotType == "Pistol" && isPistol) canEquip = true;
                    else if (slotType == "Rifle" && !isPistol) canEquip = true;
                    // Allow swapping - can put any weapon in any weapon slot
                    else canEquip = true;
                }
            }

            if (canEquip)
            {
                // Unequip current item in slot if any
                EquipmentSlot targetSlot = GetEquipmentSlotByType(slotType);

                if (targetSlot != null && targetSlot.EquippedItem.Item != null)
                {
                    UnequipItem(targetSlot);
                }

                // Equip the dragged item
                TryEquipItem(m_DraggedItem, slotType);
            }
        }
        // If dragging from another equipment slot, swap or move
        else if (m_DragSourceEquipSlot != null && m_DragSourceEquipSlot != slotType)
        {
            // For now, just unequip and re-equip to the new slot
            EquipmentSlot sourceSlot = GetEquipmentSlotByType(m_DragSourceEquipSlot);

            if (sourceSlot != null)
            {
                UnequipItem(sourceSlot);
                TryEquipItem(m_DraggedItem, slotType);
            }
        }

        EndDrag();
        RefreshUI();
    }

    public void DropOnInventory()
    {
        if (!m_IsDragging || m_DraggedItem.Item == null) return;

        // If dragging from equipment slot, unequip it
        if (m_DragSourceEquipSlot != null)
        {
            EquipmentSlot sourceSlot = GetEquipmentSlotByType(m_DragSourceEquipSlot);

            if (sourceSlot != null)
            {
                UnequipItem(sourceSlot);
            }
        }

        EndDrag();
        RefreshUI();
    }

    #endregion

    private void OnEquipmentSlotClicked(string slotType)
    {
        EquipmentSlot slot = GetEquipmentSlotByType(slotType);
        if (slot == null) return;

        m_SelectedEquipSlot = slot;

        // If slot has an item, show unequip option
        if (slot.EquippedItem.Item != null)
        {
            m_SelectedItem = slot.EquippedItem;
            m_SelectedSlotIndex = -1; // Mark as equipment slot

            var menuRect = m_ContextMenu.GetComponent<RectTransform>();
            menuRect.position = Input.mousePosition + new Vector3(10, -10, 0);
            m_ContextMenu.SetActive(true);
        }
        else
        {
            Debug.Log($"[SimpleInventoryUI] {slotType} slot is empty. Drag an item here to equip.");
        }
    }

    private void OnSlotClicked(int index)
    {
        if (m_PlayerInventory == null) return;

        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        if (defaultCollection == null)
            defaultCollection = m_PlayerInventory.MainItemCollection;

        var allItems = defaultCollection?.GetAllItemStacks();
        if (allItems == null || index >= allItems.Count) return;

        var itemStack = allItems[index];
        if (itemStack?.Item == null) return;

        m_SelectedItem = (ItemInfo)itemStack;
        m_SelectedSlotIndex = index;

        // Position context menu at mouse
        var menuRect = m_ContextMenu.GetComponent<RectTransform>();
        menuRect.position = Input.mousePosition + new Vector3(10, -10, 0);
        m_ContextMenu.SetActive(true);
    }

    private void OnUseItem()
    {
        if (m_SelectedItem.Item == null) return;

        // If from equipment slot, unequip it
        if (m_SelectedSlotIndex == -1 && m_SelectedEquipSlot != null)
        {
            UnequipItem(m_SelectedEquipSlot);
        }
        else
        {
            // Try to equip item from inventory
            TryEquipItem(m_SelectedItem);
        }

        m_SelectedEquipSlot = null;
        RefreshUI();
    }

    private void TryEquipItem(ItemInfo itemInfo, string targetSlotType = null)
    {
        if (itemInfo.Item == null) return;

        var category = itemInfo.Item.Category;
        if (category == null)
        {
            Debug.Log($"[SimpleInventoryUI] Cannot equip {itemInfo.Item.name} - no category");
            return;
        }

        string categoryName = category.name;
        string itemName = itemInfo.Item.name.ToLower();
        EquipmentSlot targetSlot = null;

        // If target slot type is specified, use that
        if (!string.IsNullOrEmpty(targetSlotType))
        {
            targetSlot = GetEquipmentSlotByType(targetSlotType);
        }
        else
        {
            // Determine target slot based on category
            if (categoryName.Contains("Backpack") || categoryName.Contains("Bag"))
            {
                targetSlot = m_BackpackSlot;
            }
            else if (categoryName.Contains("Flashlight") || categoryName.Contains("Light") ||
                     itemName.Contains("flashlight") || itemName.Contains("headlamp") || itemName.Contains("torch"))
            {
                targetSlot = m_FlashlightSlot;
            }
            else if (categoryName.Contains("Ranged") || categoryName.Contains("Weapon"))
            {
                if (itemName.Contains("pistol") || itemName.Contains("handgun") ||
                    itemName.Contains("revolver") || itemName.Contains("sr-9") ||
                    itemName.Contains("9mm") || itemName.Contains("glock"))
                {
                    targetSlot = m_PistolHolsterSlot;
                }
                else
                {
                    targetSlot = m_RifleHolsterSlot;
                }
            }
        }

        if (targetSlot == null)
        {
            Debug.Log($"[SimpleInventoryUI] No equipment slot for {itemInfo.Item.name}");
            return;
        }

        // If slot already has something, unequip it first
        if (targetSlot.EquippedItem.Item != null)
        {
            UnequipItem(targetSlot);
        }

        // Move item from Default to Equippable collection
        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        var equipCollection = m_PlayerInventory.GetItemCollection(m_EquippableCollectionName);

        if (defaultCollection != null && equipCollection != null)
        {
            // Remove from default
            defaultCollection.RemoveItem(itemInfo);
            // Add to equippable
            equipCollection.AddItem(itemInfo);
            Debug.Log($"[SimpleInventoryUI] Equipped {itemInfo.Item.name} to {targetSlot.SlotType}");

            // Notify flashlight handler if flashlight was equipped
            if (targetSlot == m_FlashlightSlot)
            {
                var flashlightHandler = m_PlayerInventory.GetComponent<FlashlightEquipHandler>();
                if (flashlightHandler != null)
                {
                    flashlightHandler.OnFlashlightEquipped();
                }
            }
        }
    }

    private void UnequipItem(EquipmentSlot slot)
    {
        if (slot == null || slot.EquippedItem.Item == null) return;

        var itemInfo = slot.EquippedItem;

        // Move item from Equippable to Default collection
        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        var equipCollection = m_PlayerInventory.GetItemCollection(m_EquippableCollectionName);

        if (defaultCollection != null && equipCollection != null)
        {
            // Notify flashlight handler if flashlight was unequipped
            if (slot == m_FlashlightSlot)
            {
                var flashlightHandler = m_PlayerInventory.GetComponent<FlashlightEquipHandler>();
                if (flashlightHandler != null)
                {
                    flashlightHandler.OnFlashlightUnequipped();
                }
            }

            // Remove from equippable
            equipCollection.RemoveItem(itemInfo);
            // Add to default
            defaultCollection.AddItem(itemInfo);
            Debug.Log($"[SimpleInventoryUI] Unequipped {itemInfo.Item.name} from {slot.SlotType}");
        }
    }

    private void OnDropItem()
    {
        if (m_SelectedItem.Item == null) return;

        // If from equipment slot, unequip first then drop
        if (m_SelectedSlotIndex == -1 && m_SelectedEquipSlot != null)
        {
            var equipCollection = m_PlayerInventory.GetItemCollection(m_EquippableCollectionName);
            equipCollection?.RemoveItem(m_SelectedItem);
        }
        else
        {
            // Remove item from inventory
            m_PlayerInventory.MainItemCollection?.RemoveItem(m_SelectedItem);
        }

        // Optionally spawn a pickup (would need InventoryDropper reference)
        m_SelectedEquipSlot = null;
        RefreshUI();
    }

    public bool IsOpen => m_IsOpen;
}

/// <summary>
/// Simple hover effect for slots
/// </summary>
public class SlotHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image m_Image;
    private Color m_NormalColor;
    private Color m_HoverColor;

    public void Initialize(Image image, Color normal, Color hover)
    {
        m_Image = image;
        m_NormalColor = normal;
        m_HoverColor = hover;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_Image != null)
            m_Image.color = m_HoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_Image != null)
            m_Image.color = m_NormalColor;
    }
}

/// <summary>
/// Handles dragging items from inventory slots
/// </summary>
public class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SimpleInventoryUI m_InventoryUI;
    private int m_SlotIndex;
    private Image m_IconImage;

    public void Initialize(SimpleInventoryUI inventoryUI, int slotIndex, Image iconImage)
    {
        m_InventoryUI = inventoryUI;
        m_SlotIndex = slotIndex;
        m_IconImage = iconImage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Allow dragging even without an icon (for items without icons like clothing)
        Sprite sprite = (m_IconImage != null && m_IconImage.enabled) ? m_IconImage.sprite : null;
        m_InventoryUI.BeginDragFromInventory(m_SlotIndex, sprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_InventoryUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_InventoryUI.EndDrag();
    }
}

/// <summary>
/// Handles dragging items from equipment slots
/// </summary>
public class EquipmentSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SimpleInventoryUI m_InventoryUI;
    private string m_SlotType;
    private Image m_IconImage;

    public void Initialize(SimpleInventoryUI inventoryUI, string slotType, Image iconImage)
    {
        m_InventoryUI = inventoryUI;
        m_SlotType = slotType;
        m_IconImage = iconImage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_IconImage == null || !m_IconImage.enabled) return;
        m_InventoryUI.BeginDragFromEquipment(m_SlotType, m_IconImage.sprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_InventoryUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_InventoryUI.EndDrag();
    }
}

/// <summary>
/// Handles dropping items onto equipment slots
/// </summary>
public class EquipmentSlotDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private SimpleInventoryUI m_InventoryUI;
    private string m_SlotType;
    private Image m_Border;
    private Color m_NormalColor;
    private Color m_HighlightColor = new Color(0.8f, 0.7f, 0.3f, 1f);

    public void Initialize(SimpleInventoryUI inventoryUI, string slotType, Image border)
    {
        m_InventoryUI = inventoryUI;
        m_SlotType = slotType;
        m_Border = border;
        m_NormalColor = border.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        m_InventoryUI.DropOnEquipmentSlot(m_SlotType);
        if (m_Border != null) m_Border.color = m_NormalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_InventoryUI.IsDragging && m_Border != null)
            m_Border.color = m_HighlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_Border != null)
            m_Border.color = m_NormalColor;
    }
}

/// <summary>
/// Handles dropping items onto inventory grid
/// </summary>
public class InventoryDropHandler : MonoBehaviour, IDropHandler
{
    private SimpleInventoryUI m_InventoryUI;

    public void Initialize(SimpleInventoryUI inventoryUI)
    {
        m_InventoryUI = inventoryUI;
    }

    public void OnDrop(PointerEventData eventData)
    {
        m_InventoryUI.DropOnInventory();
    }
}
