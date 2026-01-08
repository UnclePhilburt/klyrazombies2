using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using TMPro;

/// <summary>
/// Simple loot/storage UI matching the SimpleInventoryUI style.
/// Shows player inventory + equipment on LEFT, container on RIGHT.
/// </summary>
public class SimpleLootUI : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int m_GridColumns = 4;
    [SerializeField] private int m_MaxGridRows = 4;
    [SerializeField] private int m_BasePlayerSlots = 4;
    [SerializeField] private float m_SlotSize = 70f;
    [SerializeField] private float m_SlotSpacing = 15f;
    [SerializeField] private float m_NameHeight = 18f;
    [SerializeField] private float m_GridGap = 50f;

    [Header("Search Animation")]
    [SerializeField] private float m_SearchTimePerItem = 1.5f;
    [SerializeField] private bool m_EnableSearchAnimation = true;
    [SerializeField] private Color m_SearchProgressColor = new Color(1f, 0.8f, 0.3f, 0.8f);
    [SerializeField] private Color m_SearchProgressBgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    [Header("Search Sounds")]
    [SerializeField] private AudioClip m_SearchingLoopSound;
    [SerializeField] private AudioClip m_ItemRevealSound;
    [SerializeField] private AudioClip m_SearchCompleteSound;
    [SerializeField] private AudioClip m_SearchInterruptSound;
    [SerializeField] [Range(0f, 1f)] private float m_SearchSoundVolume = 0.5f;

    [Header("Inventory Sounds")]
    [SerializeField] private AudioClip m_EquipSound;
    [SerializeField] private AudioClip m_UnequipSound;
    [SerializeField] private AudioClip m_MoveItemSound;
    [SerializeField] private AudioClip m_PickupItemSound;
    [SerializeField] [Range(0f, 1f)] private float m_InventorySoundVolume = 0.6f;

    [Header("Equipment Slots")]
    [SerializeField] private float m_EquipSlotSize = 80f;
    [SerializeField] private float m_EquipSlotSpacing = 20f;

    [Header("Colors")]
    [SerializeField] private Color m_OverlayColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color m_SlotColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color m_SlotHoverColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
    [SerializeField] private Color m_ContainerBorderColor = new Color(0.6f, 0.4f, 0.2f, 0.8f);
    [SerializeField] private Color m_PlayerBorderColor = new Color(0.3f, 0.5f, 0.7f, 0.8f);
    [SerializeField] private Color m_EquipBorderColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);
    [SerializeField] private Color m_TextColor = new Color(1f, 1f, 1f, 1f);

    // UI Elements
    private Canvas m_Canvas;
    private GameObject m_OverlayPanel;
    private GameObject m_ContentPanel;
    private GameObject m_ContainerGrid;
    private GameObject m_PlayerGrid;
    private GameObject m_EquipmentContainer;
    private TextMeshProUGUI m_ContainerLabel;
    private TextMeshProUGUI m_PlayerLabel;
    private List<LootSlot> m_ContainerSlots = new List<LootSlot>();
    private List<LootSlot> m_PlayerSlots = new List<LootSlot>();

    // Equipment slots
    private EquipSlot m_BackpackSlot;
    private EquipSlot m_RifleSlot;
    private EquipSlot m_PistolSlot;

    // State
    private bool m_IsOpen = false;
    private bool m_Initialized = false;
    private Inventory m_PlayerInventory;
    private Inventory m_ContainerInventory;

    // Search animation state
    private bool m_IsSearching = false;
    private int m_RevealedItemCount = 0;
    private float m_CurrentItemProgress = 0f;
    private int m_TotalItemsToReveal = 0;
    private List<ItemStack> m_ItemsToReveal = new List<ItemStack>();
    private TextMeshProUGUI m_SearchingText;
    private Image m_CurrentProgressImage;
    private Vector3 m_PlayerSearchStartPos;
    private Sprite m_CircleSprite;
    private AudioSource m_AudioSource;
    private AudioSource m_LoopAudioSource;
    private HashSet<int> m_SearchedContainers = new HashSet<int>();

    // Drag and drop
    private GameObject m_DragIcon;
    private Image m_DragIconImage;
    private bool m_IsDragging = false;
    private ItemInfo m_DraggedItem;
    private bool m_DragFromContainer = false;
    private int m_DragSourceIndex = -1;
    private string m_DragFromEquipSlot = null;

    public bool IsDragging => m_IsDragging;
    public bool IsOpen => m_IsOpen;

    private static SimpleLootUI s_Instance;
    public static SimpleLootUI Instance => s_Instance;

    private class LootSlot
    {
        public GameObject SlotObject;
        public Image Background;
        public Image Border;
        public Image IconImage;
        public TextMeshProUGUI AmountText;
        public TextMeshProUGUI NameText;
        public Button Button;
        public int Index;
        public bool IsContainerSlot;
        public Image ProgressBg;
        public Image ProgressFill;
    }

    private class EquipSlot
    {
        public GameObject SlotObject;
        public Image Background;
        public Image Border;
        public Image IconImage;
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI NameText;
        public Button Button;
        public string SlotType;
        public ItemInfo EquippedItem;
    }

    private void Awake()
    {
        s_Instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (m_Initialized) return;

        CreateUI();
        SetOpen(false);

        m_Initialized = true;
    }

    private void CreateUI()
    {
        // Create circle sprite for progress indicators
        m_CircleSprite = CreateCircleSprite(64);

        // Create audio sources for search sounds
        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;

        m_LoopAudioSource = gameObject.AddComponent<AudioSource>();
        m_LoopAudioSource.playOnAwake = false;
        m_LoopAudioSource.loop = true;

        m_Canvas = FindFirstObjectByType<Canvas>();
        if (m_Canvas == null)
        {
            var canvasObj = new GameObject("LootCanvas");
            m_Canvas = canvasObj.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = 110;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        CreateOverlay();
        CreateContentPanel();
        CreateEquipmentSlots();  // Equipment on far left
        CreatePlayerGrid();       // Player inventory next
        CreateContainerGrid();    // Container on right
        CreateDragIcon();
    }

    private void CreateOverlay()
    {
        m_OverlayPanel = new GameObject("LootOverlay");
        m_OverlayPanel.transform.SetParent(m_Canvas.transform, false);

        var rect = m_OverlayPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = m_OverlayPanel.AddComponent<Image>();
        image.color = m_OverlayColor;
        image.raycastTarget = true;

        var button = m_OverlayPanel.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Close);
    }

    private void CreateContentPanel()
    {
        m_ContentPanel = new GameObject("LootContent");
        m_ContentPanel.transform.SetParent(m_OverlayPanel.transform, false);

        var rect = m_ContentPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Calculate size: Equipment + Player Grid + Gap + Container Grid
        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
        float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;

        float equipWidth = m_EquipSlotSize + m_EquipSlotSpacing;
        float totalWidth = equipWidth + gridWidth + m_GridGap + gridWidth;
        float totalHeight = gridHeight + 40;

        rect.sizeDelta = new Vector2(totalWidth, totalHeight);
    }

    private void CreateEquipmentSlots()
    {
        m_EquipmentContainer = new GameObject("EquipmentSlots");
        m_EquipmentContainer.transform.SetParent(m_ContentPanel.transform, false);

        var rect = m_EquipmentContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        float equipTotalHeight = m_EquipSlotSize + m_NameHeight;
        float totalHeight = 3 * equipTotalHeight + 2 * m_EquipSlotSpacing;
        rect.sizeDelta = new Vector2(m_EquipSlotSize, totalHeight);

        // Create slots: Backpack, Rifle, Pistol
        m_BackpackSlot = CreateEquipSlot("Backpack", 0);
        m_RifleSlot = CreateEquipSlot("Rifle", 1);
        m_PistolSlot = CreateEquipSlot("Pistol", 2);
    }

    private EquipSlot CreateEquipSlot(string slotType, int index)
    {
        var slot = new EquipSlot();
        slot.SlotType = slotType;

        float equipTotalHeight = m_EquipSlotSize + m_NameHeight;
        float y = -index * (equipTotalHeight + m_EquipSlotSpacing);

        slot.SlotObject = new GameObject($"EquipSlot_{slotType}");
        slot.SlotObject.transform.SetParent(m_EquipmentContainer.transform, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(m_EquipSlotSize, m_EquipSlotSize);

        // Border
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slot.SlotObject.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        slot.Border = borderObj.AddComponent<Image>();
        slot.Border.color = m_EquipBorderColor;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slot.SlotObject.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        slot.Background = bgObj.AddComponent<Image>();
        slot.Background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(slot.SlotObject.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1);
        labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, -4);
        labelRect.sizeDelta = new Vector2(m_EquipSlotSize, 14);
        slot.LabelText = labelObj.AddComponent<TextMeshProUGUI>();
        slot.LabelText.text = slotType;
        slot.LabelText.fontSize = 9;
        slot.LabelText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        slot.LabelText.alignment = TextAlignmentOptions.Top;
        slot.LabelText.raycastTarget = false;

        // Icon
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.SlotObject.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6, 6);
        iconRect.offsetMax = new Vector2(-6, -16);
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;
        slot.IconImage.enabled = false;

        // Name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -2);
        nameRect.sizeDelta = new Vector2(m_EquipSlotSize + 10, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = 9;
        slot.NameText.color = m_TextColor;
        slot.NameText.alignment = TextAlignmentOptions.Top;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        string capturedType = slotType;
        slot.Button.onClick.AddListener(() => OnEquipSlotClicked(capturedType));

        // Hover
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, new Color(0.15f, 0.15f, 0.2f, 0.9f), new Color(0.25f, 0.25f, 0.3f, 0.95f));

        // Drag handler
        var dragHandler = slot.SlotObject.AddComponent<LootEquipSlotDragHandler>();
        dragHandler.Initialize(this, slotType, slot.IconImage);

        // Drop handler
        var dropHandler = slot.SlotObject.AddComponent<LootEquipSlotDropHandler>();
        dropHandler.Initialize(this, slotType, slot.Border);

        return slot;
    }

    private void CreatePlayerGrid()
    {
        // Player label - positioned after equipment slots
        float equipWidth = m_EquipSlotSize + m_EquipSlotSpacing;

        var labelObj = new GameObject("PlayerLabel");
        labelObj.transform.SetParent(m_ContentPanel.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(equipWidth, 0);
        labelRect.sizeDelta = new Vector2(200, 30);
        m_PlayerLabel = labelObj.AddComponent<TextMeshProUGUI>();
        m_PlayerLabel.text = "Inventory";
        m_PlayerLabel.fontSize = 18;
        m_PlayerLabel.fontStyle = FontStyles.Bold;
        m_PlayerLabel.color = m_PlayerBorderColor;
        m_PlayerLabel.alignment = TextAlignmentOptions.Left;

        // Player grid
        m_PlayerGrid = new GameObject("PlayerGrid");
        m_PlayerGrid.transform.SetParent(m_ContentPanel.transform, false);

        var rect = m_PlayerGrid.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(equipWidth, -35);

        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
        float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;
        rect.sizeDelta = new Vector2(gridWidth, gridHeight);

        int totalSlots = m_GridColumns * m_MaxGridRows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i, false, m_PlayerGrid.transform, m_PlayerSlots, m_PlayerBorderColor);
        }

        var gridImage = m_PlayerGrid.AddComponent<Image>();
        gridImage.color = new Color(0, 0, 0, 0);
        var dropHandler = m_PlayerGrid.AddComponent<LootGridDropHandler>();
        dropHandler.Initialize(this, false);
    }

    private void CreateContainerGrid()
    {
        // Container label - on the right
        var labelObj = new GameObject("ContainerLabel");
        labelObj.transform.SetParent(m_ContentPanel.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(1, 1);
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(200, 30);
        m_ContainerLabel = labelObj.AddComponent<TextMeshProUGUI>();
        m_ContainerLabel.text = "Container";
        m_ContainerLabel.fontSize = 18;
        m_ContainerLabel.fontStyle = FontStyles.Bold;
        m_ContainerLabel.color = m_ContainerBorderColor;
        m_ContainerLabel.alignment = TextAlignmentOptions.Right;

        // Searching text (shown during search animation)
        var searchingObj = new GameObject("SearchingText");
        searchingObj.transform.SetParent(m_ContentPanel.transform, false);
        var searchRect = searchingObj.AddComponent<RectTransform>();
        searchRect.anchorMin = new Vector2(1, 0.5f);
        searchRect.anchorMax = new Vector2(1, 0.5f);
        searchRect.pivot = new Vector2(1, 0.5f);
        searchRect.anchoredPosition = new Vector2(-50, 0);
        searchRect.sizeDelta = new Vector2(200, 40);
        m_SearchingText = searchingObj.AddComponent<TextMeshProUGUI>();
        m_SearchingText.text = "Searching...";
        m_SearchingText.fontSize = 16;
        m_SearchingText.fontStyle = FontStyles.Italic;
        m_SearchingText.color = new Color(1f, 0.9f, 0.7f, 0.9f);
        m_SearchingText.alignment = TextAlignmentOptions.Center;
        searchingObj.SetActive(false);

        // Container grid
        m_ContainerGrid = new GameObject("ContainerGrid");
        m_ContainerGrid.transform.SetParent(m_ContentPanel.transform, false);

        var rect = m_ContainerGrid.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(0, -35);

        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float gridWidth = m_GridColumns * m_SlotSize + (m_GridColumns - 1) * m_SlotSpacing;
        float gridHeight = m_MaxGridRows * slotTotalHeight + (m_MaxGridRows - 1) * m_SlotSpacing;
        rect.sizeDelta = new Vector2(gridWidth, gridHeight);

        int totalSlots = m_GridColumns * m_MaxGridRows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i, true, m_ContainerGrid.transform, m_ContainerSlots, m_ContainerBorderColor);
        }

        var gridImage = m_ContainerGrid.AddComponent<Image>();
        gridImage.color = new Color(0, 0, 0, 0);
        var dropHandler = m_ContainerGrid.AddComponent<LootGridDropHandler>();
        dropHandler.Initialize(this, true);
    }

    private void CreateSlot(int index, bool isContainer, Transform parent, List<LootSlot> slotList, Color borderColor)
    {
        int row = index / m_GridColumns;
        int col = index % m_GridColumns;

        float slotTotalHeight = m_SlotSize + m_NameHeight;
        float x = col * (m_SlotSize + m_SlotSpacing);
        float y = -row * (slotTotalHeight + m_SlotSpacing);

        var slot = new LootSlot();
        slot.Index = index;
        slot.IsContainerSlot = isContainer;

        slot.SlotObject = new GameObject($"Slot_{index}");
        slot.SlotObject.transform.SetParent(parent, false);

        var rect = slot.SlotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
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
        slot.Border.color = borderColor;

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
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);
        slot.IconImage = iconObj.AddComponent<Image>();
        slot.IconImage.preserveAspect = true;
        slot.IconImage.raycastTarget = false;

        // Progress circle (only for container slots)
        if (isContainer)
        {
            // Progress background
            var progressBgObj = new GameObject("ProgressBg");
            progressBgObj.transform.SetParent(slot.SlotObject.transform, false);
            var progressBgRect = progressBgObj.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressBgRect.sizeDelta = new Vector2(40, 40);
            slot.ProgressBg = progressBgObj.AddComponent<Image>();
            slot.ProgressBg.sprite = m_CircleSprite;
            slot.ProgressBg.color = m_SearchProgressBgColor;
            slot.ProgressBg.type = Image.Type.Filled;
            slot.ProgressBg.fillMethod = Image.FillMethod.Radial360;
            slot.ProgressBg.fillAmount = 1f;
            slot.ProgressBg.raycastTarget = false;
            progressBgObj.SetActive(false);

            // Progress fill
            var progressFillObj = new GameObject("ProgressFill");
            progressFillObj.transform.SetParent(slot.SlotObject.transform, false);
            var progressFillRect = progressFillObj.AddComponent<RectTransform>();
            progressFillRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressFillRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressFillRect.sizeDelta = new Vector2(40, 40);
            slot.ProgressFill = progressFillObj.AddComponent<Image>();
            slot.ProgressFill.sprite = m_CircleSprite;
            slot.ProgressFill.color = m_SearchProgressColor;
            slot.ProgressFill.type = Image.Type.Filled;
            slot.ProgressFill.fillMethod = Image.FillMethod.Radial360;
            slot.ProgressFill.fillOrigin = (int)Image.Origin360.Top;
            slot.ProgressFill.fillClockwise = true;
            slot.ProgressFill.fillAmount = 0f;
            slot.ProgressFill.raycastTarget = false;
            progressFillObj.SetActive(false);
        }

        // Amount
        var amountObj = new GameObject("Amount");
        amountObj.transform.SetParent(slot.SlotObject.transform, false);
        var amountRect = amountObj.AddComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1, 0);
        amountRect.anchorMax = new Vector2(1, 0);
        amountRect.pivot = new Vector2(1, 0);
        amountRect.anchoredPosition = new Vector2(-3, 3);
        amountRect.sizeDelta = new Vector2(35, 18);
        slot.AmountText = amountObj.AddComponent<TextMeshProUGUI>();
        slot.AmountText.fontSize = 11;
        slot.AmountText.fontStyle = FontStyles.Bold;
        slot.AmountText.color = m_TextColor;
        slot.AmountText.alignment = TextAlignmentOptions.BottomRight;
        slot.AmountText.raycastTarget = false;

        // Name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.SlotObject.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0);
        nameRect.anchorMax = new Vector2(0.5f, 0);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -2);
        nameRect.sizeDelta = new Vector2(m_SlotSize + m_SlotSpacing, m_NameHeight);
        slot.NameText = nameObj.AddComponent<TextMeshProUGUI>();
        slot.NameText.fontSize = 9;
        slot.NameText.color = m_TextColor;
        slot.NameText.alignment = TextAlignmentOptions.Top;
        slot.NameText.enableWordWrapping = false;
        slot.NameText.overflowMode = TextOverflowModes.Ellipsis;
        slot.NameText.raycastTarget = false;

        // Button
        slot.Button = slot.SlotObject.AddComponent<Button>();
        slot.Button.transition = Selectable.Transition.None;
        int slotIndex = index;
        bool containerSlot = isContainer;
        slot.Button.onClick.AddListener(() => OnSlotClicked(slotIndex, containerSlot));

        // Hover
        var hoverHandler = slot.SlotObject.AddComponent<SlotHoverHandler>();
        hoverHandler.Initialize(slot.Background, m_SlotColor, m_SlotHoverColor);

        // Drag handler
        var dragHandler = slot.SlotObject.AddComponent<LootSlotDragHandler>();
        dragHandler.Initialize(this, index, isContainer, slot.IconImage);

        slotList.Add(slot);
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
        m_DragIconImage.raycastTarget = false;

        var canvasGroup = m_DragIcon.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        m_DragIcon.SetActive(false);
    }

    private void Update()
    {
        if (!m_IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Tab))
        {
            Close();
        }

        // Handle search animation
        if (m_IsSearching)
        {
            // Check if player moved (interrupt search)
            if (m_PlayerInventory != null)
            {
                float moveDistance = Vector3.Distance(m_PlayerInventory.transform.position, m_PlayerSearchStartPos);
                if (moveDistance > 0.3f)
                {
                    InterruptSearch();
                    Close();
                    return;
                }
            }

            // Progress the current item search
            m_CurrentItemProgress += Time.deltaTime / m_SearchTimePerItem;

            // Update progress circle
            if (m_RevealedItemCount < m_ContainerSlots.Count)
            {
                var currentSlot = m_ContainerSlots[m_RevealedItemCount];
                if (currentSlot.ProgressFill != null)
                {
                    currentSlot.ProgressFill.fillAmount = m_CurrentItemProgress;
                }
            }

            // Check if current item is done
            if (m_CurrentItemProgress >= 1f)
            {
                RevealNextItem();
                m_CurrentItemProgress = 0f;

                // Start next item's progress indicator
                if (m_RevealedItemCount < m_TotalItemsToReveal && m_RevealedItemCount < m_ContainerSlots.Count)
                {
                    ShowProgressOnSlot(m_RevealedItemCount);
                }
            }

            // Update searching text
            if (m_SearchingText != null)
            {
                int dots = (int)(Time.time * 3f) % 4;
                m_SearchingText.text = "Searching" + new string('.', dots);
            }
        }
    }

    #region Public API

    public void Open(Inventory playerInventory, Inventory containerInventory, string containerName = "Container")
    {
        if (!m_Initialized) Initialize();

        m_PlayerInventory = playerInventory;
        m_ContainerInventory = containerInventory;

        if (m_ContainerLabel != null)
            m_ContainerLabel.text = containerName;

        SetOpen(true);

        // Refresh player side immediately
        RefreshEquipmentSlots();
        RefreshPlayerGrid();

        // Check if this container was already searched
        int containerID = m_ContainerInventory.GetInstanceID();
        bool alreadySearched = m_SearchedContainers.Contains(containerID);

        // Start search animation for container (only if not already searched)
        if (m_EnableSearchAnimation && !alreadySearched)
        {
            StartSearch();
        }
        else
        {
            RefreshContainerGrid();
        }
    }

    public void Close()
    {
        m_IsSearching = false;

        // Stop any playing sounds
        if (m_LoopAudioSource != null)
            m_LoopAudioSource.Stop();

        if (m_SearchingText != null)
            m_SearchingText.gameObject.SetActive(false);

        SetOpen(false);
        m_PlayerInventory = null;
        m_ContainerInventory = null;
    }

    private void SetOpen(bool open)
    {
        m_IsOpen = open;

        if (m_OverlayPanel != null)
            m_OverlayPanel.SetActive(open);

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

        if (!open)
        {
            EndDrag();
        }
    }

    #endregion

    #region Refresh UI

    public void RefreshUI()
    {
        RefreshEquipmentSlots();
        RefreshContainerGrid();
        RefreshPlayerGrid();
    }

    #endregion

    #region Search Animation

    private void StartSearch()
    {
        // Clear all container slots and hide progress
        foreach (var slot in m_ContainerSlots)
        {
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
            slot.AmountText.text = "";
            slot.NameText.text = "";
            slot.SlotObject.SetActive(true);
            HideProgressOnSlot(slot);
        }

        // Get items to reveal
        m_ItemsToReveal.Clear();
        if (m_ContainerInventory != null)
        {
            var collection = m_ContainerInventory.MainItemCollection;
            var allItems = collection?.GetAllItemStacks();
            if (allItems != null)
            {
                foreach (var item in allItems)
                {
                    if (item?.Item != null)
                        m_ItemsToReveal.Add(item);
                }
            }
        }

        m_TotalItemsToReveal = m_ItemsToReveal.Count;
        m_RevealedItemCount = 0;
        m_CurrentItemProgress = 0f;
        m_IsSearching = true;

        // Record player position for movement detection
        if (m_PlayerInventory != null)
            m_PlayerSearchStartPos = m_PlayerInventory.transform.position;

        // Show searching text
        if (m_SearchingText != null)
        {
            m_SearchingText.gameObject.SetActive(true);
            m_SearchingText.text = "Searching...";
        }

        // If no items, complete immediately
        if (m_TotalItemsToReveal == 0)
        {
            CompleteSearch();
        }
        else
        {
            // Start looping search sound
            if (m_SearchingLoopSound != null && m_LoopAudioSource != null)
            {
                m_LoopAudioSource.clip = m_SearchingLoopSound;
                m_LoopAudioSource.volume = m_SearchSoundVolume;
                m_LoopAudioSource.Play();
            }

            // Show progress on first slot
            ShowProgressOnSlot(0);
        }
    }

    private void ShowProgressOnSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_ContainerSlots.Count) return;

        var slot = m_ContainerSlots[slotIndex];
        if (slot.ProgressBg != null)
        {
            slot.ProgressBg.gameObject.SetActive(true);
            slot.ProgressBg.fillAmount = 1f;
        }
        if (slot.ProgressFill != null)
        {
            slot.ProgressFill.gameObject.SetActive(true);
            slot.ProgressFill.fillAmount = 0f;
        }
    }

    private void HideProgressOnSlot(LootSlot slot)
    {
        if (slot.ProgressBg != null)
            slot.ProgressBg.gameObject.SetActive(false);
        if (slot.ProgressFill != null)
            slot.ProgressFill.gameObject.SetActive(false);
    }

    private void RevealNextItem()
    {
        if (m_RevealedItemCount >= m_TotalItemsToReveal || m_RevealedItemCount >= m_ContainerSlots.Count)
        {
            CompleteSearch();
            return;
        }

        var itemStack = m_ItemsToReveal[m_RevealedItemCount];
        var slot = m_ContainerSlots[m_RevealedItemCount];

        // Hide progress indicator
        HideProgressOnSlot(slot);

        // Play item reveal sound
        if (m_ItemRevealSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_ItemRevealSound, m_SearchSoundVolume);
        }

        // Show the item
        var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");
        PopulateSlot(slot, itemStack, iconDb);

        m_RevealedItemCount++;

        // Check if done
        if (m_RevealedItemCount >= m_TotalItemsToReveal)
        {
            CompleteSearch();
        }
    }

    private void CompleteSearch()
    {
        m_IsSearching = false;

        // Mark this container as searched
        if (m_ContainerInventory != null)
        {
            m_SearchedContainers.Add(m_ContainerInventory.GetInstanceID());
        }

        // Stop looping sound
        if (m_LoopAudioSource != null)
            m_LoopAudioSource.Stop();

        // Play complete sound
        if (m_SearchCompleteSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_SearchCompleteSound, m_SearchSoundVolume);
        }

        // Hide searching text
        if (m_SearchingText != null)
            m_SearchingText.gameObject.SetActive(false);

        // Hide all progress indicators and reveal remaining items
        var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");
        for (int i = 0; i < m_ContainerSlots.Count; i++)
        {
            var slot = m_ContainerSlots[i];
            HideProgressOnSlot(slot);

            // Reveal unrevealed items
            if (i >= m_RevealedItemCount && i < m_TotalItemsToReveal)
            {
                var itemStack = m_ItemsToReveal[i];
                PopulateSlot(slot, itemStack, iconDb);
            }
        }

        m_RevealedItemCount = m_TotalItemsToReveal;
    }

    private void InterruptSearch()
    {
        m_IsSearching = false;

        // Stop looping sound
        if (m_LoopAudioSource != null)
            m_LoopAudioSource.Stop();

        // Play interrupt sound
        if (m_SearchInterruptSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_SearchInterruptSound, m_SearchSoundVolume);
        }

        // Hide searching text
        if (m_SearchingText != null)
            m_SearchingText.gameObject.SetActive(false);

        // Hide all progress indicators (items already revealed stay revealed)
        foreach (var slot in m_ContainerSlots)
        {
            HideProgressOnSlot(slot);
        }
    }

    #endregion

    #region Equipment Slots

    private void RefreshEquipmentSlots()
    {
        ClearEquipSlot(m_BackpackSlot);
        ClearEquipSlot(m_RifleSlot);
        ClearEquipSlot(m_PistolSlot);

        if (m_PlayerInventory == null) return;

        var equipCollection = m_PlayerInventory.GetItemCollection("Equippable");
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
            EquipSlot targetSlot = null;

            if (categoryName.Contains("Backpack") || categoryName.Contains("Bag"))
            {
                targetSlot = m_BackpackSlot;
            }
            else if (categoryName.Contains("Ranged") || categoryName.Contains("Weapon"))
            {
                string itemName = item.name.ToLower();
                if (itemName.Contains("pistol") || itemName.Contains("handgun") ||
                    itemName.Contains("revolver") || itemName.Contains("sr-9") ||
                    itemName.Contains("9mm") || itemName.Contains("glock"))
                {
                    targetSlot = m_PistolSlot;
                }
                else
                {
                    targetSlot = m_RifleSlot;
                }
            }

            if (targetSlot == null || targetSlot.EquippedItem.Item != null) continue;

            targetSlot.EquippedItem = (ItemInfo)itemStack;

            Sprite icon = null;
            if (iconDb != null)
                icon = iconDb.GetIcon(item.ItemDefinition);
            if (icon == null && item.TryGetAttributeValue<Sprite>("Icon", out var attrIcon))
                icon = attrIcon;

            if (icon != null)
            {
                targetSlot.IconImage.sprite = icon;
                targetSlot.IconImage.enabled = true;
            }

            targetSlot.NameText.text = item.name;
        }
    }

    private void ClearEquipSlot(EquipSlot slot)
    {
        if (slot == null) return;
        slot.IconImage.sprite = null;
        slot.IconImage.enabled = false;
        slot.NameText.text = "";
        slot.EquippedItem = default;
    }

    private void RefreshContainerGrid()
    {
        foreach (var slot in m_ContainerSlots)
        {
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
            slot.AmountText.text = "";
            slot.NameText.text = "";
            slot.SlotObject.SetActive(true);
        }

        if (m_ContainerInventory == null) return;

        var collection = m_ContainerInventory.MainItemCollection;
        var allItems = collection?.GetAllItemStacks();
        if (allItems == null) return;

        var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");

        int slotIndex = 0;
        for (int i = 0; i < allItems.Count && slotIndex < m_ContainerSlots.Count; i++)
        {
            var itemStack = allItems[i];
            if (itemStack?.Item == null) continue;

            var slot = m_ContainerSlots[slotIndex];
            PopulateSlot(slot, itemStack, iconDb);
            slotIndex++;
        }
    }

    private void RefreshPlayerGrid()
    {
        int availableSlots = GetPlayerAvailableSlots();

        for (int i = 0; i < m_PlayerSlots.Count; i++)
        {
            var slot = m_PlayerSlots[i];
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
            slot.AmountText.text = "";
            slot.NameText.text = "";
            slot.SlotObject.SetActive(i < availableSlots);
        }

        if (m_PlayerInventory == null) return;

        var collection = m_PlayerInventory.GetItemCollection("Default");
        if (collection == null)
            collection = m_PlayerInventory.MainItemCollection;

        var allItems = collection?.GetAllItemStacks();
        if (allItems == null) return;

        var iconDb = Resources.Load<ItemIconDatabase>("ItemIconDatabase");

        int slotIndex = 0;
        for (int i = 0; i < allItems.Count && slotIndex < availableSlots; i++)
        {
            var itemStack = allItems[i];
            if (itemStack?.Item == null) continue;

            var slot = m_PlayerSlots[slotIndex];
            PopulateSlot(slot, itemStack, iconDb);
            slotIndex++;
        }
    }

    private void PopulateSlot(LootSlot slot, ItemStack itemStack, ItemIconDatabase iconDb)
    {
        Sprite icon = null;
        if (iconDb != null)
            icon = iconDb.GetIcon(itemStack.Item.ItemDefinition);
        if (icon == null && itemStack.Item.TryGetAttributeValue<Sprite>("Icon", out var attrIcon))
            icon = attrIcon;

        if (icon != null)
        {
            slot.IconImage.sprite = icon;
            slot.IconImage.enabled = true;
        }

        slot.NameText.text = itemStack.Item.name;

        if (itemStack.Amount > 1)
            slot.AmountText.text = itemStack.Amount.ToString();
    }

    private int GetPlayerAvailableSlots()
    {
        int slots = m_BasePlayerSlots;

        if (m_BackpackSlot != null && m_BackpackSlot.EquippedItem.Item != null)
        {
            if (m_BackpackSlot.EquippedItem.Item.TryGetAttributeValue<int>("BagSize", out int bagSize))
                slots += bagSize;
            else
                slots += 8;
        }

        int maxSlots = m_GridColumns * m_MaxGridRows;
        return Mathf.Min(slots, maxSlots);
    }

    #endregion

    #region Slot Interaction

    private void OnSlotClicked(int index, bool isContainer)
    {
        if (isContainer)
            TransferFromContainer(index);
        else
            TransferFromPlayer(index);
    }

    private void OnEquipSlotClicked(string slotType)
    {
        EquipSlot slot = GetEquipSlot(slotType);
        if (slot == null || slot.EquippedItem.Item == null) return;

        // Unequip to player inventory
        UnequipItem(slot);
        RefreshUI();
    }

    private EquipSlot GetEquipSlot(string slotType)
    {
        if (slotType == "Backpack") return m_BackpackSlot;
        if (slotType == "Rifle") return m_RifleSlot;
        if (slotType == "Pistol") return m_PistolSlot;
        return null;
    }

    private void TransferFromContainer(int slotIndex)
    {
        if (m_ContainerInventory == null || m_PlayerInventory == null) return;

        var containerCollection = m_ContainerInventory.MainItemCollection;
        var playerCollection = m_PlayerInventory.GetItemCollection("Default") ?? m_PlayerInventory.MainItemCollection;

        var allItems = containerCollection?.GetAllItemStacks();
        if (allItems == null || slotIndex >= allItems.Count) return;

        var itemStack = allItems[slotIndex];
        if (itemStack?.Item == null) return;

        var itemInfo = (ItemInfo)itemStack;

        var addResult = playerCollection.AddItem(itemInfo);
        if (addResult.Amount > 0)
        {
            containerCollection.RemoveItem(new ItemInfo(itemInfo.Item, addResult.Amount));

            // Play pickup sound
            if (m_PickupItemSound != null && m_AudioSource != null)
                m_AudioSource.PlayOneShot(m_PickupItemSound, m_InventorySoundVolume);
        }

        RefreshUI();
    }

    private void TransferFromPlayer(int slotIndex)
    {
        if (m_ContainerInventory == null || m_PlayerInventory == null) return;

        var playerCollection = m_PlayerInventory.GetItemCollection("Default") ?? m_PlayerInventory.MainItemCollection;
        var containerCollection = m_ContainerInventory.MainItemCollection;

        var allItems = playerCollection?.GetAllItemStacks();
        if (allItems == null || slotIndex >= allItems.Count) return;

        var itemStack = allItems[slotIndex];
        if (itemStack?.Item == null) return;

        var itemInfo = (ItemInfo)itemStack;

        var addResult = containerCollection.AddItem(itemInfo);
        if (addResult.Amount > 0)
        {
            playerCollection.RemoveItem(new ItemInfo(itemInfo.Item, addResult.Amount));

            // Play move sound
            if (m_MoveItemSound != null && m_AudioSource != null)
                m_AudioSource.PlayOneShot(m_MoveItemSound, m_InventorySoundVolume);
        }

        RefreshUI();
    }

    private void TryEquipItem(ItemInfo itemInfo)
    {
        if (itemInfo.Item == null || m_PlayerInventory == null) return;

        var category = itemInfo.Item.Category;
        if (category == null) return;

        string categoryName = category.name;
        EquipSlot targetSlot = null;

        if (categoryName.Contains("Backpack") || categoryName.Contains("Bag"))
        {
            targetSlot = m_BackpackSlot;
        }
        else if (categoryName.Contains("Ranged") || categoryName.Contains("Weapon"))
        {
            string itemName = itemInfo.Item.name.ToLower();
            if (itemName.Contains("pistol") || itemName.Contains("handgun") ||
                itemName.Contains("revolver") || itemName.Contains("sr-9") ||
                itemName.Contains("9mm") || itemName.Contains("glock"))
            {
                targetSlot = m_PistolSlot;
            }
            else
            {
                targetSlot = m_RifleSlot;
            }
        }

        if (targetSlot == null) return;

        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        var equipCollection = m_PlayerInventory.GetItemCollection("Equippable");

        if (defaultCollection == null || equipCollection == null) return;

        // Check for existing equipped item
        ItemInfo oldEquippedItem = default;
        if (targetSlot.EquippedItem.Item != null)
        {
            oldEquippedItem = targetSlot.EquippedItem;
        }

        // Remove the new item from inventory first
        defaultCollection.RemoveItem(itemInfo);

        // If there's an old equipped item, swap it to inventory
        if (oldEquippedItem.Item != null)
        {
            equipCollection.RemoveItem(oldEquippedItem);
            defaultCollection.AddItem(oldEquippedItem);
        }

        // Equip the new item
        equipCollection.AddItem(itemInfo);

        // Play equip sound
        if (m_EquipSound != null && m_AudioSource != null)
            m_AudioSource.PlayOneShot(m_EquipSound, m_InventorySoundVolume);
    }

    private void UnequipItem(EquipSlot slot)
    {
        if (slot == null || slot.EquippedItem.Item == null || m_PlayerInventory == null) return;

        var itemInfo = slot.EquippedItem;
        var defaultCollection = m_PlayerInventory.GetItemCollection("Default");
        var equipCollection = m_PlayerInventory.GetItemCollection("Equippable");

        if (defaultCollection != null && equipCollection != null)
        {
            equipCollection.RemoveItem(itemInfo);
            defaultCollection.AddItem(itemInfo);

            // Play unequip sound
            if (m_UnequipSound != null && m_AudioSource != null)
                m_AudioSource.PlayOneShot(m_UnequipSound, m_InventorySoundVolume);
        }
    }

    #endregion

    #region Drag and Drop

    public void BeginDrag(int slotIndex, bool fromContainer, Sprite icon)
    {
        Inventory inventory = fromContainer ? m_ContainerInventory : m_PlayerInventory;
        if (inventory == null) return;

        ItemCollection collection;
        if (fromContainer)
            collection = inventory.MainItemCollection;
        else
            collection = inventory.GetItemCollection("Default") ?? inventory.MainItemCollection;

        var allItems = collection?.GetAllItemStacks();
        if (allItems == null || slotIndex >= allItems.Count) return;

        var itemStack = allItems[slotIndex];
        if (itemStack?.Item == null) return;

        m_DraggedItem = (ItemInfo)itemStack;
        m_DragFromContainer = fromContainer;
        m_DragSourceIndex = slotIndex;
        m_DragFromEquipSlot = null;
        m_IsDragging = true;

        m_DragIconImage.sprite = icon;
        // If no icon, show a colored placeholder
        m_DragIconImage.color = icon != null ? Color.white : new Color(0.6f, 0.5f, 0.3f, 0.8f);
        m_DragIcon.SetActive(true);
    }

    public void BeginDragFromEquip(string slotType, Sprite icon)
    {
        EquipSlot slot = GetEquipSlot(slotType);
        if (slot == null || slot.EquippedItem.Item == null) return;

        m_DraggedItem = slot.EquippedItem;
        m_DragFromContainer = false;
        m_DragSourceIndex = -1;
        m_DragFromEquipSlot = slotType;
        m_IsDragging = true;

        m_DragIconImage.sprite = icon;
        m_DragIconImage.color = icon != null ? Color.white : new Color(0.6f, 0.5f, 0.3f, 0.8f);
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
        m_DragSourceIndex = -1;
        m_DragFromEquipSlot = null;
    }

    /// <summary>
    /// Check if a slot has an item (for drag handler to use instead of checking icon)
    /// </summary>
    public bool SlotHasItem(int slotIndex, bool isContainer)
    {
        Inventory inventory = isContainer ? m_ContainerInventory : m_PlayerInventory;
        if (inventory == null) return false;

        ItemCollection collection;
        if (isContainer)
            collection = inventory.MainItemCollection;
        else
            collection = inventory.GetItemCollection("Default") ?? inventory.MainItemCollection;

        var allItems = collection?.GetAllItemStacks();
        if (allItems == null || slotIndex >= allItems.Count) return false;

        var itemStack = allItems[slotIndex];
        return itemStack?.Item != null;
    }

    public void DropOnGrid(bool toContainer)
    {
        if (!m_IsDragging || m_DraggedItem.Item == null) return;

        // From equip slot to container
        if (m_DragFromEquipSlot != null && toContainer)
        {
            EquipSlot sourceSlot = GetEquipSlot(m_DragFromEquipSlot);
            if (sourceSlot != null)
            {
                UnequipItem(sourceSlot);
                // Then move to container
                var playerCollection = m_PlayerInventory.GetItemCollection("Default") ?? m_PlayerInventory.MainItemCollection;
                var containerCollection = m_ContainerInventory.MainItemCollection;

                var addResult = containerCollection.AddItem(m_DraggedItem);
                if (addResult.Amount > 0)
                    playerCollection.RemoveItem(new ItemInfo(m_DraggedItem.Item, addResult.Amount));
            }
            EndDrag();
            RefreshUI();
            return;
        }

        // From equip slot to player inventory (unequip)
        if (m_DragFromEquipSlot != null && !toContainer)
        {
            EquipSlot sourceSlot = GetEquipSlot(m_DragFromEquipSlot);
            if (sourceSlot != null)
                UnequipItem(sourceSlot);
            EndDrag();
            RefreshUI();
            return;
        }

        // Same grid - do nothing
        if (toContainer == m_DragFromContainer)
        {
            EndDrag();
            return;
        }

        // Transfer between grids
        Inventory sourceInv = m_DragFromContainer ? m_ContainerInventory : m_PlayerInventory;
        Inventory destInv = toContainer ? m_ContainerInventory : m_PlayerInventory;

        if (sourceInv == null || destInv == null)
        {
            EndDrag();
            return;
        }

        ItemCollection sourceCollection, destCollection;
        if (m_DragFromContainer)
            sourceCollection = sourceInv.MainItemCollection;
        else
            sourceCollection = sourceInv.GetItemCollection("Default") ?? sourceInv.MainItemCollection;

        if (toContainer)
            destCollection = destInv.MainItemCollection;
        else
            destCollection = destInv.GetItemCollection("Default") ?? destInv.MainItemCollection;

        var transferResult = destCollection.AddItem(m_DraggedItem);
        if (transferResult.Amount > 0)
        {
            sourceCollection.RemoveItem(new ItemInfo(m_DraggedItem.Item, transferResult.Amount));

            // Play appropriate sound
            if (m_DragFromContainer)
            {
                // Picking up from container
                if (m_PickupItemSound != null && m_AudioSource != null)
                    m_AudioSource.PlayOneShot(m_PickupItemSound, m_InventorySoundVolume);
            }
            else
            {
                // Putting into container
                if (m_MoveItemSound != null && m_AudioSource != null)
                    m_AudioSource.PlayOneShot(m_MoveItemSound, m_InventorySoundVolume);
            }
        }

        EndDrag();
        RefreshUI();
    }

    public void DropOnEquipSlot(string slotType)
    {
        if (!m_IsDragging || m_DraggedItem.Item == null) return;

        // Can only equip from container or player inventory (not from another equip slot)
        if (m_DragFromEquipSlot != null)
        {
            EndDrag();
            return;
        }

        // Check if item can go in this slot
        var category = m_DraggedItem.Item.Category;
        if (category == null)
        {
            EndDrag();
            return;
        }

        string categoryName = category.name;
        bool canEquip = false;

        if (slotType == "Backpack" && (categoryName.Contains("Backpack") || categoryName.Contains("Bag")))
            canEquip = true;
        else if ((slotType == "Rifle" || slotType == "Pistol") &&
                 (categoryName.Contains("Ranged") || categoryName.Contains("Weapon")))
            canEquip = true;

        if (!canEquip)
        {
            EndDrag();
            return;
        }

        var equipCollection = m_PlayerInventory.GetItemCollection("Equippable");
        if (equipCollection == null)
        {
            EndDrag();
            return;
        }

        // Get the target slot and check for existing item
        EquipSlot targetSlot = GetEquipSlot(slotType);
        ItemInfo oldEquippedItem = default;

        if (targetSlot != null && targetSlot.EquippedItem.Item != null)
        {
            oldEquippedItem = targetSlot.EquippedItem;
        }

        // If from container
        if (m_DragFromContainer)
        {
            var containerCollection = m_ContainerInventory.MainItemCollection;

            // Remove the new item from container first
            containerCollection.RemoveItem(m_DraggedItem);

            // If there's an old equipped item, move it to the container (swap)
            if (oldEquippedItem.Item != null)
            {
                equipCollection.RemoveItem(oldEquippedItem);
                containerCollection.AddItem(oldEquippedItem);
            }

            // Equip the new item
            equipCollection.AddItem(m_DraggedItem);

            // Play equip sound
            if (m_EquipSound != null && m_AudioSource != null)
                m_AudioSource.PlayOneShot(m_EquipSound, m_InventorySoundVolume);
        }
        else
        {
            // From player inventory
            var defaultCollection = m_PlayerInventory.GetItemCollection("Default") ?? m_PlayerInventory.MainItemCollection;

            // Remove the new item from player inventory first
            defaultCollection.RemoveItem(m_DraggedItem);

            // If there's an old equipped item, move it to player inventory (swap)
            if (oldEquippedItem.Item != null)
            {
                equipCollection.RemoveItem(oldEquippedItem);
                defaultCollection.AddItem(oldEquippedItem);
            }

            // Equip the new item
            equipCollection.AddItem(m_DraggedItem);

            // Play equip sound
            if (m_EquipSound != null && m_AudioSource != null)
                m_AudioSource.PlayOneShot(m_EquipSound, m_InventorySoundVolume);
        }

        EndDrag();
        RefreshUI();
    }

    #endregion

    #region Utility

    private Sprite CreateCircleSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        float radiusSq = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float distSq = dx * dx + dy * dy;

                if (distSq <= radiusSq)
                {
                    // Smooth edge with anti-aliasing
                    float dist = Mathf.Sqrt(distSq);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    #endregion
}

// Drag handler for loot slots
public class LootSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SimpleLootUI m_LootUI;
    private int m_SlotIndex;
    private bool m_IsContainer;
    private Image m_IconImage;

    public void Initialize(SimpleLootUI lootUI, int slotIndex, bool isContainer, Image iconImage)
    {
        m_LootUI = lootUI;
        m_SlotIndex = slotIndex;
        m_IsContainer = isContainer;
        m_IconImage = iconImage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if slot actually has an item (don't rely on icon being enabled)
        if (!m_LootUI.SlotHasItem(m_SlotIndex, m_IsContainer)) return;
        m_LootUI.BeginDrag(m_SlotIndex, m_IsContainer, m_IconImage?.sprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_LootUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_LootUI.EndDrag();
    }
}

// Drop handler for loot grids
public class LootGridDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private SimpleLootUI m_LootUI;
    private bool m_IsContainer;
    private Image m_GridImage;

    public void Initialize(SimpleLootUI lootUI, bool isContainer)
    {
        m_LootUI = lootUI;
        m_IsContainer = isContainer;
        m_GridImage = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        m_LootUI.DropOnGrid(m_IsContainer);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_LootUI.IsDragging && m_GridImage != null)
            m_GridImage.color = new Color(1, 1, 1, 0.1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_GridImage != null)
            m_GridImage.color = new Color(0, 0, 0, 0);
    }
}

// Drag handler for equipment slots in loot UI
public class LootEquipSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SimpleLootUI m_LootUI;
    private string m_SlotType;
    private Image m_IconImage;

    public void Initialize(SimpleLootUI lootUI, string slotType, Image iconImage)
    {
        m_LootUI = lootUI;
        m_SlotType = slotType;
        m_IconImage = iconImage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_IconImage == null || !m_IconImage.enabled) return;
        m_LootUI.BeginDragFromEquip(m_SlotType, m_IconImage.sprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_LootUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_LootUI.EndDrag();
    }
}

// Drop handler for equipment slots in loot UI
public class LootEquipSlotDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private SimpleLootUI m_LootUI;
    private string m_SlotType;
    private Image m_Border;
    private Color m_NormalColor;
    private Color m_HighlightColor = new Color(0.8f, 0.7f, 0.3f, 1f);

    public void Initialize(SimpleLootUI lootUI, string slotType, Image border)
    {
        m_LootUI = lootUI;
        m_SlotType = slotType;
        m_Border = border;
        m_NormalColor = border.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        m_LootUI.DropOnEquipSlot(m_SlotType);
        if (m_Border != null) m_Border.color = m_NormalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_LootUI.IsDragging && m_Border != null)
            m_Border.color = m_HighlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_Border != null)
            m_Border.color = m_NormalColor;
    }
}
