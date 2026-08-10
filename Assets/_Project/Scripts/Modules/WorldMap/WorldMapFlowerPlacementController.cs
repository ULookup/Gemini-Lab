#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 花朵自由摆放入口。
    /// UI 层级、花朵按钮、网格基准和有效草地区域由 Scene / Inspector 作者化；
    /// 运行时只负责选择、预览、吸附和落位。
    /// </summary>
    public sealed class WorldMapFlowerPlacementController : MonoBehaviour
    {
        public enum PlacementVisualType
        {
            Single,
            Cluster
        }

        [Serializable]
        public sealed class FlowerOption
        {
            [SerializeField] private string _id = string.Empty;
            [SerializeField] private string _displayName = string.Empty;
            [SerializeField] private Vector2Int _singleFootprint = Vector2Int.one;
            [SerializeField] private Vector2Int _clusterFootprint = new(2, 2);

            public string Id => _id;
            public string DisplayName => _displayName;
            public Vector2Int SingleFootprint => _singleFootprint;
            public Vector2Int ClusterFootprint => _clusterFootprint;
        }

        [Header("Scene / Inspector 引用")]
        [SerializeField] private Button? _openButton;
        [SerializeField] private GameObject? _inventoryPanel;
        [SerializeField] private Transform? _flowerButtonRoot;
        [SerializeField] private Button? _singleButton;
        [SerializeField] private Button? _clusterButton;
        [SerializeField] private Button? _cancelButton;
        [SerializeField] private GameObject? _placementStatusBar;
        [SerializeField] private Collider2D? _placementSurface;
        [SerializeField] private Transform? _placementRoot;
        [SerializeField] private Sprite? _gridReferenceSprite;

        [Header("摆放参数")]
        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField] private bool _useSurfaceBoundsAsGridOrigin = true;
        [SerializeField] private Vector2 _gridOrigin;
        [SerializeField] private PlacementVisualType _defaultVisualType = PlacementVisualType.Single;
        [SerializeField] private string _sortingLayerName = "Furniture";
        [SerializeField] private int _sortingOrder = 12;
        [SerializeField, Min(0.05f)] private float _previewAlpha = 0.5f;
        [SerializeField] private Color _validPreviewColor = Color.white;
        [SerializeField] private Color _invalidPreviewColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private bool _showGridInPlacement = true;
        [SerializeField] private Color _gridColor = new(1f, 1f, 1f, 0.32f);
        [SerializeField, Min(0.005f)] private float _gridLineWidth = 0.025f;
        [SerializeField] private List<FlowerOption> _options = new();

        private readonly List<Button> _flowerButtons = new();
        private Camera? _camera;
        private GameObject? _previewObject;
        private SpriteRenderer? _previewRenderer;
        private GameObject? _gridObject;
        private FlowerOption? _selectedOption;
        private PlacementVisualType _selectedVisualType;
        private bool _isSelecting;
        private Vector2Int _selectedFootprint = Vector2Int.one;

        public Vector2 CellSize => _cellSize;
        public bool IsSelecting => _isSelecting;

        private void Awake()
        {
            _camera = Camera.main;
            ResolvePlacementSurfaceFallback();
            _selectedVisualType = _defaultVisualType;
            ApplyGridReferenceSize();
            _cellSize = new Vector2(Mathf.Max(0.05f, _cellSize.x), Mathf.Max(0.05f, _cellSize.y));
            BindButtons();
            SetInventoryVisible(false);
            SetStatusVisible(false);
        }

        private void Update()
        {
            if (_isSelecting && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
                return;
            }

            if (!_isSelecting || _previewRenderer == null || _camera == null)
            {
                return;
            }

            Vector2 worldPoint = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 snapped = SnapToGrid(worldPoint, _selectedFootprint);
            _previewObject!.transform.position = new Vector3(snapped.x, snapped.y, 0f);

            bool valid = IsValidPlacement(snapped, _selectedFootprint);
            _previewRenderer.color = WithAlpha(valid ? _validPreviewColor : _invalidPreviewColor, _previewAlpha);

            if (Input.GetMouseButtonDown(0) && valid && !IsPointerOverUI())
            {
                CommitPlacement(snapped);
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelSelection();
            }
        }

        private void OnDestroy()
        {
            ClearPreview();
            ClearPlacementGrid();
        }

        private void BindButtons()
        {
            if (_openButton != null) _openButton.onClick.AddListener(OpenInventory);
            if (_singleButton != null) _singleButton.onClick.AddListener(() => SelectVisualType(PlacementVisualType.Single));
            if (_clusterButton != null) _clusterButton.onClick.AddListener(() => SelectVisualType(PlacementVisualType.Cluster));
            if (_cancelButton != null) _cancelButton.onClick.AddListener(CancelSelection);

            _flowerButtons.Clear();
            if (_flowerButtonRoot == null) return;

            for (int i = 0; i < _flowerButtonRoot.childCount && i < _options.Count; i++)
            {
                Button? button = _flowerButtonRoot.GetChild(i).GetComponent<Button>();
                if (button == null) continue;
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    // 清除历史场景中可能残留的图鉴 Sprite，库存按钮只显示中性占位样式。
                    image.sprite = null;
                }
                int captured = i;
                button.onClick.AddListener(() => SelectFlower(captured));
                _flowerButtons.Add(button);
            }
        }

        private void OpenInventory()
        {
            CancelSelection();
            SetInventoryVisible(true);
        }

        private void SelectFlower(int index)
        {
            if (index < 0 || index >= _options.Count) return;
            _selectedOption = _options[index];
            SetInventoryVisible(true);
        }

        private void SelectVisualType(PlacementVisualType visualType)
        {
            if (_selectedOption == null) return;
            _selectedVisualType = visualType;
            _selectedFootprint = visualType == PlacementVisualType.Cluster
                ? _selectedOption.ClusterFootprint
                : _selectedOption.SingleFootprint;
            SetInventoryVisible(false);
            SetStatusVisible(true);
            CreatePreview(_selectedFootprint);
            CreatePlacementGrid();
            _isSelecting = true;
        }

        private void SetInventoryVisible(bool visible)
        {
            if (_inventoryPanel != null) _inventoryPanel.SetActive(visible);
        }

        private void SetStatusVisible(bool visible)
        {
            if (_placementStatusBar != null) _placementStatusBar.SetActive(visible);
        }

        private void CreatePreview(Vector2Int footprint)
        {
            ClearPreview();
            _previewObject = new GameObject("FlowerPlacementPreview");
            _previewObject.transform.SetParent(_placementRoot, false);
            _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
            _previewRenderer.sprite = CreatePlaceholderSprite();
            _previewRenderer.sortingLayerName = _sortingLayerName;
            _previewRenderer.sortingOrder = _sortingOrder + 1;
            _previewRenderer.color = WithAlpha(_validPreviewColor, _previewAlpha);
            _previewRenderer.drawMode = SpriteDrawMode.Sliced;
            _previewRenderer.size = Vector2.Scale((Vector2)footprint, _cellSize) * 0.9f;
        }

        private void CommitPlacement(Vector2 position)
        {
            GameObject placed = new GameObject($"PlacedFlower_{_selectedOption!.Id}_{_selectedVisualType}");
            placed.transform.SetParent(_placementRoot, false);
            placed.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer = placed.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatePlaceholderSprite();
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _sortingOrder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.Scale((Vector2)_selectedFootprint, _cellSize) * 0.9f;
            renderer.color = new Color(0.9f, 0.9f, 0.9f, 0.35f);

            BoxCollider2D collider = placed.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.Scale((Vector2)_selectedFootprint, _cellSize);

            var footprint = placed.AddComponent<WorldMapPlacedFlower>();
            footprint.FlowerId = _selectedOption!.Id;
            footprint.VisualType = _selectedVisualType;
            footprint.GridFootprint = _selectedFootprint;
            footprint.CellSize = _cellSize;

            // 保留放置模式，允许玩家连续点击多个有效网格；按 Esc 或取消按钮退出。
        }

        private void CancelSelection()
        {
            _isSelecting = false;
            ClearPreview();
            ClearPlacementGrid();
            SetInventoryVisible(false);
            SetStatusVisible(false);
        }

        private void ClearPreview()
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
                _previewRenderer = null;
            }
        }

        private Vector2 SnapToGrid(Vector2 worldPoint, Vector2Int footprint)
        {
            Vector2 origin = ResolveGridOrigin();
            Vector2 size = Vector2.Scale((Vector2)footprint, _cellSize);
            float x = origin.x + Mathf.Floor((worldPoint.x - origin.x) / _cellSize.x) * _cellSize.x + size.x * 0.5f;
            float y = origin.y + Mathf.Floor((worldPoint.y - origin.y) / _cellSize.y) * _cellSize.y + size.y * 0.5f;
            return new Vector2(x, y);
        }

        private bool IsValidPlacement(Vector2 center, Vector2Int footprint)
        {
            if (_placementSurface == null) return false;
            Vector2 size = Vector2.Scale((Vector2)footprint, _cellSize);
            Bounds bounds = GetPlacementBounds();
            Rect placement = new(center - size * 0.5f, size);
            Rect surface = new(bounds.min, bounds.size);
            if (!surface.Contains(placement.min) || !surface.Contains(placement.max)) return false;

            if (_placementRoot == null) return true;
            foreach (var placed in _placementRoot.GetComponentsInChildren<WorldMapPlacedFlower>(true))
            {
                if (placed == null || placed.gameObject == _previewObject) continue;
                Vector2 otherSize = Vector2.Scale((Vector2)placed.GridFootprint, placed.CellSize);
                Rect other = new((Vector2)placed.transform.position - otherSize * 0.5f, otherSize);
                if (placement.Overlaps(other)) return false;
            }

            return true;
        }

        private Vector2 ResolveGridOrigin()
        {
            if (_useSurfaceBoundsAsGridOrigin && _placementSurface != null)
                return GetPlacementBounds().min;
            return _gridOrigin;
        }

        private void ResolvePlacementSurfaceFallback()
        {
            // 花朵摆放区域与宠物横向移动边界完全分离，避免错误复用窄条 PetMovementBounds。
            var boundsObject = GameObject.Find("FlowerPlacementBounds");
            if (boundsObject != null)
            {
                _placementSurface = boundsObject.GetComponent<Collider2D>();
                return;
            }

            if (_placementSurface != null && _placementSurface.gameObject.name != "PetMovementBounds")
                return;

            // 当前 Editor 会话尚未执行场景作者化时的非视觉兜底；不影响正式 Scene / Inspector 配置。
            var fallbackObject = GameObject.Find("FlowerPlacementBounds_RuntimeFallback");
            if (fallbackObject == null)
            {
                fallbackObject = new GameObject("FlowerPlacementBounds_RuntimeFallback");
                fallbackObject.transform.position = new Vector3(0f, -3f, 0f);
                var fallbackBox = fallbackObject.AddComponent<BoxCollider2D>();
                fallbackBox.size = new Vector2(36f, 8.96f);
                fallbackBox.isTrigger = true;
                fallbackBox.enabled = false;
                _placementSurface = fallbackBox;
                return;
            }

            _placementSurface = fallbackObject.GetComponent<Collider2D>();
        }

        private Bounds GetPlacementBounds()
        {
            if (_placementSurface is BoxCollider2D box)
            {
                Vector3 scale = box.transform.lossyScale;
                Vector2 absoluteScale = new(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                Vector2 size = Vector2.Scale(box.size, absoluteScale);
                Vector2 offset = Vector2.Scale(box.offset, absoluteScale);
                Vector2 center = (Vector2)box.transform.position + offset;
                return new Bounds(center, size);
            }

            return _placementSurface != null ? _placementSurface.bounds : new Bounds();
        }

        private void ApplyGridReferenceSize()
        {
            if (_gridReferenceSprite == null) return;
            Vector2 referenceSize = _gridReferenceSprite.bounds.size;
            if (referenceSize.x > 0f && referenceSize.y > 0f)
                _cellSize = referenceSize;
        }

        private void CreatePlacementGrid()
        {
            ClearPlacementGrid();
            if (!_showGridInPlacement || _placementSurface == null) return;

            Bounds bounds = GetPlacementBounds();
            Vector2 origin = ResolveGridOrigin();
            float startX = origin.x + Mathf.Floor((bounds.min.x - origin.x) / _cellSize.x) * _cellSize.x;
            float startY = origin.y + Mathf.Floor((bounds.min.y - origin.y) / _cellSize.y) * _cellSize.y;
            int verticalCount = Mathf.CeilToInt((bounds.max.x - startX) / _cellSize.x) + 1;
            int horizontalCount = Mathf.CeilToInt((bounds.max.y - startY) / _cellSize.y) + 1;

            _gridObject = new GameObject("FlowerPlacementGrid");
            _gridObject.transform.SetParent(_placementRoot, false);
            for (int i = 0; i < verticalCount; i++)
            {
                float x = startX + i * _cellSize.x;
                CreateGridLine(new Vector3(x, bounds.min.y, 0f), new Vector3(x, bounds.max.y, 0f), i);
            }

            for (int i = 0; i < horizontalCount; i++)
            {
                float y = startY + i * _cellSize.y;
                CreateGridLine(new Vector3(bounds.min.x, y, 0f), new Vector3(bounds.max.x, y, 0f), verticalCount + i);
            }

            Debug.Log($"[WorldMapFlowerPlacement] 网格预览已创建：{verticalCount} 列 x {horizontalCount} 行，区域 {bounds.size}");
        }

        private void CreateGridLine(Vector3 start, Vector3 end, int index)
        {
            var lineObject = new GameObject($"GridLine_{index:000}");
            lineObject.transform.SetParent(_gridObject!.transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = _gridLineWidth;
            line.endWidth = _gridLineWidth;
            line.startColor = _gridColor;
            line.endColor = _gridColor;
            line.sortingLayerName = _sortingLayerName;
            line.sortingOrder = _sortingOrder + 100;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                line.material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                line.material.color = _gridColor;
            }
        }

        private void ClearPlacementGrid()
        {
            if (_gridObject == null) return;
            Destroy(_gridObject);
            _gridObject = null;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Sprite CreatePlaceholderSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    public sealed class WorldMapPlacedFlower : MonoBehaviour
    {
        [SerializeField] private string _flowerId = string.Empty;
        [SerializeField] private WorldMapFlowerPlacementController.PlacementVisualType _visualType;
        [SerializeField] private Vector2Int _gridFootprint = Vector2Int.one;
        [SerializeField] private Vector2 _cellSize = Vector2.one;

        public string FlowerId { get => _flowerId; set => _flowerId = value; }
        public WorldMapFlowerPlacementController.PlacementVisualType VisualType { get => _visualType; set => _visualType = value; }
        public Vector2Int GridFootprint { get => _gridFootprint; set => _gridFootprint = value; }
        public Vector2 CellSize { get => _cellSize; set => _cellSize = value; }
    }
}
