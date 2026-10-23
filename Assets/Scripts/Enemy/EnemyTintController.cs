using System.Collections.Generic;
using UnityEngine;

public enum TintChannel
{
    Stun,
    Flash
}

/// <summary>
/// 敵人 3D 模型的「疊色」管理：多個表現（暈眩、強化閃光…）各自用一個頻道設定顏色與強度，
/// 這裡統一合成後用 MaterialPropertyBlock 套到模型上，不會互相蓋掉，也不會改到共用材質。
/// 同時提供模型外框與頭頂位置，給其他視覺效果定位用。
/// </summary>
[DisallowMultipleComponent]
public class EnemyTintController : MonoBehaviour
{
    private struct Channel
    {
        public Color color;
        public float amount;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<int> colorPropertyIds = new List<int>();
    private readonly List<Color> originalColors = new List<Color>();
    private readonly Channel[] channels = new Channel[2];
    private MaterialPropertyBlock block;
    private Bounds bounds;
    private Vector3 headLocalOffset;
    private bool cached;
    private bool dirty;

    public bool BoundsReady { get; private set; }
    public Bounds ModelBounds => bounds;

    /// <summary>其他特效（例如抖動）暫時加在 transform 上的位移，算頭頂位置時會扣掉。</summary>
    public Vector3 ExternalOffset { get; set; }

    public Vector3 HeadPosition => transform.position - ExternalOffset + headLocalOffset;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
    }

    public void SetChannel(TintChannel channel, Color color, float amount)
    {
        EnsureCache();
        channels[(int)channel] = new Channel { color = color, amount = Mathf.Clamp01(amount) };
        dirty = true;
    }

    private void LateUpdate()
    {
        if (!dirty)
            return;

        dirty = false;
        Apply();
    }

    public void EnsureCache()
    {
        if (cached)
            return;

        cached = true;
        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        Bounds total = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < found.Length; i++)
        {
            Renderer r = found[i];
            // 只處理 3D 模型；粒子、拖尾等不要變色
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer) && !(r is SpriteRenderer))
                continue;

            int propertyId = 0;
            Color original = Color.white;
            Material material = r.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty(BaseColorId)) { propertyId = BaseColorId; original = material.GetColor(BaseColorId); }
                else if (material.HasProperty(ColorId)) { propertyId = ColorId; original = material.GetColor(ColorId); }
            }

            renderers.Add(r);
            colorPropertyIds.Add(propertyId);
            originalColors.Add(original);

            if (!hasBounds) { total = r.bounds; hasBounds = true; }
            else total.Encapsulate(r.bounds);
        }

        if (hasBounds)
        {
            bounds = total;
            headLocalOffset = new Vector3(total.center.x, total.max.y, total.center.z) - transform.position;
            BoundsReady = true;
        }
    }

    private void Apply()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            int propertyId = colorPropertyIds[i];
            r.GetPropertyBlock(block);

            Color final = originalColors[i];
            bool tinted = false;
            for (int c = 0; c < channels.Length; c++)
            {
                if (channels[c].amount <= 0.001f)
                    continue;

                final = Color.Lerp(final, channels[c].color, channels[c].amount);
                tinted = true;
            }

            if (propertyId != 0 && tinted)
                block.SetColor(propertyId, final);
            else
                block.Clear();

            r.SetPropertyBlock(block);
        }
    }
}
