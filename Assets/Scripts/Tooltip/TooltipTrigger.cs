using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 掛在任何 UI 物件上，游標移上去就顯示一個懸浮說明。內容由呼叫端用函式提供（每次顯示時重新取，數字永遠是最新的）。
/// 兩種偵測方式自動選擇：
///   · 一般螢幕 UI（有開 Raycast Target 的圖）：用 EventSystem 的指標事件，會正確被其他面板蓋住
///   · 3D 世界裡的 UI（敵人頭上）或沒有可點圖形的物件：直接用游標位置對這個物件的範圍做命中判斷，
///     不需要在世界 Canvas 上加 Raycaster，也不會擋到原本點敵人選目標的操作
/// </summary>
[DisallowMultipleComponent]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Func<TooltipContent> provider;
    private RectTransform rect;
    private Canvas rootCanvas;
    private bool manualHitTest;
    private bool hovering;
    private bool showWhilePressed;

    /// <summary>掛上（或更新）說明；重複呼叫只會換掉內容函式。</summary>
    public static TooltipTrigger Attach(GameObject target, Func<TooltipContent> provider, bool showWhilePressed = false)
    {
        if (target == null)
            return null;

        TooltipTrigger trigger = target.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<TooltipTrigger>();

        trigger.provider = provider;
        trigger.showWhilePressed = showWhilePressed;
        return trigger;
    }

    private void Awake()
    {
        rect = transform as RectTransform;

        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;

        Graphic graphic = GetComponent<Graphic>();
        bool worldSpace = rootCanvas != null && rootCanvas.renderMode == RenderMode.WorldSpace;
        bool noRaycastGraphic = graphic == null || !graphic.raycastTarget;
        manualHitTest = rect != null && (worldSpace || noRaycastGraphic);
    }

    private void OnDisable()
    {
        End();
    }

    private void Update()
    {
        if (!manualHitTest)
            return;

        Mouse mouse = Mouse.current;
        bool inside = mouse != null && rect != null
            && RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue(), EventCamera());

        if (inside && !hovering)
            Begin();
        else if (!inside && hovering)
            End();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!manualHitTest)
            Begin();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!manualHitTest)
            End();
    }

    private Camera EventCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    private void Begin()
    {
        if (provider == null)
            return;

        hovering = true;
        TooltipSystem.Instance.Show(this, provider, showWhilePressed);
    }

    private void End()
    {
        if (!hovering)
            return;

        hovering = false;
        TooltipSystem.Existing?.Hide(this);   // 關閉遊戲時不要為了隱藏而新建一個說明框
    }
}
