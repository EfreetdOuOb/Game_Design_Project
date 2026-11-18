using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapSettings _mapSettings;
    [SerializeField] private GameObject _mapNode;
    [SerializeField] private GameObject _nodeLine;
    [SerializeField] private RectTransform _mapNodeParentRect;
    [SerializeField] private GameObject _mapPanel;

    [Header("進度提示")]
    [Tooltip("還沒走、也還不能選的連線")]
    [SerializeField] private Color _lineIdleColor = new Color(0f, 0f, 0f, 0.75f);
    [Tooltip("已經走完第一步、現在可以選的連線（下一步的選項）")]
    [SerializeField] private Color _lineAvailableColor = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("已經走過的路")]
    [SerializeField] private Color _lineTraveledColor = new Color(1f, 0.82f, 0.3f, 1f);
    [SerializeField, Min(1f)] private float _availableLineThickness = 1.3f;
    [SerializeField, Min(1f)] private float _traveledLineThickness = 1.8f;

    [Header("自動捲動")]
    [Tooltip("打開地圖時，自動把畫面捲到目前進度的位置")]
    [SerializeField] private bool _autoFocusProgress = true;
    [SerializeField, Min(0f)] private float _focusDuration = 0.6f;

    private MapGraphData _mapGraphData;
    private MapNode[][] _mapNodeArray;
    private MapProgressData _progress;
    private readonly List<MapLineView> _lineViews = new List<MapLineView>();
    private Tween _focusTween;

    private enum LineState
    {
        Idle,       // 還沒走
        Available,  // 下一步可以選的路
        Traveled    // 已走過
    }

    private class MapLineView
    {
        public Image image;
        public RectTransform rect;
        public float baseThickness;
        public int fromLayer;
        public int fromNode;
        public int toLayer;
        public int toNode;
    }

    private void Start()
    {
        InitializeMap();
    }

    public void InitializeMap()
    {
        ClearMapView();
        GenerateMapData();
        SetView();
        CreateMapView();
        SetNodeLines();
        MapNodeTooltip.Create(_mapNodeParentRect);
        InitializeProgress();
        RefreshNodeStates();
        ResetScrollPosition();
        OpenMap();
        TutorialUIControl.Instance?.NotifyMapDisplayed();
    }

    public void ResetRun()
    {
        InitializeMap();
    }

    private void ClearMapView()
    {
        if (_mapNodeParentRect == null)
        {
            return;
        }

        for (int i = _mapNodeParentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(_mapNodeParentRect.GetChild(i).gameObject);
        }

        _mapNodeArray = null;
        _mapGraphData = null;
        _progress = null;
        _lineViews.Clear();
        _focusTween?.Kill();
    }

    private void GenerateMapData()
    {
        MapGenerator generator = new MapGenerator(_mapSettings);
        _mapGraphData = generator.Generate();

        _mapNodeArray = new MapNode[_mapGraphData.layers.Count][];
        for (int i = 0; i < _mapGraphData.layers.Count; i++)
        {
            _mapNodeArray[i] = new MapNode[_mapGraphData.layers[i].nodes.Count];
        }
    }

    private void SetView()
    {
        float layerLength = _mapSettings.mapNodeHeight * (_mapSettings.layerCount - 1) + _mapSettings.padding * 2;

        if (!IsHorizontal)
        {
            var size = _mapNodeParentRect.sizeDelta;
            size.y = layerLength;
            _mapNodeParentRect.sizeDelta = size;
            ConfigureScrollRect();
            return;
        }

        // 橫向：內容區貼著左邊、上下撐滿可視範圍，寬度就是整張地圖的長度。
        // 原點（localPosition 0,0）在內容區的左側正中間，節點才能以它為中心上下展開。
        _mapNodeParentRect.anchorMin = new Vector2(0f, 0f);
        _mapNodeParentRect.anchorMax = new Vector2(0f, 1f);
        _mapNodeParentRect.pivot = new Vector2(0f, 0.5f);
        _mapNodeParentRect.sizeDelta = new Vector2(layerLength, 0f);
        _mapNodeParentRect.anchoredPosition = Vector2.zero;

        ConfigureScrollRect();
    }

    private bool IsHorizontal => _mapSettings.orientation == MapOrientation.Horizontal;

    // 卷動方向跟著地圖方向走：橫向只能左右拖，直向只能上下拖
    private void ConfigureScrollRect()
    {
        ScrollRect scrollRect = _mapNodeParentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
            return;

        scrollRect.horizontal = IsHorizontal;
        scrollRect.vertical = !IsHorizontal;

        // 原本只有直向捲軸；橫向用不到它，關掉並把原本為它讓出的可視範圍還回來
        if (IsHorizontal && scrollRect.verticalScrollbar != null)
        {
            scrollRect.verticalScrollbar.gameObject.SetActive(false);
            scrollRect.verticalScrollbar = null;

            RectTransform viewport = scrollRect.viewport;
            if (viewport != null)
            {
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;
            }
        }
    }

    // 座標用「層方向（along）」和「同層展開方向（cross）」兩個軸來算，最後才依地圖方向換成畫面座標：
    //   直向：cross = 左右、along = 由下往上
    //   橫向：cross = 上下（第 0 個在最上面）、along = 由左往右
    private void CreateMapView()
    {
        int offsetAlong = _mapSettings.padding;

        for (int i = 0; i < _mapGraphData.layers.Count; i++)
        {
            var layer = _mapGraphData.layers[i];
            int offsetCross = -(layer.nodes.Count - 1) * _mapSettings.mapNodeWidth / 2;

            for (int j = 0; j < layer.nodes.Count; j++)
            {
                GameObject nodeObject = Instantiate(_mapNode, _mapNodeParentRect);
                MapNode mapNode = nodeObject.GetComponent<MapNode>();
                _mapNodeArray[i][j] = mapNode;

                MapNodeData mapNodeData = layer.nodes[j];
                mapNode.SetMapNodeType(mapNodeData);
                mapNode.SetIcon(GetNodeSprite(mapNodeData.mapNodeType));

                Button button = nodeObject.GetComponent<Button>();
                int capturedLayer = i;
                int capturedNode = j;

                if (button != null)
                {
                    mapNode._button = button;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnNodeClicked(capturedLayer, capturedNode));
                }

                int roomCross = offsetCross + Random.Range(-_mapSettings.mapNodeWidth / 8, _mapSettings.mapNodeWidth / 8);
                int roomAlong = offsetAlong;

                if (i != 0 && i != _mapSettings.layerCount - 1)
                {
                    roomAlong = offsetAlong + Random.Range(-_mapSettings.mapNodeHeight / 10, _mapSettings.mapNodeHeight / 10);
                }

                nodeObject.transform.localPosition = IsHorizontal
                    ? new Vector3(roomAlong, -roomCross)
                    : new Vector3(roomCross, roomAlong);
                nodeObject.SetActive(true);

                offsetCross += _mapSettings.mapNodeWidth;
            }

            offsetAlong += _mapSettings.mapNodeHeight;
        }
    }

    private void SetNodeLines()
    {
        for (int i = 0; i < _mapGraphData.layers.Count - 1; i++)
        {
            for (int j = 0; j < _mapGraphData.layers[i].nodes.Count; j++)
            {
                MapNode currentNodeView = _mapNodeArray[i][j];
                MapNodeData currentNodeData = _mapGraphData.layers[i].nodes[j];
                Vector2 startPosition = currentNodeView.transform.localPosition;

                foreach (int nextIndex in currentNodeData.nextLayerConnectedNodes)
                {
                    MapNode nextNodeView = _mapNodeArray[i + 1][nextIndex];
                    Vector2 endPosition = nextNodeView.transform.localPosition;

                    GameObject nodeLine = Instantiate(_nodeLine, _mapNodeParentRect);
                    nodeLine.SetActive(true);
                    // Instantiate 預設把新物件加到最後一個 sibling，在 UI 裡代表畫在最上層，
                    // 會蓋住節點按鈕。連線本來就該畫在節點下面，所以固定移到最前面的 sibling。
                    nodeLine.transform.SetAsFirstSibling();
                    SetNodeLinePosition(nodeLine, startPosition, endPosition);

                    RectTransform lineRect = nodeLine.GetComponent<RectTransform>();
                    _lineViews.Add(new MapLineView
                    {
                        image = nodeLine.GetComponent<Image>(),
                        rect = lineRect,
                        baseThickness = lineRect.sizeDelta.y,
                        fromLayer = i,
                        fromNode = j,
                        toLayer = i + 1,
                        toNode = nextIndex
                    });
                }
            }
        }
    }

    private void SetNodeLinePosition(GameObject nodeLine, Vector2 startPosition, Vector2 endPosition)
    {
        RectTransform rectTransform = nodeLine.GetComponent<RectTransform>();
        rectTransform.localPosition = startPosition;

        var size = rectTransform.sizeDelta;
        size.x = Vector2.Distance(startPosition, endPosition);
        rectTransform.sizeDelta = size;

        Vector2 direction = endPosition - startPosition;
        rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private Sprite GetNodeSprite(MapNodeType nodeType)
    {
        MapNodeSpriteSetting setting = _mapSettings.mapNodeSpriteSettings
            .FirstOrDefault(x => x.nodeType == nodeType);

        if (setting != null && setting.icon != null)
            return setting.icon;

        return _mapSettings.defaultNodeSprite;
    }

    private void InitializeProgress()
    {
        _progress = new MapProgressData();

        if (_mapGraphData == null || _mapGraphData.layers.Count == 0)
            return;

        for (int i = 0; i < _mapGraphData.layers[0].nodes.Count; i++)
        {
            _progress.unlockedNodes.Add(_progress.GetKey(0, i));
        }
    }

    private void RefreshNodeStates()
    {
        for (int i = 0; i < _mapNodeArray.Length; i++)
        {
            for (int j = 0; j < _mapNodeArray[i].Length; j++)
            {
                string key = _progress.GetKey(i, j);
                bool unlocked = _progress.unlockedNodes.Contains(key);
                bool completed = _progress.completedNodes.Contains(key);

                _mapNodeArray[i][j].SetState(unlocked, completed);
            }
        }

        RefreshLineStates();
    }

    private void OnNodeClicked(int layerIndex, int nodeIndex)
    {
        string key = _progress.GetKey(layerIndex, nodeIndex);

        if (!_progress.unlockedNodes.Contains(key)) return;
        if (_progress.completedNodes.Contains(key)) return;

        _progress.currentLayer = layerIndex;
        _progress.currentNodeIndex = nodeIndex;

        LockOtherNodesInSameLayer(layerIndex, nodeIndex);
        RefreshNodeStates();

        MapNodeData selectedNode = _mapGraphData.layers[layerIndex].nodes[nodeIndex]; 
        GameFlowController.Instance.StartNode(selectedNode);
    }

    private void LockOtherNodesInSameLayer(int selectedLayer, int selectedNode)
    {
        for (int i = 0; i < _mapGraphData.layers[selectedLayer].nodes.Count; i++)
        {
            if (i == selectedNode) continue;

            string key = _progress.GetKey(selectedLayer, i);
            _progress.unlockedNodes.Remove(key);
        }
    }

    public void CompleteCurrentNode()
    {
        if (_progress == null) return;
        if (_progress.currentLayer < 0 || _progress.currentNodeIndex < 0) return;

        string currentKey = _progress.GetKey(_progress.currentLayer, _progress.currentNodeIndex);
        _progress.completedNodes.Add(currentKey);

        MapNodeData currentNodeData = _mapGraphData.layers[_progress.currentLayer].nodes[_progress.currentNodeIndex];

        int nextLayer = _progress.currentLayer + 1;
        if (nextLayer < _mapGraphData.layers.Count)
        {
            foreach (int nextIndex in currentNodeData.nextLayerConnectedNodes)
            {
                string nextKey = _progress.GetKey(nextLayer, nextIndex);
                _progress.unlockedNodes.Add(nextKey);
            }
        }

        RefreshNodeStates();
    }

    public void OpenMap()
    {
        if (_mapPanel != null)
            _mapPanel.SetActive(true);

        FocusProgress();
    }

    public void CloseMap()
    {
        if (_mapPanel != null)
            _mapPanel.SetActive(false);
    }

    public void ToggleMap()
    {
        if (_mapPanel == null)
            return;

        bool opening = !_mapPanel.activeSelf;
        _mapPanel.SetActive(opening);

        if (opening)
            FocusProgress();
    }

    // 節點面板呼叫「觀看當前地圖」時，暫時關閉節點按鈕互動，
    // 避免玩家在偷看地圖時誤觸目前節點（例如再次打開已開過的寶箱面板）。
    public void SetNodeInteractionEnabled(bool enabled)
    {
        if (_mapNodeArray == null)
            return;

        if (enabled)
        {
            RefreshNodeStates();
            return;
        }

        for (int i = 0; i < _mapNodeArray.Length; i++)
        {
            for (int j = 0; j < _mapNodeArray[i].Length; j++)
            {
                MapNode node = _mapNodeArray[i][j];
                if (node != null && node._button != null)
                    node._button.interactable = false;
            }
        }
    }

    // ---- 進度提示 ----

    // 依目前進度替每一條連線上色：走過的亮金色加粗、下一步的選項偏白、其餘維持暗色
    private void RefreshLineStates()
    {
        if (_progress == null)
            return;

        for (int i = 0; i < _lineViews.Count; i++)
        {
            MapLineView line = _lineViews[i];
            if (line.image == null)
                continue;

            LineState state = GetLineState(line);
            Color color = state == LineState.Traveled ? _lineTraveledColor
                : state == LineState.Available ? _lineAvailableColor
                : _lineIdleColor;
            float thickness = state == LineState.Traveled ? _traveledLineThickness
                : state == LineState.Available ? _availableLineThickness
                : 1f;

            line.image.color = color;
            Vector2 size = line.rect.sizeDelta;
            size.y = line.baseThickness * thickness;
            line.rect.sizeDelta = size;
        }
    }

    private LineState GetLineState(MapLineView line)
    {
        bool fromDone = _progress.completedNodes.Contains(_progress.GetKey(line.fromLayer, line.fromNode));
        if (!fromDone)
            return LineState.Idle;

        string toKey = _progress.GetKey(line.toLayer, line.toNode);
        bool toDone = _progress.completedNodes.Contains(toKey);
        bool toInProgress = _progress.currentLayer == line.toLayer && _progress.currentNodeIndex == line.toNode;
        if (toDone || toInProgress)
            return LineState.Traveled;

        return _progress.unlockedNodes.Contains(toKey) ? LineState.Available : LineState.Idle;
    }

    // 把畫面捲到「目前進度」：還沒完成的可選節點所在的最前面一層；全走完就停在最後一層。
    // 這樣回到地圖時，玩家一眼就在目前位置，不用自己拖。
    private void FocusProgress()
    {
        if (!_autoFocusProgress || _progress == null || _mapNodeArray == null || _mapNodeParentRect == null)
            return;

        ScrollRect scrollRect = _mapNodeParentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect == null || scrollRect.viewport == null || !scrollRect.gameObject.activeInHierarchy)
            return;

        // 地圖剛被打開，版面要先更新，才量得到正確的內容區與可視範圍大小
        Canvas.ForceUpdateCanvases();

        if (!TryGetProgressFocusPoint(out Vector2 focusPoint))
            return;

        Vector2 contentSize = _mapNodeParentRect.rect.size;
        Vector2 viewportSize = scrollRect.viewport.rect.size;
        Vector2 pivot = _mapNodeParentRect.pivot;

        _focusTween?.Kill();

        if (IsHorizontal)
        {
            float scrollable = contentSize.x - viewportSize.x;
            float distanceFromLeft = focusPoint.x + pivot.x * contentSize.x;
            float target = scrollable <= 0f ? 0f : Mathf.Clamp01((distanceFromLeft - viewportSize.x * 0.5f) / scrollable);
            _focusTween = _focusDuration > 0f
                ? scrollRect.DOHorizontalNormalizedPos(target, _focusDuration).SetEase(Ease.OutCubic).SetLink(scrollRect.gameObject)
                : null;
            if (_focusDuration <= 0f)
                scrollRect.horizontalNormalizedPosition = target;
        }
        else
        {
            float scrollable = contentSize.y - viewportSize.y;
            float distanceFromBottom = focusPoint.y + pivot.y * contentSize.y;
            float target = scrollable <= 0f ? 0f : Mathf.Clamp01((distanceFromBottom - viewportSize.y * 0.5f) / scrollable);
            _focusTween = _focusDuration > 0f
                ? scrollRect.DOVerticalNormalizedPos(target, _focusDuration).SetEase(Ease.OutCubic).SetLink(scrollRect.gameObject)
                : null;
            if (_focusDuration <= 0f)
                scrollRect.verticalNormalizedPosition = target;
        }
    }

    private bool TryGetProgressFocusPoint(out Vector2 point)
    {
        point = Vector2.zero;

        int focusLayer = _mapNodeArray.Length - 1;
        for (int i = 0; i < _mapNodeArray.Length; i++)
        {
            if (HasSelectableNode(i))
            {
                focusLayer = i;
                break;
            }
        }

        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int j = 0; j < _mapNodeArray[focusLayer].Length; j++)
        {
            MapNode node = _mapNodeArray[focusLayer][j];
            if (node == null)
                continue;

            // 該層有可選節點時只看可選的，沒有（例如全走完）就取整層
            bool selectable = _progress.unlockedNodes.Contains(_progress.GetKey(focusLayer, j))
                && !_progress.completedNodes.Contains(_progress.GetKey(focusLayer, j));
            if (HasSelectableNode(focusLayer) && !selectable)
                continue;

            sum += (Vector2)node.transform.localPosition;
            count++;
        }

        if (count == 0)
            return false;

        point = sum / count;
        return true;
    }

    private bool HasSelectableNode(int layer)
    {
        for (int j = 0; j < _mapNodeArray[layer].Length; j++)
        {
            string key = _progress.GetKey(layer, j);
            if (_progress.unlockedNodes.Contains(key) && !_progress.completedNodes.Contains(key))
                return true;
        }

        return false;
    }

    // 回到起點：直向是最下面、橫向是最左邊
    private void ResetScrollPosition()
    {
        ScrollRect scrollRect = _mapNodeParentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
            return;

        // 內容區剛改過大小，先讓版面更新完，再設定捲動位置才會對
        Canvas.ForceUpdateCanvases();

        if (IsHorizontal)
            scrollRect.horizontalNormalizedPosition = 0f;
        else
            scrollRect.verticalNormalizedPosition = 0f;
    }
}