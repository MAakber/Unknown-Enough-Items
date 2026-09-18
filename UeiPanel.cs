using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class UeiPanel
{
    private const int Columns = 6;
    private const int Rows = 10;
    private const int PageSize = Columns * Rows;
    private const float HeaderHeight = 22f;
    private const float PageButtonWidth = 26f;
    private const float PageTextWidth = 64f;
    private const float PageTextSize = 13f;
    private const float CategoryBarHeight = 20f;
    private const float CategoryButtonWidth = 76f;
    private const float SearchInputHeight = 22f;
    private const float SearchTextSize = 11f;
    private const float SearchBorderThickness = 1f;
    private const float SearchHorizontalPadding = 5f;
    private const float SearchVerticalPadding = 2f;
    private const float SettingsButtonWidth = 22f;
    private const int RecipePageSize = 2;
    private const float DetailPanelWidth = 310f;
    private const float DetailGap = 12f;
    private const float DetailHeaderHeight = 22f;
    private const float RecipeCardHeight = 132f;
    private const float RecipeIconSize = 32f;
    private const float RecipeIngredientSize = 26f;
    private const float DetailActionButtonSize = 22f;
    private const float EnterAnimationSeconds = 0.16f;
    private const float ExitAnimationSeconds = 0.12f;
    private const float WheelPageThreshold = 0.1f;
    private const float PointerBoundsPadding = 8f;
    private const float PanelEdgeInset = 18f;
    private const BindingFlags StaticLookup = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
    private const BindingFlags InstanceLookup = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Color TransparentBlockerColor = new(0f, 0f, 0f, 0f);
    private static readonly Color CellColor = new(0f, 0f, 0f, 0f);
    private static readonly Color CellSelectedColor = new(1f, 1f, 1f, 0.12f);
    private static readonly Color SearchColor = new(0f, 0f, 0f, 0.96f);
    private static readonly Color PixelBorderColor = new(0.72f, 0.76f, 0.78f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color MutedTextColor = new(0.70f, 0.72f, 0.74f, 1f);
    private static bool _ownsNativeTooltip;
    private static readonly Vector3[] PointerCornerBuffer = new Vector3[4];

    private readonly Canvas _canvas;
    private readonly int _uiLayer;
    private readonly TMP_FontAsset? _font;
    private readonly GameObject _root;
    private readonly RectTransform _panelRect;
    private readonly CanvasGroup _panelCanvasGroup;
    private readonly RectTransform _detailRect;
    private readonly GameObject _detailPanel;
    private readonly CanvasGroup _detailCanvasGroup;
    private readonly RectTransform _recipeContent;
    private readonly RectTransform _settingsRect;
    private readonly GameObject _settingsPanel;
    private readonly CanvasGroup _settingsCanvasGroup;
    private readonly RectTransform _settingsContent;
    private readonly GridLayoutGroup _grid;
    private readonly RectTransform _gridContent;
    private readonly TMP_InputField _searchInput;
    private TextMeshProUGUI _pageText = null!;
    private TextMeshProUGUI _categoryText = null!;
    private TextMeshProUGUI _detailModeText = null!;
    private TextMeshProUGUI _detailTitleText = null!;
    private TextMeshProUGUI _recipePageText = null!;
    private TextMeshProUGUI _settingsTitleText = null!;
    private TextMeshProUGUI _settingsButtonText = null!;
    private TextMeshProUGUI _scaleValueText = null!;
    private Slider _scaleSlider = null!;
    private readonly TextMeshProUGUI _selectionText;
    private LayoutElement _gridFrameLayout = null!;
    private const float PanelSpacing = 6f;
    private const float SelectionHeight = 16f;

    private int _catalogRevision = -1;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private string _lastPanelPosition = string.Empty;
    private float _lastPanelScale = -1f;
    private int _page;
    private int _lastPageCount = 1;
    private int _categoryIndex;
    private int _recipePage;
    private int _lastRecipePageCount = 1;
    private bool _dirty = true;
    private bool _detailDirty = true;
    private bool _loadingShown;
    private bool _settingsOpen;
    private bool _wantsVisible;
    private float _animationProgress;
    private int _lastAnimationFrame = -1;
    private int _lastPagingInputFrame = -1;
    private int _lastWheelSampleFrame = -1;
    private int _lastPointerCheckFrame = -1;
    private float _wheelAccumulator;
    private bool _lastPointerOverPanel;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private UeiRecipeRelation _detailMode = UeiRecipeRelation.ProducedBy;
    private UeiEntry? _selectedEntry;
    private float _pendingPanelScale;

    public UeiPanel(Canvas canvas)
    {
        _canvas = canvas;
        _uiLayer = LayerMask.NameToLayer("UI");
        _font = FindFont(canvas);
        EnsureEventSystem();

        if (_canvas.GetComponent<GraphicRaycaster>() == null)
        {
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        _root = NewUiObject("UEI.Root", _canvas.transform);
        _root.GetComponent<RectTransform>().SetStretch();

        GameObject panel = AddImage(_root.transform, "Panel", TransparentBlockerColor, raycast: true);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0f, 0.5f);
        _panelRect.anchorMax = new Vector2(0f, 0.5f);
        _panelRect.pivot = new Vector2(0f, 0.5f);
        _panelRect.anchoredPosition = PanelShownPosition() + PanelHiddenOffset();
        _panelRect.sizeDelta = new Vector2(310f, 680f);
        _panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        _panelCanvasGroup.alpha = 0f;
        _panelCanvasGroup.interactable = false;
        _panelCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(0, 0, 0, 0);
        panelLayout.spacing = PanelSpacing;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        BuildHeader(panel.transform);
        BuildCategoryBar(panel.transform);
        _gridContent = BuildGrid(panel.transform, out _grid);
        _selectionText = AddText(panel.transform, "Selection", string.Empty, 11f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement selectionLayout = _selectionText.GetOrAddLayoutElement();
        selectionLayout.preferredHeight = SelectionHeight;
        selectionLayout.minHeight = SelectionHeight;
        _searchInput = BuildBottomBar(panel.transform);

        _detailPanel = AddImage(_root.transform, "DetailPanel", TransparentBlockerColor, raycast: true);
        _detailRect = _detailPanel.GetComponent<RectTransform>();
        _detailRect.anchorMin = new Vector2(0f, 0.5f);
        _detailRect.anchorMax = new Vector2(0f, 0.5f);
        _detailRect.pivot = new Vector2(0f, 0.5f);
        _detailRect.sizeDelta = new Vector2(DetailPanelWidth, 420f);
        _detailCanvasGroup = _detailPanel.AddComponent<CanvasGroup>();
        _detailCanvasGroup.alpha = 0f;
        _detailCanvasGroup.interactable = false;
        _detailCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup detailLayout = _detailPanel.AddComponent<VerticalLayoutGroup>();
        detailLayout.padding = new RectOffset(0, 0, 0, 0);
        detailLayout.spacing = 6f;
        detailLayout.childAlignment = TextAnchor.UpperLeft;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;

        BuildDetailHeader(_detailPanel.transform);
        _recipeContent = NewUiObject("RecipeContent", _detailPanel.transform).GetComponent<RectTransform>();
        LayoutElement recipeLayout = _recipeContent.gameObject.AddComponent<LayoutElement>();
        recipeLayout.flexibleHeight = 1f;
        recipeLayout.minHeight = 250f;
        VerticalLayoutGroup recipeGroup = _recipeContent.gameObject.AddComponent<VerticalLayoutGroup>();
        recipeGroup.spacing = 6f;
        recipeGroup.childAlignment = TextAnchor.UpperLeft;
        recipeGroup.childControlWidth = true;
        recipeGroup.childControlHeight = true;
        recipeGroup.childForceExpandWidth = true;
        recipeGroup.childForceExpandHeight = false;
        _detailPanel.SetActive(false);

        _settingsPanel = AddImage(_root.transform, "SettingsPanel", TransparentBlockerColor, raycast: true);
        _settingsRect = _settingsPanel.GetComponent<RectTransform>();
        _settingsRect.anchorMin = new Vector2(0f, 0.5f);
        _settingsRect.anchorMax = new Vector2(0f, 0.5f);
        _settingsRect.pivot = new Vector2(0f, 0.5f);
        _settingsRect.sizeDelta = new Vector2(DetailPanelWidth, 260f);
        _settingsCanvasGroup = _settingsPanel.AddComponent<CanvasGroup>();
        _settingsCanvasGroup.alpha = 0f;
        _settingsCanvasGroup.interactable = false;
        _settingsCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup settingsLayout = _settingsPanel.AddComponent<VerticalLayoutGroup>();
        settingsLayout.padding = new RectOffset(0, 0, 0, 0);
        settingsLayout.spacing = 6f;
        settingsLayout.childAlignment = TextAnchor.UpperLeft;
        settingsLayout.childControlWidth = true;
        settingsLayout.childControlHeight = true;
        settingsLayout.childForceExpandWidth = true;
        settingsLayout.childForceExpandHeight = false;

        BuildSettingsHeader(_settingsPanel.transform);
        _settingsContent = NewUiObject("SettingsContent", _settingsPanel.transform).GetComponent<RectTransform>();
        LayoutElement settingsContentLayout = _settingsContent.gameObject.AddComponent<LayoutElement>();
        settingsContentLayout.flexibleHeight = 1f;
        VerticalLayoutGroup settingsContentGroup = _settingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        settingsContentGroup.spacing = 6f;
        settingsContentGroup.childAlignment = TextAnchor.UpperLeft;
        settingsContentGroup.childControlWidth = true;
        settingsContentGroup.childControlHeight = true;
        settingsContentGroup.childForceExpandWidth = true;
        settingsContentGroup.childForceExpandHeight = false;
        _settingsPanel.SetActive(false);

        Hide();
    }

    public bool IsTextInputFocused => _root.activeInHierarchy && _animationProgress > 0.98f && _searchInput.isFocused;

    public bool IsPointerOverPanel
    {
        get
        {
            try
            {
                int frame = Time.frameCount;
                if (_lastPointerCheckFrame == frame)
                {
                    return _lastPointerOverPanel;
                }

                _lastPointerCheckFrame = frame;
                _lastPointerOverPanel = false;

                if (!_root.activeInHierarchy || _animationProgress <= 0.02f)
                {
                    return false;
                }

                Camera? uiCamera = GetUiCamera();
                _lastPointerOverPanel = RectTransformUtility.RectangleContainsScreenPoint(_panelRect, Input.mousePosition, uiCamera)
                    || (_detailPanel.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_detailRect, Input.mousePosition, uiCamera))
                    || (_settingsPanel.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_settingsRect, Input.mousePosition, uiCamera))
                    || IsPointerInsidePanelScreenBounds(uiCamera)
                    || IsPointerOverPanelGraphic();
                return _lastPointerOverPanel;
            }
            catch
            {
                return false;
            }
        }
    }

    public void Show(PlayerCamera camera)
    {
        BeginShow();
        _loadingShown = false;
        UpdatePanelSize(camera);
        HandlePagingInput();

        if (_catalogRevision != UeiCatalog.Revision)
        {
            _catalogRevision = UeiCatalog.Revision;
            _dirty = true;
            _detailDirty = true;
        }

        if (_dirty)
        {
            RefreshGrid();
        }

        if (_detailDirty)
        {
            RefreshDetail();
        }

        TickAnimation();
    }

    public void ShowLoading(PlayerCamera camera)
    {
        BeginShow();
        UpdatePanelSize(camera);
        if (!_loadingShown)
        {
            ClearChildren(_gridContent);
            _pageText.text = "...";
            _categoryText.text = UeiI18n.T("category.all");
            _selectionText.text = UeiI18n.T("loading.items");
            _detailPanel.SetActive(false);
            _loadingShown = true;
        }
        TickAnimation();
    }

    public void Hide()
    {
        _wantsVisible = false;
        ClearNativeTooltip();

        if (!_root.activeSelf)
        {
            if (_animationProgress > 0f)
            {
                _animationProgress = 0f;
                ApplyAnimation();
            }
            return;
        }

        TickAnimation();
    }

    public void Destroy()
    {
        ClearNativeTooltip();
        if (_root != null)
        {
            UnityEngine.Object.Destroy(_root);
        }
    }

    public string DescribeVisualState()
    {
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        return string.Format(
            "rootActive={0} inHierarchy={1} sibling={2} panelPos=({3:0.##},{4:0.##}) panelSize=({5:0.##},{6:0.##}) alpha={7:0.##} anim={8:0.##} canvas={9} canvasScale=({10:0.##},{11:0.##},{12:0.##}) canvasSize=({13:0.##},{14:0.##}) page={15}/{16}",
            _root.activeSelf,
            _root.activeInHierarchy,
            _root.transform.GetSiblingIndex(),
            _panelRect.anchoredPosition.x,
            _panelRect.anchoredPosition.y,
            _panelRect.sizeDelta.x,
            _panelRect.sizeDelta.y,
            _panelCanvasGroup.alpha,
            _animationProgress,
            _canvas.name,
            _canvas.transform.localScale.x,
            _canvas.transform.localScale.y,
            _canvas.transform.localScale.z,
            canvasRect != null ? canvasRect.rect.width : 0f,
            canvasRect != null ? canvasRect.rect.height : 0f,
            _page + 1,
            _lastPageCount);
    }

    public bool ShowItemUses(Item item)
    {
        try
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
            {
                return false;
            }

            if (!UeiCatalog.TryGetEntry(UeiEntryKind.Item, item.id, out UeiEntry? entry) || entry == null)
            {
                return false;
            }

            if (_settingsOpen)
            {
                CloseSettings();
            }

            FocusGridEntry(entry);
            SelectEntry(entry, UeiRecipeRelation.UsedIn, linkOriginalRecipe: false);
            return true;
        }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning($"Failed to open UEI uses for inventory item: {ex.Message}");
            return false;
        }
    }

    private void BeginShow()
    {
        _wantsVisible = true;
        if (!_root.activeSelf)
        {
            _root.SetActive(true);
            _dirty = true;
            ApplyAnimation();
        }
        BringToFront();
    }

    private void BringToFront()
    {
        Transform parent = _root.transform.parent;
        if (parent != null && _root.transform.GetSiblingIndex() != parent.childCount - 1)
        {
            _root.transform.SetAsLastSibling();
        }
    }

    private void TickAnimation()
    {
        if (_lastAnimationFrame == Time.frameCount)
        {
            ApplyAnimation();
            return;
        }

        _lastAnimationFrame = Time.frameCount;
        float duration = _wantsVisible ? EnterAnimationSeconds : ExitAnimationSeconds;
        float direction = _wantsVisible ? 1f : -1f;
        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        float step = duration <= 0.001f ? 1f : deltaTime / duration;
        _animationProgress = Mathf.Clamp01(_animationProgress + direction * step);
        ApplyAnimation();

        if (!_wantsVisible && _animationProgress <= 0f && _root.activeSelf)
        {
            _root.SetActive(false);
        }
    }

    private void ApplyAnimation()
    {
        float eased = EaseOutCubic(_animationProgress);
        _panelCanvasGroup.alpha = eased;
        _panelCanvasGroup.interactable = _wantsVisible && _animationProgress > 0.98f;
        _panelCanvasGroup.blocksRaycasts = _animationProgress > 0.02f;
        _panelRect.anchoredPosition = PanelShownPosition() + PanelHiddenOffset() * (1f - eased);
        _detailCanvasGroup.alpha = eased;
        _detailCanvasGroup.interactable = _wantsVisible && _animationProgress > 0.98f;
        _detailCanvasGroup.blocksRaycasts = _animationProgress > 0.02f;
        _detailRect.anchoredPosition = DetailShownPosition() + PanelHiddenOffset() * (1f - eased);
        _settingsCanvasGroup.alpha = eased;
        _settingsCanvasGroup.interactable = _wantsVisible && _settingsOpen && _animationProgress > 0.98f;
        _settingsCanvasGroup.blocksRaycasts = _settingsOpen && _animationProgress > 0.02f;
        _settingsRect.anchoredPosition = DetailShownPosition() + PanelHiddenOffset() * (1f - eased);
    }

    private static float EaseOutCubic(float value)
    {
        float t = Mathf.Clamp01(value);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = NewUiObject("Header", parent);
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = HeaderHeight;
        layout.minHeight = HeaderHeight;

        HorizontalLayoutGroup group = header.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 4f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        Button previous = AddButton(header.transform, "PreviousPage", "<", PageTextSize, HeaderHeight, PageButtonWidth);
        LayoutElement prevLayout = previous.GetComponent<LayoutElement>();
        prevLayout.preferredWidth = PageButtonWidth;
        prevLayout.minWidth = PageButtonWidth;
        previous.onClick.AddListener(() => ChangePage(-1));

        _pageText = AddText(header.transform, "PageText", "1/1", PageTextSize, TextColor, TextAlignmentOptions.Center);
        LayoutElement pageLayout = _pageText.GetOrAddLayoutElement();
        pageLayout.preferredWidth = PageTextWidth;
        pageLayout.minWidth = PageTextWidth;
        pageLayout.preferredHeight = HeaderHeight;
        _pageText.enableWordWrapping = false;
        _pageText.overflowMode = TextOverflowModes.Overflow;

        Button next = AddButton(header.transform, "NextPage", ">", PageTextSize, HeaderHeight, PageButtonWidth);
        LayoutElement nextLayout = next.GetComponent<LayoutElement>();
        nextLayout.preferredWidth = PageButtonWidth;
        nextLayout.minWidth = PageButtonWidth;
        next.onClick.AddListener(() => ChangePage(1));
    }

    private void BuildCategoryBar(Transform parent)
    {
        GameObject bar = NewUiObject("CategoryBar", parent);
        LayoutElement layout = bar.AddComponent<LayoutElement>();
        layout.preferredHeight = CategoryBarHeight;
        layout.minHeight = CategoryBarHeight;

        HorizontalLayoutGroup group = bar.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 4f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        Button previous = AddButton(bar.transform, "PreviousCategory", "<", 10f, CategoryBarHeight, 22f);
        LayoutElement prevLayout = previous.GetComponent<LayoutElement>();
        prevLayout.preferredWidth = 22f;
        previous.onClick.AddListener(() => ChangeCategory(-1));

        _categoryText = AddText(bar.transform, "CategoryText", UeiI18n.T("category.all"), 10f, TextColor, TextAlignmentOptions.Center);
        LayoutElement textLayout = _categoryText.GetOrAddLayoutElement();
        textLayout.preferredWidth = CategoryButtonWidth;
        textLayout.minWidth = CategoryButtonWidth;
        textLayout.preferredHeight = CategoryBarHeight;
        _categoryText.enableWordWrapping = false;

        Button next = AddButton(bar.transform, "NextCategory", ">", 10f, CategoryBarHeight, 22f);
        LayoutElement nextLayout = next.GetComponent<LayoutElement>();
        nextLayout.preferredWidth = 22f;
        next.onClick.AddListener(() => ChangeCategory(1));
    }

    private void BuildDetailHeader(Transform parent)
    {
        GameObject header = NewUiObject("DetailHeader", parent);
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = DetailHeaderHeight;
        layout.minHeight = DetailHeaderHeight;

        HorizontalLayoutGroup group = header.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 4f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        Button mode = AddButton(header.transform, "Mode", UeiI18n.T("detail.mode.source"), 10f, DetailHeaderHeight, 36f);
        LayoutElement modeLayout = mode.GetComponent<LayoutElement>();
        modeLayout.preferredWidth = 36f;
        _detailModeText = mode.GetComponentInChildren<TextMeshProUGUI>();
        mode.onClick.AddListener(ToggleDetailMode);

        Button prev = AddButton(header.transform, "PreviousRecipe", "<", 10f, DetailHeaderHeight, 22f);
        LayoutElement prevLayout = prev.GetComponent<LayoutElement>();
        prevLayout.preferredWidth = 22f;
        prev.onClick.AddListener(() => ChangeRecipePage(-1));

        Button close = AddButton(header.transform, "CloseDetail", "×", 10f, DetailHeaderHeight, DetailActionButtonSize);
        LayoutElement closeLayout = close.GetComponent<LayoutElement>();
        closeLayout.preferredWidth = DetailActionButtonSize;
        closeLayout.minWidth = DetailActionButtonSize;
        close.onClick.AddListener(CloseDetail);
        BindTooltip(close.gameObject, UeiI18n.T("button.closeDetail"), UeiI18n.T("button.closeDetail.desc"));

        _detailTitleText = AddText(header.transform, "Title", string.Empty, 10f, TextColor, TextAlignmentOptions.Center);
        LayoutElement titleLayout = _detailTitleText.GetOrAddLayoutElement();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = DetailHeaderHeight;
        _detailTitleText.enableWordWrapping = false;

        _recipePageText = AddText(header.transform, "RecipePage", "0/0", 10f, MutedTextColor, TextAlignmentOptions.Center);
        LayoutElement pageLayout = _recipePageText.GetOrAddLayoutElement();
        pageLayout.preferredWidth = 42f;
        pageLayout.minWidth = 42f;
        pageLayout.preferredHeight = DetailHeaderHeight;
        _recipePageText.enableWordWrapping = false;

        Button next = AddButton(header.transform, "NextRecipe", ">", 10f, DetailHeaderHeight, 22f);
        LayoutElement nextLayout = next.GetComponent<LayoutElement>();
        nextLayout.preferredWidth = 22f;
        next.onClick.AddListener(() => ChangeRecipePage(1));
    }

    private void BuildSettingsHeader(Transform parent)
    {
        GameObject header = NewUiObject("SettingsHeader", parent);
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = DetailHeaderHeight;
        layout.minHeight = DetailHeaderHeight;

        HorizontalLayoutGroup group = header.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 4f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        _settingsTitleText = AddText(header.transform, "Title", UeiI18n.T("settings.title"), 10f, TextColor, TextAlignmentOptions.Center);
        LayoutElement titleLayout = _settingsTitleText.GetOrAddLayoutElement();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = DetailHeaderHeight;
        _settingsTitleText.enableWordWrapping = false;

        Button close = AddButton(header.transform, "CloseSettings", "×", 10f, DetailHeaderHeight, DetailActionButtonSize);
        LayoutElement closeLayout = close.GetComponent<LayoutElement>();
        closeLayout.preferredWidth = DetailActionButtonSize;
        closeLayout.minWidth = DetailActionButtonSize;
        close.onClick.AddListener(CloseSettings);
        BindTooltip(close.gameObject, UeiI18n.T("button.closeDetail"), UeiI18n.T("button.closeDetail.desc"));
    }

    private RectTransform BuildGrid(Transform parent, out GridLayoutGroup grid)
    {
        GameObject frame = AddImage(parent, "GridFrame", Color.clear, raycast: true);
        _gridFrameLayout = frame.AddComponent<LayoutElement>();
        _gridFrameLayout.flexibleHeight = 0f;
        _gridFrameLayout.preferredHeight = 430f;
        _gridFrameLayout.minHeight = 0f;

        GameObject content = NewUiObject("Grid", frame.transform);
        RectTransform rt = content.GetComponent<RectTransform>();
        rt.SetStretch();
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);

        grid = content.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.spacing = new Vector2(6f, 6f);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.cellSize = new Vector2(42f, 42f);
        return rt;
    }

    private TMP_InputField BuildBottomBar(Transform parent)
    {
        GameObject bar = NewUiObject("BottomBar", parent);
        LayoutElement barLayout = bar.AddComponent<LayoutElement>();
        barLayout.preferredHeight = SearchInputHeight;
        barLayout.minHeight = SearchInputHeight;

        HorizontalLayoutGroup group = bar.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 4f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        TMP_InputField input = AddInput(bar.transform, "SearchInput", UeiI18n.T("search.placeholder"));
        LayoutElement inputLayout = input.GetComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        inputLayout.minWidth = 60f;
        input.onValueChanged.AddListener(value =>
        {
            _searchText = value ?? string.Empty;
            _page = 0;
            _dirty = true;
            RefreshGrid();
        });

        Button settings = AddButton(bar.transform, "Settings", string.Empty, SearchTextSize, SearchInputHeight, SettingsButtonWidth);
        LayoutElement settingsLayout = settings.GetComponent<LayoutElement>();
        settingsLayout.preferredWidth = SettingsButtonWidth;
        settingsLayout.minWidth = SettingsButtonWidth;
        _settingsButtonText = settings.GetComponentInChildren<TextMeshProUGUI>();
        AddGearIcon(settings.transform);
        settings.onClick.AddListener(OpenSettings);
        BindTooltip(settings.gameObject, UeiI18n.T("button.settings"), UeiI18n.T("button.settings.desc"));

        return input;
    }

    private void HandlePagingInput()
    {
        Camera? uiCamera = GetUiCamera();
        bool overDetail = _detailPanel.activeInHierarchy
            && (RectTransformUtility.RectangleContainsScreenPoint(_detailRect, Input.mousePosition, uiCamera)
                || IsPointerInsideScreenBounds(_detailRect, uiCamera));
        bool overSettings = _settingsPanel.activeInHierarchy
            && (RectTransformUtility.RectangleContainsScreenPoint(_settingsRect, Input.mousePosition, uiCamera)
                || IsPointerInsideScreenBounds(_settingsRect, uiCamera));
        bool overList = RectTransformUtility.RectangleContainsScreenPoint(_panelRect, Input.mousePosition, uiCamera)
            || IsPointerInsideScreenBounds(_panelRect, uiCamera);

        if (overSettings)
        {
            return;
        }

        if (!overDetail && !overList)
        {
            return;
        }

        int frame = Time.frameCount;
        if (_lastPagingInputFrame == frame)
        {
            return;
        }

        int delta = ReadWheelPageDelta(frame);
        if (delta == 0)
        {
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                delta = -1;
            }
            else if (Input.GetKeyDown(KeyCode.PageDown))
            {
                delta = 1;
            }
        }

        if (delta != 0)
        {
            _lastPagingInputFrame = frame;
            if (overDetail)
            {
                ChangeRecipePage(delta);
            }
            else
            {
                ChangePage(delta);
            }
        }
    }

    private int ReadWheelPageDelta(int frame)
    {
        if (_lastWheelSampleFrame == frame)
        {
            return 0;
        }

        _lastWheelSampleFrame = frame;
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f)
        {
            return 0;
        }

        if (_wheelAccumulator != 0f && Mathf.Sign(_wheelAccumulator) != Mathf.Sign(wheel))
        {
            _wheelAccumulator = 0f;
        }

        _wheelAccumulator += wheel;
        if (_wheelAccumulator >= WheelPageThreshold)
        {
            _wheelAccumulator = 0f;
            return -1;
        }

        if (_wheelAccumulator <= -WheelPageThreshold)
        {
            _wheelAccumulator = 0f;
            return 1;
        }

        return 0;
    }

    private void ChangePage(int delta)
    {
        _page = Mathf.Clamp(_page + delta, 0, Math.Max(0, _lastPageCount - 1));
        _dirty = true;
        RefreshGrid();
    }

    private void ChangeCategory(int delta)
    {
        if (!UeiCatalog.EnsureBuilt() || UeiCatalog.CategoryKeys.Count == 0)
        {
            return;
        }

        int count = UeiCatalog.CategoryKeys.Count;
        _categoryIndex = (_categoryIndex + delta) % count;
        if (_categoryIndex < 0)
        {
            _categoryIndex += count;
        }

        _page = 0;
        _dirty = true;
        RefreshGrid();
    }

    private void ChangeRecipePage(int delta)
    {
        _recipePage = Mathf.Clamp(_recipePage + delta, 0, Math.Max(0, _lastRecipePageCount - 1));
        _detailDirty = true;
        RefreshDetail();
    }

    private void ToggleDetailMode()
    {
        if (_selectedEntry == null)
        {
            return;
        }

        _detailMode = _detailMode == UeiRecipeRelation.ProducedBy
            ? UeiRecipeRelation.UsedIn
            : UeiRecipeRelation.ProducedBy;
        _recipePage = 0;
        _detailDirty = true;
        RefreshDetail();
    }

    private void OpenSettings()
    {
        _settingsOpen = true;
        ClearNativeTooltip();
        _detailPanel.SetActive(false);
        _settingsPanel.SetActive(true);
        _pendingPanelScale = CurrentPanelScale();
        RefreshSettings();
    }

    private void CloseSettings()
    {
        _settingsOpen = false;
        ClearNativeTooltip();
        _settingsPanel.SetActive(false);
        _settingsCanvasGroup.blocksRaycasts = false;
        if (_selectedEntry != null)
        {
            _detailDirty = true;
            RefreshDetail();
        }
    }

    private void CloseDetail()
    {
        if (_settingsOpen)
        {
            CloseSettings();
        }

        _selectedEntry = null;
        _statusMessage = string.Empty;
        _recipePage = 0;
        ClearNativeTooltip();
        _detailDirty = true;
        _dirty = true;
        RefreshDetail();
        RefreshGrid();
    }

    private void RefreshSettings()
    {
        ClearChildren(_settingsContent);
        UpdateStaticLanguageText();
        _settingsTitleText.text = UeiI18n.T("settings.title");

        AddSettingRow(
            _settingsContent,
            UeiI18n.T("settings.language"),
            CurrentLanguageLabel(),
            CycleLanguageMode);

        AddSettingRow(
            _settingsContent,
            UeiI18n.T("settings.position"),
            CurrentPositionLabel(),
            CyclePanelPosition);

        AddScaleSettingRow(_settingsContent);

        bool hasCheatPermission = HasCheatPermission();
        if (!hasCheatPermission && CheatConfigured())
        {
            UeiPlugin.SetCheatEnabled(false);
        }

        bool cheatEnabled = CheatEnabled();
        AddSettingRow(
            _settingsContent,
            UeiI18n.T("settings.cheat"),
            hasCheatPermission
                ? (cheatEnabled ? UeiI18n.T("settings.on") : UeiI18n.T("settings.off"))
                : UeiI18n.T("settings.noCommandAccess"),
            ToggleCheatMode,
            cheatEnabled,
            hasCheatPermission);

        AddSettingText(
            _settingsContent,
            UeiI18n.T("settings.about") + "\n" + UeiI18n.Format(
                "settings.aboutText",
                UeiPlugin.PluginVersion,
                UeiPlugin.PluginGuid,
                UeiPlugin.PluginAuthor));
    }

    private void AddSettingRow(Transform parent, string label, string value, Action action, bool danger = false, bool enabled = true)
    {
        Color rowColor = danger ? new Color(0.55f, 0.04f, 0.02f, 0.72f) : new Color(0f, 0f, 0f, 0.42f);
        Color labelColor = enabled
            ? (danger ? new Color(1f, 0.34f, 0.28f, 1f) : TextColor)
            : MutedTextColor;
        GameObject row = AddImage(parent, "SettingRow", rowColor, raycast: true);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 30f;
        rowLayout.minHeight = 30f;
        AddPixelBorder(row.transform, PixelBorderColor, 1f);

        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(6, 4, 3, 3);
        group.spacing = 6f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        TextMeshProUGUI labelText = AddText(row.transform, "Label", label, 10f, labelColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement labelLayout = labelText.GetOrAddLayoutElement();
        labelLayout.flexibleWidth = 1f;
        labelLayout.preferredHeight = 22f;
        labelText.enableWordWrapping = false;

        Button button = AddButton(row.transform, "Value", value, 10f, 22f, 82f);
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null && (danger || !enabled))
        {
            buttonText.color = labelColor;
        }
        LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
        buttonLayout.preferredWidth = 92f;
        buttonLayout.minWidth = 82f;
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.55f);
        button.colors = colors;
        button.interactable = enabled;
        if (enabled)
        {
            button.onClick.AddListener(() => action());
        }
    }

    private void AddScaleSettingRow(Transform parent)
    {
        GameObject row = AddImage(parent, "SettingScaleRow", new Color(0f, 0f, 0f, 0.42f), raycast: true);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 30f;
        rowLayout.minHeight = 30f;
        AddPixelBorder(row.transform, PixelBorderColor, 1f);

        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(6, 4, 3, 3);
        group.spacing = 6f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        TextMeshProUGUI labelText = AddText(row.transform, "Label", UeiI18n.T("settings.scale"), 10f, TextColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement labelLayout = labelText.GetOrAddLayoutElement();
        labelLayout.preferredWidth = 48f;
        labelLayout.minWidth = 40f;
        labelLayout.preferredHeight = 22f;
        labelText.enableWordWrapping = false;

        _scaleSlider = AddSlider(row.transform, "ScaleSlider", 85f, 130f, Mathf.RoundToInt(_pendingPanelScale * 100f));
        LayoutElement sliderLayout = _scaleSlider.GetComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minWidth = 80f;
        sliderLayout.preferredHeight = 22f;
        _scaleSlider.onValueChanged.AddListener(value =>
        {
            _pendingPanelScale = Mathf.Round(value) / 100f;
            if (_scaleValueText != null)
            {
                _scaleValueText.text = CurrentScaleLabel(_pendingPanelScale);
            }
        });

        _scaleValueText = AddText(row.transform, "Value", CurrentScaleLabel(_pendingPanelScale), 10f, TextColor, TextAlignmentOptions.Center);
        LayoutElement valueLayout = _scaleValueText.GetOrAddLayoutElement();
        valueLayout.preferredWidth = 38f;
        valueLayout.minWidth = 38f;
        valueLayout.preferredHeight = 22f;
        _scaleValueText.enableWordWrapping = false;

        Button apply = AddButton(row.transform, "ApplyScale", UeiI18n.T("settings.apply"), 10f, 22f, 48f);
        LayoutElement applyLayout = apply.GetComponent<LayoutElement>();
        applyLayout.preferredWidth = 50f;
        applyLayout.minWidth = 48f;
        apply.onClick.AddListener(ApplyPendingPanelScale);
    }

    private void AddSettingText(Transform parent, string text)
    {
        TextMeshProUGUI label = AddText(parent, "SettingText", text, 10f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement layout = label.GetOrAddLayoutElement();
        layout.preferredHeight = 78f;
        layout.minHeight = 78f;
        label.enableWordWrapping = true;
    }

    private static string CurrentLanguageLabel()
    {
        string mode = SafeConfigString(() => UeiPlugin.LanguageMode.Value, "auto").Trim().ToLowerInvariant();
        if (mode == "zh" || mode == "cn" || mode == "chinese")
        {
            return UeiI18n.T("settings.language.zh");
        }

        if (mode == "en" || mode == "eng" || mode == "english")
        {
            return UeiI18n.T("settings.language.en");
        }

        return UeiI18n.T("settings.language.auto");
    }

    private static string CurrentPositionMode()
    {
        string mode = SafeConfigString(() => UeiPlugin.PanelPosition.Value, "left").Trim().ToLowerInvariant();
        return mode == "right" ? "right" : "left";
    }

    private static bool IsRightPosition()
    {
        return string.Equals(CurrentPositionMode(), "right", StringComparison.OrdinalIgnoreCase);
    }

    private static float CurrentPanelScale()
    {
        float value = 1f;
        try { value = UeiPlugin.PanelScale.Value; }
        catch { value = 1f; }
        return Mathf.Clamp(value, 0.85f, 1.3f);
    }

    private static bool CheatConfigured()
    {
        try { return UeiPlugin.CheatEnabled.Value; }
        catch { return false; }
    }

    private static bool CheatEnabled()
    {
        return CheatConfigured() && HasCheatPermission();
    }

    private static bool HasCheatPermission()
    {
        return HasVanillaCommandAccess() && HasKrokoshaCommandAccess();
    }

    private static bool HasVanillaCommandAccess()
    {
        try
        {
            ConsoleScript? console = ConsoleScript.instance;
            if (console == null || !console.enabled || console.gameObject == null || !console.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (console.consoleCanvas == null || console.consoleRect == null)
            {
                return false;
            }

            try
            {
                if (KeyBinds.GetBind("console") == KeyCode.None)
                {
                    return false;
                }
            }
            catch
            {
            }

            return ConsoleScript.SearchExact("spawn") != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasKrokoshaCommandAccess()
    {
        Type? mpType = FindLoadedType("KrokoshaCasualtiesMP.KrokoshaScavMultiplayer");
        Type? conType = FindLoadedType("KrokoshaCasualtiesMP.Con");
        if (mpType == null || conType == null)
        {
            return true;
        }

        bool? networkRunning = TryReadStaticBool(mpType, "network_system_is_running");
        if (networkRunning != true)
        {
            return true;
        }

        bool? canCheat = TryInvokeStaticBool(conType, "CanCheat");
        bool? canExecuteCommands = TryInvokeStaticBool(conType, "CanExecuteAdminCommands");
        if (canCheat.HasValue && canExecuteCommands.HasValue)
        {
            return canCheat.Value && canExecuteCommands.Value;
        }

        if (canCheat == false || canExecuteCommands == false)
        {
            return false;
        }

        bool? isServer = TryReadStaticBool(mpType, "is_server");
        if (isServer == true)
        {
            return TryReadKrokoshaRuleBool(mpType, "sv_cheats") == true;
        }

        bool? clientIsAdmin = TryReadStaticBool(conType, "client_isadmin");
        bool? clientCheatsAllowed = TryReadKrokoshaRuleBool(mpType, "AllowClientCheatCommands");
        if (clientIsAdmin.HasValue || clientCheatsAllowed.HasValue)
        {
            return clientIsAdmin == true && clientCheatsAllowed == true;
        }

        return false;
    }

    private static bool? TryReadKrokoshaRuleBool(Type mpType, string name)
    {
        object? rules = TryReadStaticMember(mpType, "rules");
        return TryReadInstanceBool(rules, name);
    }

    private static Type? FindLoadedType(string fullName)
    {
        try
        {
            Type? direct = Type.GetType(fullName, throwOnError: false);
            if (direct != null)
            {
                return direct;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool? TryInvokeStaticBool(Type type, string methodName)
    {
        try
        {
            MethodInfo? method = type.GetMethod(methodName, StaticLookup, null, Type.EmptyTypes, null);
            if (method?.Invoke(null, null) is bool value)
            {
                return value;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool? TryReadStaticBool(Type type, string name)
    {
        object? value = TryReadStaticMember(type, name);
        return value is bool flag ? flag : null;
    }

    private static object? TryReadStaticMember(Type type, string name)
    {
        try
        {
            PropertyInfo? property = type.GetProperty(name, StaticLookup);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            FieldInfo? field = type.GetField(name, StaticLookup);
            if (field != null)
            {
                return field.GetValue(null);
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool? TryReadInstanceBool(object? instance, string name)
    {
        if (instance == null)
        {
            return null;
        }

        try
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, InstanceLookup);
            if (property?.GetValue(instance, null) is bool propertyValue)
            {
                return propertyValue;
            }

            FieldInfo? field = type.GetField(name, InstanceLookup);
            if (field?.GetValue(instance) is bool fieldValue)
            {
                return fieldValue;
            }
        }
        catch
        {
        }

        return null;
    }

    private void CycleLanguageMode()
    {
        string mode = SafeConfigString(() => UeiPlugin.LanguageMode.Value, "auto").Trim().ToLowerInvariant();
        string next = mode switch
        {
            "auto" => "zh",
            "zh" => "en",
            "cn" => "en",
            "chinese" => "en",
            "en" => "auto",
            "eng" => "auto",
            "english" => "auto",
            _ => "auto",
        };

        UeiPlugin.SetLanguageMode(next);
        UeiCatalog.ForceRebuild();
        UpdateStaticLanguageText();
        _catalogRevision = -1;
        _dirty = true;
        _detailDirty = true;
        RefreshGrid();
        RefreshSettings();
    }

    private void UpdateStaticLanguageText()
    {
        if (_settingsButtonText != null)
        {
            _settingsButtonText.text = string.Empty;
        }

        if (_searchInput != null && _searchInput.placeholder is TMP_Text placeholder)
        {
            placeholder.text = UeiI18n.T("search.placeholder");
        }
    }

    private static string CurrentPositionLabel()
    {
        return IsRightPosition()
            ? UeiI18n.T("settings.position.right")
            : UeiI18n.T("settings.position.left");
    }

    private void CyclePanelPosition()
    {
        UeiPlugin.SetPanelPosition(IsRightPosition() ? "left" : "right");
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        RefreshSettings();
    }

    private static string CurrentScaleLabel()
    {
        return Mathf.RoundToInt(CurrentPanelScale() * 100f) + "%";
    }

    private static string CurrentScaleLabel(float scale)
    {
        return Mathf.RoundToInt(scale * 100f) + "%";
    }

    private void CyclePanelScale()
    {
        float current = CurrentPanelScale();
        float next = current < 0.93f ? 1.0f
            : current < 1.08f ? 1.15f
            : current < 1.23f ? 1.3f
            : 0.85f;
        UeiPlugin.SetPanelScale(next);
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        RefreshSettings();
    }

    private void ApplyPendingPanelScale()
    {
        UeiPlugin.SetPanelScale(_pendingPanelScale);
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        RefreshSettings();
    }

    private void ToggleCheatMode()
    {
        if (!HasCheatPermission())
        {
            UeiPlugin.SetCheatEnabled(false);
            RefreshSettings();
            return;
        }

        UeiPlugin.SetCheatEnabled(!CheatConfigured());
        RefreshSettings();
    }

    private void RefreshGrid()
    {
        if (!UeiCatalog.EnsureBuilt())
        {
            return;
        }

        _dirty = false;
        ClearChildren(_gridContent);

        List<UeiEntry> filtered = FilterEntries().ToList();
        _lastPageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
        _page = Mathf.Clamp(_page, 0, _lastPageCount - 1);
        _pageText.text = filtered.Count == 0 ? "0/0" : $"{_page + 1}/{_lastPageCount}";

        List<UeiEntry> pageItems = filtered
            .Skip(_page * PageSize)
            .Take(PageSize)
            .ToList();

        foreach (UeiEntry entry in pageItems)
        {
            AddEntryCell(entry);
        }

        for (int i = pageItems.Count; i < PageSize; i++)
        {
            AddEmptyCell();
        }

        if (_selectedEntry != null && filtered.All(x => x.EntryKey != _selectedEntry.EntryKey))
        {
            _selectedEntry = null;
            _detailDirty = true;
        }

        UpdateSelectionText(filtered.Count);
    }

    private IEnumerable<UeiEntry> FilterEntries()
    {
        IEnumerable<UeiEntry> entries = UeiCatalog.Entries;
        string category = CurrentCategoryKey();

        if (category == UeiCatalog.CategoryFavorites)
        {
            entries = entries.Where(x => x.IsFavorite);
        }
        else if (category == UeiCatalog.CategoryLiquid)
        {
            entries = entries.Where(x => x.Kind == UeiEntryKind.Liquid);
        }
        else if (category != UeiCatalog.CategoryAll)
        {
            entries = entries.Where(x => x.Kind == UeiEntryKind.Item
                && string.Equals(x.CategoryKey, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string search = _searchText.Trim();
            entries = entries.Where(x => x.SearchText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return entries
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.CategoryLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    private void AddEntryCell(UeiEntry entry)
    {
        bool selected = _selectedEntry != null && _selectedEntry.EntryKey == entry.EntryKey;
        GameObject cell = AddImage(_gridContent, "Cell." + entry.EntryKey, selected ? CellSelectedColor : CellColor, raycast: true);
        Button button = cell.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.fadeDuration = 0.04f;
        button.colors = colors;
        UeiEntryClickHandler click = cell.AddComponent<UeiEntryClickHandler>();
        click.Init(
            () => HandleEntryLeftClick(entry),
            () => SelectEntry(entry, UeiRecipeRelation.UsedIn, linkOriginalRecipe: false));

        BindTooltip(cell, entry);

        Image icon = AddIcon(cell.transform, "Icon", entry.Icon, entry.IconColor, 38f);
        icon.raycastTarget = false;

    }

    private void AddEmptyCell()
    {
        GameObject cell = AddImage(_gridContent, "Empty", new Color(0f, 0f, 0f, 0f), raycast: false);
        cell.GetComponent<Image>().enabled = false;
    }

    private void HandleEntryLeftClick(UeiEntry entry)
    {
        if (!CheatEnabled())
        {
            SelectEntry(entry, UeiRecipeRelation.ProducedBy, linkOriginalRecipe: true);
            return;
        }

        string status = TryCheatGiveItem(entry)
            ? UeiI18n.T("status.cheatSpawned")
            : (IsLegalCheatItem(entry) ? UeiI18n.T("status.cheatFailed") : UeiI18n.T("status.cheatIllegal"));
        SelectEntry(entry, UeiRecipeRelation.ProducedBy, linkOriginalRecipe: false);
        _statusMessage = status;
        RefreshGrid();
        RefreshDetail();
    }

    private void SelectEntry(UeiEntry entry, UeiRecipeRelation mode, bool linkOriginalRecipe)
    {
        _selectedEntry = entry;
        _detailMode = mode;
        _recipePage = 0;
        _detailDirty = true;
        if (linkOriginalRecipe)
        {
            TryLinkOriginalRecipes(entry);
        }
        else
        {
            _statusMessage = mode == UeiRecipeRelation.UsedIn ? UeiI18n.T("status.usedIn") : UeiI18n.T("status.source");
        }

        _dirty = true;
        RefreshGrid();
        RefreshDetail();
    }

    private void FocusGridEntry(UeiEntry entry)
    {
        List<UeiEntry> filtered = FilterEntries().ToList();
        int index = filtered.FindIndex(x => string.Equals(x.EntryKey, entry.EntryKey, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            _categoryIndex = 0;
            _searchText = string.Empty;
            _searchInput.SetTextWithoutNotify(string.Empty);
            filtered = FilterEntries().ToList();
            index = filtered.FindIndex(x => string.Equals(x.EntryKey, entry.EntryKey, StringComparison.OrdinalIgnoreCase));
        }

        if (index >= 0)
        {
            _page = index / PageSize;
        }
    }

    private static bool IsLegalCheatItem(UeiEntry entry)
    {
        if (entry.Kind != UeiEntryKind.Item || string.IsNullOrWhiteSpace(entry.Id))
        {
            return false;
        }

        if (string.Equals(entry.CategoryKey, "unobtainable", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            string id = RshLibHelper.GetBaseId(entry.Id);
            if (Item.GlobalItems != null && Item.GlobalItems.ContainsKey(id))
            {
                return true;
            }

            if (IsCustomModItem(id))
            {
                return true;
            }

            GameObject prefab = Resources.Load<GameObject>(id);
            return prefab != null && prefab.GetComponent<Item>() != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCustomModItem(string itemId)
    {
        return RshLibHelper.IsCustomItem(itemId)
            || CuCoreLibHelper.IsCustomItem(itemId)
            || SexModHelper.IsRegistered(itemId);
    }

    private static bool TryCheatGiveItem(UeiEntry entry)
    {
        if (!HasCheatPermission() || !IsLegalCheatItem(entry))
        {
            return false;
        }

        try
        {
            Body? body = PlayerCamera.main?.body;
            if (body == null)
            {
                return false;
            }

            Vector3 pos = body.transform.position + new Vector3(0f, 0.4f, 0f);
            pos.z = 0f;

            GameObject? inst = Utils.Create(entry.Id, pos, 0f);
            if (inst == null)
            {
                inst = SexModHelper.CreateInstance(entry.Id, pos, 0f);
            }
            if (inst == null)
            {
                return false;
            }

            Item? item = inst.GetComponent<Item>();
            if (item == null)
            {
                UnityEngine.Object.Destroy(inst);
                return false;
            }

            try { item.condition = 1f; } catch { }
            try
            {
                AmmoScript? ammo = inst.GetComponent<AmmoScript>();
                if (ammo != null && ammo.itemType == AmmoScript.AmmoItemType.Magazine)
                {
                    ammo.rounds = ammo.maxRounds;
                }
            }
            catch { }

            body.AutoPickUpItem(item);
            return true;
        }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning($"UEI cheat give failed for {entry.Id}: {ex.Message}");
            return false;
        }
    }

    private void TryLinkOriginalRecipes(UeiEntry entry)
    {
        if (entry.Kind != UeiEntryKind.Item)
        {
            _statusMessage = _detailMode == UeiRecipeRelation.ProducedBy ? UeiI18n.T("status.liquidSource") : UeiI18n.T("status.liquidUse");
            return;
        }

        Item? owned = FindOwnedItem(entry.Id);
        if (owned == null)
        {
            _statusMessage = UeiI18n.T("status.detailOnly");
            return;
        }

        try
        {
            PlayerCamera.main.SeeRecipesWithItem(owned);
            _statusMessage = UeiI18n.T("status.recipes");
        }
        catch (Exception ex)
        {
            _statusMessage = UeiI18n.T("status.recipeLinkFailed");
            UeiPlugin.LogWarning($"Failed to call SeeRecipesWithItem for {entry.Id}: {ex.Message}");
        }
    }

    private static bool JumpToOriginalRecipe(Recipe recipe)
    {
        PlayerCamera camera = PlayerCamera.main;
        if (camera == null || recipe == null)
        {
            return false;
        }

        try
        {
            if (camera.craftingPanel == null || !camera.craftingPanel.activeSelf)
            {
                camera.OpenCraftScreen();
            }

            if (camera.craftingPanel == null || !camera.craftingPanel.activeSelf)
            {
                return false;
            }

            try { camera.SetRecipeCategory(0); }
            catch (Exception ex) { UeiPlugin.LogWarning($"Failed to clear original recipe category: {ex.Message}"); }

            try { camera.ClearRecipeFilter(); }
            catch (Exception ex)
            {
                UeiPlugin.LogWarning($"Failed to clear original recipe filter: {ex.Message}");
                try { camera.RefreshRecipeList(); }
                catch { }
            }

            int index = OriginalRecipeIndex(recipe);
            if (index < 0)
            {
                return false;
            }

            camera.SelectRecipe(index);
            return true;
        }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning($"Failed to select original recipe {SafeRecipeName(recipe)}: {ex.Message}");
            return false;
        }
    }

    private static int OriginalRecipeIndex(Recipe recipe)
    {
        try
        {
            if (Recipes.recipes == null)
            {
                return recipe.index;
            }

            if (recipe.index >= 0
                && recipe.index < Recipes.recipes.Count
                && ReferenceEquals(Recipes.recipes[recipe.index], recipe))
            {
                return recipe.index;
            }

            int found = Recipes.recipes.IndexOf(recipe);
            return found >= 0 ? found : recipe.index;
        }
        catch
        {
            return recipe.index;
        }
    }

    private static bool IsOriginalRecipePinned(Recipe recipe)
    {
        try
        {
            PlayerCamera camera = PlayerCamera.main;
            if (camera == null || !camera.pinnedRecipe.HasValue)
            {
                return false;
            }

            return camera.pinnedRecipe.Value == OriginalRecipeIndex(recipe);
        }
        catch
        {
            return false;
        }
    }

    private void RefreshDetail()
    {
        _detailDirty = false;
        ClearChildren(_recipeContent);

        if (_settingsOpen)
        {
            _detailPanel.SetActive(false);
            return;
        }

        if (_selectedEntry == null)
        {
            _detailPanel.SetActive(false);
            return;
        }

        _detailPanel.SetActive(true);
        List<UeiRecipeLink> links = CurrentRecipeLinks();
        _lastRecipePageCount = Math.Max(1, (links.Count + RecipePageSize - 1) / RecipePageSize);
        _recipePage = Mathf.Clamp(_recipePage, 0, _lastRecipePageCount - 1);
        _detailModeText.text = _detailMode == UeiRecipeRelation.ProducedBy
            ? UeiI18n.T("detail.mode.source")
            : UeiI18n.T("detail.mode.uses");
        _detailTitleText.text = DetailModeLabel(_detailMode) + ": " + _selectedEntry.DisplayName;
        _recipePageText.text = links.Count == 0 ? "0/0" : $"{_recipePage + 1}/{_lastRecipePageCount}";

        Recipe? currentRecipe = links
            .Skip(_recipePage * RecipePageSize)
            .Select(link => link.Recipe)
            .FirstOrDefault(r => r != null);
        AddEntrySummary(_recipeContent, _selectedEntry, currentRecipe);

        if (links.Count == 0)
        {
            TextMeshProUGUI empty = AddText(_recipeContent, "Empty", UeiI18n.T("detail.empty"), 11f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
            LayoutElement emptyLayout = empty.GetOrAddLayoutElement();
            emptyLayout.preferredHeight = 28f;
            return;
        }

        foreach (UeiRecipeLink link in links.Skip(_recipePage * RecipePageSize).Take(RecipePageSize))
        {
            Recipe? recipe = link.Recipe;
            if (recipe != null)
            {
                AddRecipeCard(_recipeContent, link, recipe);
            }
        }
    }

    private void AddEntrySummary(Transform parent, UeiEntry entry, Recipe? recipe)
    {
        GameObject row = AddImage(parent, "EntrySummary", new Color(0f, 0f, 0f, 0.40f), raycast: true);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
        layout.minHeight = 44f;
        AddPixelBorder(row.transform, PixelBorderColor, 1f);

        Image icon = AddIcon(row.transform, "Icon", entry.Icon, entry.IconColor, 34f);
        RectTransform iconRt = icon.rectTransform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(5f, 0f);

        TextMeshProUGUI text = AddText(row.transform, "Text", BuildEntrySummaryText(entry), 10f, TextColor, TextAlignmentOptions.MidlineLeft);
        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(44f, 3f);
        textRt.offsetMax = new Vector2(recipe != null ? -58f : -32f, -3f);
        text.enableWordWrapping = false;

        Button favorite = AddButton(row.transform, "FavoriteEntry", entry.IsFavorite ? "*" : "+", 10f, DetailActionButtonSize, DetailActionButtonSize);
        RectTransform favoriteRt = favorite.GetComponent<RectTransform>();
        favoriteRt.anchorMin = new Vector2(1f, 0.5f);
        favoriteRt.anchorMax = new Vector2(1f, 0.5f);
        favoriteRt.pivot = new Vector2(1f, 0.5f);
        favoriteRt.anchoredPosition = new Vector2(recipe != null ? -31f : -5f, 0f);
        favoriteRt.sizeDelta = new Vector2(DetailActionButtonSize, DetailActionButtonSize);
        LayoutElement favoriteLayout = favorite.GetComponent<LayoutElement>();
        favoriteLayout.ignoreLayout = true;
        favoriteLayout.preferredWidth = DetailActionButtonSize;
        favoriteLayout.minWidth = DetailActionButtonSize;
        favorite.onClick.AddListener(() =>
        {
            UeiFavorites.Toggle(entry.EntryKey);
            _dirty = true;
            _detailDirty = true;
            RefreshGrid();
            RefreshDetail();
        });

        if (recipe == null)
        {
            return;
        }

        Button jump = AddButton(row.transform, "JumpOriginalRecipe", ">", 10f, DetailActionButtonSize, DetailActionButtonSize);
        RectTransform jumpRt = jump.GetComponent<RectTransform>();
        jumpRt.anchorMin = new Vector2(1f, 0.5f);
        jumpRt.anchorMax = new Vector2(1f, 0.5f);
        jumpRt.pivot = new Vector2(1f, 0.5f);
        jumpRt.anchoredPosition = new Vector2(-5f, 0f);
        jumpRt.sizeDelta = new Vector2(DetailActionButtonSize, DetailActionButtonSize);
        LayoutElement jumpLayout = jump.GetComponent<LayoutElement>();
        jumpLayout.ignoreLayout = true;
        jumpLayout.preferredWidth = DetailActionButtonSize;
        jumpLayout.minWidth = DetailActionButtonSize;
        jump.onClick.AddListener(() => JumpToDisplayedRecipe(recipe));
        BindTooltip(jump.gameObject, UeiI18n.T("button.jumpRecipe"), UeiI18n.T("button.jumpRecipe.desc"));
    }

    private void JumpToDisplayedRecipe(Recipe recipe)
    {
        try
        {
            _statusMessage = JumpToOriginalRecipe(recipe)
                ? UeiI18n.T("status.jumpRecipe")
                : UeiI18n.T("status.jumpFailed");
        }
        catch (Exception ex)
        {
            _statusMessage = UeiI18n.T("status.jumpFailed");
            UeiPlugin.LogWarning($"Failed to jump to original recipe for {SafeRecipeName(recipe)}: {ex.Message}");
        }
    }

    private void PinDisplayedRecipe(Recipe recipe)
    {
        try
        {
            PlayerCamera camera = PlayerCamera.main;
            if (camera == null)
            {
                _statusMessage = UeiI18n.T("status.pinFailed");
                return;
            }

            int index = OriginalRecipeIndex(recipe);
            if (index < 0)
            {
                _statusMessage = UeiI18n.T("status.pinFailed");
                return;
            }

            camera.selectedRecipe = index;
            camera.PinRecipe();
            bool pinned = camera.pinnedRecipe.HasValue && camera.pinnedRecipe.Value == index;
            _statusMessage = pinned ? UeiI18n.T("status.pinRecipe") : UeiI18n.T("status.unpinRecipe");
        }
        catch (Exception ex)
        {
            _statusMessage = UeiI18n.T("status.pinFailed");
            UeiPlugin.LogWarning($"Failed to pin original recipe for {SafeRecipeName(recipe)}: {ex.Message}");
        }
        finally
        {
            _detailDirty = true;
        }
    }

    private void AddRecipeCard(Transform parent, UeiRecipeLink link, Recipe recipe)
    {
        GameObject card = AddImage(parent, "Recipe." + link.RecipeIndex, new Color(0f, 0f, 0f, 0.52f), raycast: true);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredHeight = RecipeCardHeight;
        layout.minHeight = RecipeCardHeight;
        AddPixelBorder(card.transform, PixelBorderColor, 1f);

        string state = (link.Visible ? UeiI18n.T("recipe.visible") : UeiI18n.T("recipe.locked")) + " / " + (link.HasMadeBefore ? UeiI18n.T("recipe.made") : UeiI18n.T("recipe.new"));
        TextMeshProUGUI title = AddText(card.transform, "Title", SafeRecipeName(recipe) + "\n" + RecipeCategoryLabel(recipe) + "  " + UeiI18n.T("recipe.int") + " " + recipe.INT + "  " + state, 10f, TextColor, TextAlignmentOptions.TopLeft);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.offsetMin = new Vector2(6f, -36f);
        titleRt.offsetMax = new Vector2(-6f, -4f);
        title.enableWordWrapping = false;

        AddRecipePinButton(card.transform, recipe);
        AddRecipeResult(card.transform, recipe);
        AddRecipeIngredients(card.transform, recipe);
    }

    private void AddRecipePinButton(Transform parent, Recipe recipe)
    {
        Button pin = AddButton(parent, "PinOriginalRecipe", "*", 10f, DetailActionButtonSize, DetailActionButtonSize);
        RectTransform pinRt = pin.GetComponent<RectTransform>();
        pinRt.anchorMin = new Vector2(1f, 1f);
        pinRt.anchorMax = new Vector2(1f, 1f);
        pinRt.pivot = new Vector2(1f, 1f);
        pinRt.anchoredPosition = new Vector2(-8f, -42f);
        pinRt.sizeDelta = new Vector2(DetailActionButtonSize, DetailActionButtonSize);
        LayoutElement pinLayout = pin.GetComponent<LayoutElement>();
        pinLayout.ignoreLayout = true;
        pinLayout.preferredWidth = DetailActionButtonSize;
        pinLayout.minWidth = DetailActionButtonSize;
        TextMeshProUGUI pinText = pin.GetComponentInChildren<TextMeshProUGUI>();
        if (pinText != null && IsOriginalRecipePinned(recipe))
        {
            pinText.color = new Color(0.45f, 1f, 0.45f, 1f);
        }

        pin.onClick.AddListener(() => PinDisplayedRecipe(recipe));
        BindTooltip(pin.gameObject, UeiI18n.T("button.pinRecipe"), UeiI18n.T("button.pinRecipe.desc"));
    }

    private void AddRecipeResult(Transform parent, Recipe recipe)
    {
        UeiEntry? resultEntry = TryGetResultEntry(recipe);
        Sprite? sprite = resultEntry?.Icon;
        Color color = resultEntry?.IconColor ?? Color.white;
        try
        {
            (Sprite, Color) resultSprite = recipe.resultSprite;
            if (resultSprite.Item1 != null)
            {
                sprite = resultSprite.Item1;
                color = resultSprite.Item2;
            }
        }
        catch
        {
        }

        GameObject result = AddImage(parent, "Result", new Color(1f, 1f, 1f, 0.08f), raycast: true);
        RectTransform rt = result.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-8f, 8f);
        rt.sizeDelta = new Vector2(RecipeIconSize + 8f, RecipeIconSize + 8f);
        AddPixelBorder(result.transform, PixelBorderColor, 1f);

        Image icon = AddIcon(result.transform, "Icon", sprite, color, RecipeIconSize);
        icon.raycastTarget = false;

        if (resultEntry != null)
        {
            UeiEntry target = resultEntry;
            result.AddComponent<Button>().onClick.AddListener(() => SelectEntry(target, UeiRecipeRelation.ProducedBy, linkOriginalRecipe: false));
            BindTooltip(result, target);
        }
    }

    private void AddRecipeIngredients(Transform parent, Recipe recipe)
    {
        GameObject holder = NewUiObject("Ingredients", parent);
        RectTransform holderRt = holder.GetComponent<RectTransform>();
        holderRt.anchorMin = new Vector2(0f, 0f);
        holderRt.anchorMax = new Vector2(1f, 0f);
        holderRt.pivot = new Vector2(0f, 0f);
        holderRt.offsetMin = new Vector2(6f, 8f);
        holderRt.offsetMax = new Vector2(-60f, 72f);

        GridLayoutGroup group = holder.AddComponent<GridLayoutGroup>();
        group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        group.constraintCount = 6;
        group.cellSize = new Vector2(RecipeIngredientSize, RecipeIngredientSize);
        group.spacing = new Vector2(4f, 4f);

        if (recipe.items == null || recipe.items.Count == 0)
        {
            TextMeshProUGUI empty = AddText(holder.transform, "NoIngredients", UeiI18n.T("detail.noIngredients"), 9f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
            empty.rectTransform.SetStretch();
            return;
        }

        foreach (RecipeItem item in recipe.items.Take(12))
        {
            AddIngredientCell(holder.transform, item);
        }
    }

    private void AddIngredientCell(Transform parent, RecipeItem item)
    {
        UeiEntry? target = TryGetSpecificIngredientEntry(item);
        GameObject cell = AddImage(parent, "Ingredient", new Color(1f, 1f, 1f, target != null ? 0.08f : 0.03f), raycast: true);
        AddPixelBorder(cell.transform, target != null ? PixelBorderColor : new Color(0.36f, 0.38f, 0.40f, 1f), 1f);

        if (target != null)
        {
            Image icon = AddIcon(cell.transform, "Icon", target.Icon, target.IconColor, RecipeIngredientSize - 4f);
            icon.raycastTarget = false;
            UeiEntry entry = target;
            cell.AddComponent<Button>().onClick.AddListener(() => SelectEntry(entry, UeiRecipeRelation.ProducedBy, linkOriginalRecipe: false));
            BindTooltip(cell, entry);
            return;
        }

        TextMeshProUGUI text = AddText(cell.transform, "Quality", IngredientLabel(item), 7f, MutedTextColor, TextAlignmentOptions.Center);
        text.rectTransform.SetStretch();
        text.enableWordWrapping = false;
        BindTooltip(cell, GenericIngredientTitle(item), GenericIngredientTooltip(item));
    }

    private List<UeiRecipeLink> CurrentRecipeLinks()
    {
        if (_selectedEntry == null)
        {
            return new List<UeiRecipeLink>();
        }

        return _detailMode == UeiRecipeRelation.ProducedBy
            ? UeiCatalog.GetProducedBy(_selectedEntry)
            : UeiCatalog.GetUsedIn(_selectedEntry);
    }

    private string CurrentCategoryKey()
    {
        if (UeiCatalog.CategoryKeys.Count == 0)
        {
            return UeiCatalog.CategoryAll;
        }

        _categoryIndex = Mathf.Clamp(_categoryIndex, 0, UeiCatalog.CategoryKeys.Count - 1);
        string key = UeiCatalog.CategoryKeys[_categoryIndex];
        _categoryText.text = UeiCatalog.CategoryLabel(key);
        return key;
    }

    private static string DetailModeLabel(UeiRecipeRelation relation)
    {
        return relation == UeiRecipeRelation.ProducedBy ? UeiI18n.T("detail.source") : UeiI18n.T("detail.uses");
    }

    private static string BuildEntrySummaryText(UeiEntry entry)
    {
        if (entry.Kind == UeiEntryKind.Liquid)
        {
            LiquidType? liquid = entry.LiquidInfo;
            string value = liquid != null ? liquid.valuePerLiter.ToString("0.##") + "/L" : "?/L";
            return entry.DisplayName + "\n" + entry.Id + "  " + value;
        }

        ItemInfo? info = entry.ItemInfo;
        if (info == null)
        {
            return entry.DisplayName + "\n" + entry.Id;
        }

        return entry.DisplayName + "\n" + entry.Id + "  " + info.weight.ToString("0.##") + "u  $" + info.value.ToString("0.##");
    }

    private static string SafeRecipeName(Recipe recipe)
    {
        try
        {
            string simpleName = SafeRecipeString(() => recipe.simpleName);
            string localized = LocalizeRecipeName(simpleName);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            string fullName = SafeRecipeString(() => recipe.fullName);
            if (!string.IsNullOrWhiteSpace(fullName) && ContainsCjk(fullName))
            {
                return fullName;
            }

            UeiEntry? resultEntry = TryGetResultEntry(recipe);
            if (resultEntry != null && !string.IsNullOrWhiteSpace(resultEntry.DisplayName))
            {
                return resultEntry.DisplayName;
            }

            localized = LocalizeRecipeName(fullName);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            if (!string.IsNullOrWhiteSpace(simpleName))
            {
                return simpleName;
            }
        }
        catch
        {
        }

        return UeiI18n.T("recipe.fallback") + " " + recipe.index;
    }

    private static string LocalizeRecipeName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        string trimmed = key.Trim();
        string localized = UeiI18n.OtherText(trimmed, string.Empty);
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        localized = UeiI18n.ItemText(trimmed, string.Empty);
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        string compact = trimmed.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        if (!string.Equals(compact, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            localized = UeiI18n.OtherText(compact, string.Empty);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            localized = UeiI18n.ItemText(compact, string.Empty);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }
        }

        return string.Empty;
    }

    private static string SafeRecipeString(Func<string> getter)
    {
        try
        {
            return getter() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SafeConfigString(Func<string> getter, string fallback)
    {
        try
        {
            string value = getter();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool ContainsCjk(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if ((c >= '\u4e00' && c <= '\u9fff')
                || (c >= '\u3400' && c <= '\u4dbf')
                || (c >= '\uf900' && c <= '\ufaff'))
            {
                return true;
            }
        }

        return false;
    }

    private static string RecipeCategoryLabel(Recipe recipe)
    {
        try { return UeiI18n.RecipeCategoryLabel(recipe.category); }
        catch { return UeiI18n.T("recipe.fallback"); }
    }

    private static UeiEntry? TryGetResultEntry(Recipe recipe)
    {
        try
        {
            if (recipe.result == null || string.IsNullOrWhiteSpace(recipe.result.id))
            {
                return null;
            }

            UeiCatalog.TryGetEntry(recipe.result.isLiquid ? UeiEntryKind.Liquid : UeiEntryKind.Item, recipe.result.id, out UeiEntry? entry);
            return entry;
        }
        catch
        {
            return null;
        }
    }

    private static UeiEntry? TryGetSpecificIngredientEntry(RecipeItem item)
    {
        try
        {
            if (item == null || !item.specific || string.IsNullOrWhiteSpace(item.specificId))
            {
                return null;
            }

            UeiCatalog.TryGetEntry(item.isLiquid ? UeiEntryKind.Liquid : UeiEntryKind.Item, item.specificId, out UeiEntry? entry);
            return entry;
        }
        catch
        {
            return null;
        }
    }

    private static string IngredientLabel(RecipeItem item)
    {
        try
        {
            if (item.specific && !string.IsNullOrWhiteSpace(item.specificId))
            {
                return item.specificId;
            }

            if (item.quality != null && !string.IsNullOrWhiteSpace(item.quality.id))
            {
                return (item.isLiquid ? UeiI18n.T("ingredient.liquidShort") : UeiI18n.T("ingredient.anyShort"))
                    + "\n" + item.quality.amount.ToString("0.#");
            }
        }
        catch
        {
        }

        return item.isLiquid ? UeiI18n.T("ingredient.liquid") : UeiI18n.T("ingredient.any");
    }

    private static string GenericIngredientTitle(RecipeItem item)
    {
        string quality = QualityDisplayName(item?.quality);
        return UeiI18n.Format(item != null && item.isLiquid ? "ingredient.anyLiquidTitle" : "ingredient.anyItemTitle", quality);
    }

    private static string GenericIngredientTooltip(RecipeItem item)
    {
        if (item == null)
        {
            return UeiI18n.T("ingredient.noMatches");
        }

        List<string> lines = new();
        string quality = QualityDisplayName(item.quality);
        string amount = item.quality != null ? item.quality.amount.ToString("0.##") : "?";
        lines.Add(UeiI18n.T("ingredient.requirement") + ": " + UeiI18n.T("ingredient.quality") + " " + quality + " >= " + amount);
        if (!item.isLiquid && item.minimumCondition > 0f)
        {
            lines.Add(UeiI18n.T("ingredient.minimumCondition") + ": " + (item.minimumCondition * 100f).ToString("0.#") + "%");
        }

        lines.Add(string.Empty);
        List<string> candidates = item.isLiquid ? MatchingLiquidLines(item) : MatchingItemLines(item);
        lines.Add((item.isLiquid ? UeiI18n.T("ingredient.matchingLiquids") : UeiI18n.T("ingredient.matchingItems")) + " (" + candidates.Count + "):");
        if (candidates.Count == 0)
        {
            lines.Add(UeiI18n.T("ingredient.noMatches"));
            return string.Join("\n", lines);
        }

        const int maxShown = 18;
        foreach (string candidate in candidates.Take(maxShown))
        {
            lines.Add("- " + candidate);
        }
        if (candidates.Count > maxShown)
        {
            lines.Add(UeiI18n.Format("ingredient.moreMatches", candidates.Count - maxShown));
        }

        return string.Join("\n", lines);
    }

    private static List<string> MatchingItemLines(RecipeItem item)
    {
        if (item.quality == null || string.IsNullOrWhiteSpace(item.quality.id))
        {
            return new List<string>();
        }

        return UeiCatalog.Entries
            .Where(entry => entry.Kind == UeiEntryKind.Item)
            .Select(entry => new
            {
                Entry = entry,
                Quality = entry.Qualities.FirstOrDefault(q => string.Equals(q.id, item.quality.id, StringComparison.OrdinalIgnoreCase)
                    && q.amount >= item.quality.amount),
            })
            .Where(x => x.Quality != null)
            .OrderBy(x => x.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Entry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry.DisplayName + " [" + x.Entry.Id + "]  " + x.Quality!.amount.ToString("0.##"))
            .ToList();
    }

    private static List<string> MatchingLiquidLines(RecipeItem item)
    {
        if (item.quality == null || string.IsNullOrWhiteSpace(item.quality.id))
        {
            return new List<string>();
        }

        return UeiCatalog.Entries
            .Where(entry => entry.Kind == UeiEntryKind.Liquid)
            .Select(entry => new
            {
                Entry = entry,
                Quality = entry.Qualities.FirstOrDefault(q => string.Equals(q.id, item.quality.id, StringComparison.OrdinalIgnoreCase)
                    && q.amount > 0f),
            })
            .Where(x => x.Quality != null)
            .OrderBy(x => x.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Entry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x =>
            {
                float ml = Mathf.Ceil(item.quality.amount / Mathf.Max(0.0001f, x.Quality!.amount));
                return x.Entry.DisplayName + " [" + x.Entry.Id + "]  " + UeiI18n.Format("ingredient.needsMl", ml.ToString("0.#"));
            })
            .ToList();
    }

    private static string QualityDisplayName(CraftingQuality? quality)
    {
        if (quality == null || string.IsNullOrWhiteSpace(quality.id))
        {
            return "?";
        }

        string localeName = string.Empty;
        try { localeName = quality.LocaleName; }
        catch { localeName = string.Empty; }

        if (string.IsNullOrWhiteSpace(localeName) || string.Equals(localeName, "cq" + quality.id, StringComparison.OrdinalIgnoreCase))
        {
            return UeiI18n.QualityLabel(quality.id, quality.id);
        }

        string localized = UeiI18n.QualityLabel(quality.id, localeName);
        return localized + " (" + quality.id + ")";
    }

    private static void BindTooltip(GameObject target, UeiEntry entry)
    {
        string tipName = BuildTooltipName(entry);
        string tipDesc = BuildTooltipDescription(entry);

        BindTooltip(target, tipName, tipDesc);
    }

    private static void BindTooltip(GameObject target, string tipName, string tipDesc)
    {
        UITooltip tooltip = target.GetComponent<UITooltip>() ?? target.AddComponent<UITooltip>();
        tooltip.skipLocale = true;
        tooltip.tipName = tipName;
        tooltip.tipDesc = tipDesc;

        UeiTooltipRelay relay = target.GetComponent<UeiTooltipRelay>() ?? target.AddComponent<UeiTooltipRelay>();
        relay.Init(tipName, tipDesc);
    }

    private static string BuildTooltipName(UeiEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
        {
            return entry.DisplayName;
        }

        ItemInfo? info = entry.ItemInfo;
        if (info != null && !string.IsNullOrWhiteSpace(info.fullName))
        {
            return info.fullName;
        }

        return string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.Id : entry.DisplayName;
    }

    private static string BuildTooltipDescription(UeiEntry entry)
    {
        if (entry.Kind == UeiEntryKind.Liquid)
        {
            LiquidType? liquid = entry.LiquidInfo;
            if (liquid == null)
            {
                return entry.Id;
            }

            List<string> lines = new();
            if (!string.IsNullOrWhiteSpace(entry.Description) && entry.Description != entry.Id + "dsc")
            {
                lines.Add(entry.Description);
                lines.Add(string.Empty);
            }
            lines.Add(UeiI18n.T("tooltip.id") + ": " + entry.Id);
            lines.Add(UeiI18n.T("tooltip.value") + ": " + liquid.valuePerLiter.ToString("0.##") + "/L");
            if (liquid.injectable)
            {
                lines.Add(UeiI18n.T("tooltip.injectable"));
            }
            if (liquid.healthUsable)
            {
                lines.Add(UeiI18n.T("tooltip.healthUse"));
            }
            return string.Join("\n", lines);
        }

        ItemInfo? info = entry.ItemInfo;
        if (info == null)
        {
            return entry.Id;
        }

        string description = !string.IsNullOrWhiteSpace(entry.Description)
            ? entry.Description
            : info.description;
        string weightLabel = UeiI18n.OtherText("weight", UeiI18n.T("tooltip.weight"));

        string body = string.IsNullOrWhiteSpace(description) || description == entry.Id + "dsc"
            ? entry.Id
            : description;
        return body + "\n\n<color=#ffffff><sprite index=0 tint=1>" + weightLabel + ": " + info.weight.ToString("0.##") + "u";
    }

    internal static void SetNativeTooltip(string tipName, string tipDesc)
    {
        try
        {
            if (GlobalDark.main != null && !string.IsNullOrWhiteSpace(tipName))
            {
                GlobalDark.main.SetTooltip((tipName, tipDesc ?? string.Empty));
                _ownsNativeTooltip = true;
            }
        }
        catch
        {
        }
    }

    internal static void ClearNativeTooltip()
    {
        if (!_ownsNativeTooltip)
        {
            return;
        }

        try
        {
            if (GlobalDark.main != null)
            {
                GlobalDark.main.SetTooltip((string.Empty, string.Empty));
            }
        }
        catch
        {
        }

        _ownsNativeTooltip = false;
    }

    private static Item? FindOwnedItem(string itemId)
    {
        try
        {
            PlayerCamera camera = PlayerCamera.main;
            if (camera == null || camera.body == null)
            {
                return null;
            }

            if (camera.dragItem != null && camera.dragItem.id == itemId)
            {
                return camera.dragItem;
            }

            List<Item> items = camera.body.GetAllItemsThorough();
            foreach (Item item in items)
            {
                if (item != null && string.Equals(item.id, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private void UpdateSelectionText(int filteredCount)
    {
        if (_selectedEntry != null)
        {
            string suffix = string.IsNullOrWhiteSpace(_statusMessage) ? string.Empty : " - " + _statusMessage;
            _selectionText.text = _selectedEntry.DisplayName + suffix;
        }
        else if (filteredCount == 0)
        {
            _selectionText.text = UeiI18n.T("selection.noResults");
        }
        else
        {
            _selectionText.text = UeiI18n.Format("selection.entries", filteredCount);
        }
    }

    private void UpdatePanelSize(PlayerCamera camera)
    {
        string position = CurrentPositionMode();
        float panelScale = CurrentPanelScale();
        if (Screen.width == _lastScreenWidth
            && Screen.height == _lastScreenHeight
            && string.Equals(position, _lastPanelPosition, StringComparison.OrdinalIgnoreCase)
            && Mathf.Abs(panelScale - _lastPanelScale) < 0.001f)
        {
            return;
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        _lastPanelPosition = position;
        _lastPanelScale = panelScale;
        float scale = Mathf.Max(0.01f, camera.mainCanvas.transform.localScale.y);
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        ApplyPanelAnchors();
        ApplyPanelScale(panelScale);

        float width = Mathf.Clamp(screenWidth * 0.145f, 280f, 320f);

        float cell = Mathf.Floor((width - _grid.spacing.x * (Columns - 1)) / Columns);
        cell = Mathf.Clamp(cell, 38f, 48f);
        _grid.cellSize = new Vector2(cell, cell);

        float gridFrameInset = 4f;
        float gridHeight = cell * Rows + _grid.spacing.y * (Rows - 1) + gridFrameInset;
        _gridFrameLayout.preferredHeight = gridHeight;
        _gridFrameLayout.minHeight = gridHeight;

        float contentHeight =
            HeaderHeight
            + PanelSpacing + gridHeight
            + PanelSpacing + SelectionHeight
            + PanelSpacing + SearchInputHeight;
        float height = Mathf.Min(contentHeight, Mathf.Max(contentHeight, screenHeight * 0.95f / panelScale));
        _panelRect.sizeDelta = new Vector2(width, height);
        _panelRect.anchoredPosition = PanelShownPosition();
        _detailRect.sizeDelta = new Vector2(DetailPanelWidth, height);
        _detailRect.anchoredPosition = DetailShownPosition();
        _settingsRect.sizeDelta = new Vector2(DetailPanelWidth, Math.Min(320f, height));
        _settingsRect.anchoredPosition = DetailShownPosition();
    }

    private Vector2 DetailShownPosition()
    {
        float scale = CurrentPanelScale();
        if (IsRightPosition())
        {
            return new Vector2(PanelShownPosition().x - _panelRect.sizeDelta.x * scale - DetailGap, 0f);
        }

        return new Vector2(PanelShownPosition().x + _panelRect.sizeDelta.x * scale + DetailGap, 0f);
    }

    private static Vector2 PanelShownPosition()
    {
        return IsRightPosition() ? new Vector2(-PanelEdgeInset, 0f) : new Vector2(PanelEdgeInset, 0f);
    }

    private static Vector2 PanelHiddenOffset()
    {
        return IsRightPosition() ? new Vector2(42f, 0f) : new Vector2(-42f, 0f);
    }

    private void ApplyPanelAnchors()
    {
        bool right = IsRightPosition();
        Vector2 anchor = new(right ? 1f : 0f, 0.5f);
        Vector2 pivot = new(right ? 1f : 0f, 0.5f);
        ApplyAnchor(_panelRect, anchor, pivot);
        ApplyAnchor(_detailRect, anchor, pivot);
        ApplyAnchor(_settingsRect, anchor, pivot);
    }

    private void ApplyPanelScale(float scale)
    {
        Vector3 value = Vector3.one * scale;
        _panelRect.localScale = value;
        _detailRect.localScale = value;
        _settingsRect.localScale = value;
    }

    private static void ApplyAnchor(RectTransform rect, Vector2 anchor, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
    }

    private TMP_InputField AddInput(Transform parent, string name, string placeholder)
    {
        GameObject go = AddImage(parent, name, SearchColor, raycast: true);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = SearchInputHeight;
        layout.minHeight = SearchInputHeight;

        AddPixelBorder(go.transform, PixelBorderColor, SearchBorderThickness);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.interactable = true;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = TextColor;
        input.caretWidth = 2;
        input.selectionColor = new Color(0.55f, 0.65f, 0.75f, 0.35f);

        GameObject textArea = NewUiObject("TextArea", go.transform);
        RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.SetStretch();
        textAreaRt.offsetMin = new Vector2(SearchHorizontalPadding, SearchVerticalPadding);
        textAreaRt.offsetMax = new Vector2(-SearchHorizontalPadding, -SearchVerticalPadding);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholderText = AddText(textArea.transform, "Placeholder", placeholder, SearchTextSize, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        placeholderText.rectTransform.SetStretch();
        placeholderText.margin = Vector4.zero;

        TextMeshProUGUI text = AddText(textArea.transform, "Text", string.Empty, SearchTextSize, TextColor, TextAlignmentOptions.MidlineLeft);
        text.rectTransform.SetStretch();
        text.margin = Vector4.zero;

        input.textViewport = textAreaRt;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.fontAsset = _font;
        input.pointSize = SearchTextSize;
        return input;
    }

    private Button AddButton(Transform parent, string name, string label, float size)
    {
        return AddButton(parent, name, label, size, 24f, 24f);
    }

    private Button AddButton(Transform parent, string name, string label, float size, float height, float minWidth)
    {
        GameObject go = AddImage(parent, name, SearchColor, raycast: true);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        layout.minWidth = minWidth;

        AddPixelBorder(go.transform, PixelBorderColor, SearchBorderThickness);

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.fadeDuration = 0.04f;
        button.colors = colors;

        TextMeshProUGUI text = AddText(go.transform, "Label", label, size, TextColor, TextAlignmentOptions.Center);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.rectTransform.SetStretch();
        return button;
    }

    private Slider AddSlider(Transform parent, string name, float minValue, float maxValue, float value)
    {
        GameObject go = NewUiObject(name, parent);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 22f;
        layout.minHeight = 22f;

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = true;
        slider.value = Mathf.Clamp(value, minValue, maxValue);
        slider.direction = Slider.Direction.LeftToRight;

        GameObject track = AddImage(go.transform, "Track", SearchColor, raycast: true);
        RectTransform trackRt = track.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0f, 0.5f);
        trackRt.anchorMax = new Vector2(1f, 0.5f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.offsetMin = new Vector2(0f, -4f);
        trackRt.offsetMax = new Vector2(0f, 4f);
        AddPixelBorder(track.transform, PixelBorderColor, 1f);

        GameObject fill = AddImage(go.transform, "Fill", new Color(0.72f, 0.76f, 0.78f, 0.65f), raycast: false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = new Vector2(1f, -3f);
        fillRt.offsetMax = new Vector2(-1f, 3f);

        GameObject handleArea = NewUiObject("HandleSlideArea", go.transform);
        RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.SetStretch();

        GameObject handle = AddImage(handleArea.transform, "Handle", TextColor, raycast: true);
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0.5f);
        handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(10f, 18f);
        AddPixelBorder(handle.transform, SearchColor, 1f);

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private void AddGearIcon(Transform parent)
    {
        GameObject holder = NewUiObject("GearIcon", parent);
        RectTransform rt = holder.GetComponent<RectTransform>();
        rt.SetStretch();

        AddGearPixel(holder.transform, "Center", new Vector2(6f, 6f), Vector2.zero);
        AddGearPixel(holder.transform, "Top", new Vector2(4f, 3f), new Vector2(0f, 6f));
        AddGearPixel(holder.transform, "Bottom", new Vector2(4f, 3f), new Vector2(0f, -6f));
        AddGearPixel(holder.transform, "Left", new Vector2(3f, 4f), new Vector2(-6f, 0f));
        AddGearPixel(holder.transform, "Right", new Vector2(3f, 4f), new Vector2(6f, 0f));
        AddGearPixel(holder.transform, "TopLeft", new Vector2(3f, 3f), new Vector2(-4.5f, 4.5f));
        AddGearPixel(holder.transform, "TopRight", new Vector2(3f, 3f), new Vector2(4.5f, 4.5f));
        AddGearPixel(holder.transform, "BottomLeft", new Vector2(3f, 3f), new Vector2(-4.5f, -4.5f));
        AddGearPixel(holder.transform, "BottomRight", new Vector2(3f, 3f), new Vector2(4.5f, -4.5f));
        AddGearPixel(holder.transform, "Hole", new Vector2(3f, 3f), Vector2.zero, SearchColor);
    }

    private void AddGearPixel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        AddGearPixel(parent, name, size, position, TextColor);
    }

    private void AddGearPixel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        GameObject go = AddImage(parent, "Gear." + name, color, raycast: false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private Image AddIcon(Transform parent, string name, Sprite? sprite, Color color, float maxSize)
    {
        GameObject go = NewUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? color : new Color(0.38f, 0.40f, 0.42f, 1f);
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(maxSize, maxSize);
        if (sprite != null && sprite.texture != null)
        {
            rt.sizeDelta = PlayerCamera.ImageSizeDelta(sprite.texture, 3f, maxSize);
        }

        return image;
    }

    private GameObject AddImage(Transform parent, string name, Color color, bool raycast)
    {
        GameObject go = NewUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }

    private void AddPixelBorder(Transform parent, Color color, float thickness)
    {
        AddBorderEdge(parent, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, thickness));
        AddBorderEdge(parent, "Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, thickness));
        AddBorderEdge(parent, "Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(thickness, 0f));
        AddBorderEdge(parent, "Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(thickness, 0f));
    }

    private void AddBorderEdge(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
    {
        GameObject edge = AddImage(parent, "Border." + name, color, raycast: false);
        RectTransform rt = edge.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private TextMeshProUGUI AddText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = NewUiObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (_font != null)
        {
            tmp.font = _font;
        }

        tmp.fontSize = size;
        tmp.color = color;
        tmp.text = text;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private GameObject NewUiObject(string name, Transform parent)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        if (_uiLayer >= 0)
        {
            go.layer = _uiLayer;
        }

        return go;
    }

    private Camera? GetUiCamera()
    {
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
    }

    private bool IsPointerInsidePanelScreenBounds(Camera? uiCamera)
    {
        return IsPointerInsideScreenBounds(_panelRect, uiCamera)
            || (_detailPanel.activeInHierarchy && IsPointerInsideScreenBounds(_detailRect, uiCamera))
            || (_settingsPanel.activeInHierarchy && IsPointerInsideScreenBounds(_settingsRect, uiCamera));
    }

    private static bool IsPointerInsideScreenBounds(RectTransform rect, Camera? uiCamera)
    {
        Vector3[] corners = PointerCornerBuffer;
        rect.GetWorldCorners(corners);
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        Vector2 mouse = Input.mousePosition;
        return mouse.x >= min.x - PointerBoundsPadding
            && mouse.x <= max.x + PointerBoundsPadding
            && mouse.y >= min.y - PointerBoundsPadding
            && mouse.y <= max.y + PointerBoundsPadding;
    }

    private bool IsPointerOverPanelGraphic()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        PointerEventData eventData = new(eventSystem)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new();
        eventSystem.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;
            if (hit != null
                && (hit.transform.IsChildOf(_panelRect)
                    || hit.transform.IsChildOf(_detailRect)
                    || hit.transform.IsChildOf(_settingsRect)))
            {
                return true;
            }
        }

        return false;
    }

    private static TMP_FontAsset? FindFont(Canvas canvas)
    {
        try
        {
            PlayerCamera camera = PlayerCamera.main;
            if (camera != null)
            {
                if (camera.statusText != null && camera.statusText.font != null)
                {
                    return camera.statusText.font;
                }
                if (camera.holdingText != null && camera.holdingText.font != null)
                {
                    return camera.holdingText.font;
                }
            }

            TextMeshProUGUI existing = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existing != null && existing.font != null)
            {
                return existing.font;
            }

            return Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        foreach (EventSystem existing in Resources.FindObjectsOfTypeAll<EventSystem>())
        {
            if (existing == null)
            {
                continue;
            }

            if (existing.gameObject.scene.name != "DontDestroyOnLoad")
            {
                UnityEngine.Object.DontDestroyOnLoad(existing.gameObject);
            }
            return;
        }

        GameObject go = new("UEI.EventSystem");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}

internal static class UeiRectTransformExtensions
{
    public static void SetStretch(this RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static LayoutElement GetOrAddLayoutElement(this Component component)
    {
        return component.gameObject.GetComponent<LayoutElement>()
            ?? component.gameObject.AddComponent<LayoutElement>();
    }
}

internal sealed class UeiEntryClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Action? _leftClick;
    private Action? _rightClick;

    public void Init(Action leftClick, Action rightClick)
    {
        _leftClick = leftClick;
        _rightClick = rightClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _rightClick?.Invoke();
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftClick?.Invoke();
        }
    }
}

internal sealed class UeiTooltipRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private string _tipName = string.Empty;
    private string _tipDesc = string.Empty;
    private bool _hovering;

    public void Init(string tipName, string tipDesc)
    {
        _tipName = tipName ?? string.Empty;
        _tipDesc = tipDesc ?? string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        PushTooltip();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_hovering)
        {
            PushTooltip();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        ClearTooltip();
    }

    private void OnDisable()
    {
        if (_hovering)
        {
            _hovering = false;
            ClearTooltip();
        }
    }

    private void PushTooltip()
    {
        UeiPanel.SetNativeTooltip(_tipName, _tipDesc);
    }

    private static void ClearTooltip()
    {
        UeiPanel.ClearNativeTooltip();
    }
}
