using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敵人暈眩（麻痺、韌性歸零共用）時的表現：整隻變色 + 輕微抖動 + 頭上轉圈的星星。
/// 由 <see cref="EnemyPoise"/> 在 Awake 自動掛上預設版本；想自訂（換顏色、換成自己的特效 prefab）
/// 就直接在敵人 prefab 上掛這個元件，EnemyPoise 發現已經有就不會再加。
/// 只負責「看起來」，不影響任何戰鬥數值。
/// </summary>
[RequireComponent(typeof(EnemyTintController))]
[DisallowMultipleComponent]
public class EnemyStunVisual : MonoBehaviour
{
    [Header("變色")]
    [SerializeField] private Color _tintColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.55f;
    [Tooltip("變色強弱來回閃動的速度（次/秒），0 = 不閃動")]
    [SerializeField, Min(0f)] private float _tintPulseSpeed = 3f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

    [Header("抖動")]
    [Tooltip("抖動幅度，單位是敵人外框大小的比例")]
    [SerializeField, Min(0f)] private float _shakeAmount = 0.02f;
    [SerializeField, Min(0f)] private float _shakeSpeed = 55f;

    [Header("頭上特效")]
    [Tooltip("選填：放你自己的特效 prefab（暈眩星星、電光…）。留空就用程式產生的轉圈星星。")]
    [SerializeField] private GameObject _customEffectPrefab;
    [Tooltip("頭上特效離敵人最高點多高，單位是敵人高度的比例")]
    [SerializeField] private float _headOffset = 0.12f;
    [SerializeField, Min(1)] private int _starCount = 3;
    [SerializeField] private Color _starColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float _orbitDegreesPerSecond = 220f;

    private static Sprite starSprite;

    private EnemyPoise poise;
    private EnemyTintController tint;
    private bool stunned;
    private float strength;          // 0 ~ 1，目前變色／抖動程度（淡入淡出）
    private Tween strengthTween;
    private Vector3 appliedShake;

    private Transform effectRoot;
    private Transform[] stars;
    private float starRadius;
    private float orbitAngle;

    private void Awake()
    {
        poise = GetComponent<EnemyPoise>();
        tint = GetComponent<EnemyTintController>();
    }

    private void OnEnable()
    {
        if (poise == null)
            return;

        poise.OnStunChanged += HandleStunChanged;
        HandleStunChanged();
    }

    private void OnDisable()
    {
        if (poise != null)
            poise.OnStunChanged -= HandleStunChanged;

        SetStunned(false, true);

        // 停用時把抖動的偏移還回去，避免敵人停在偏掉的位置
        transform.position -= appliedShake;
        appliedShake = Vector3.zero;
    }

    private void OnDestroy()
    {
        strengthTween?.Kill();

        // 頭上特效是獨立物件（不跟著敵人的縮放），要自己清掉
        if (effectRoot != null)
            Destroy(effectRoot.gameObject);
    }

    private void HandleStunChanged()
    {
        SetStunned(poise != null && poise.StunTurns > 0, false);
    }

    private void SetStunned(bool value, bool immediate)
    {
        if (stunned == value && !immediate)
            return;

        stunned = value;

        strengthTween?.Kill();
        float target = stunned ? 1f : 0f;
        if (immediate || _fadeDuration <= 0f)
        {
            strength = target;
            ApplyTint(strength);
        }
        else
        {
            strengthTween = DOTween.To(() => strength, v => strength = v, target, _fadeDuration)
                .SetLink(gameObject)
                .OnComplete(() => ApplyTint(strength));
        }

        if (effectRoot == null && stunned)
            BuildHeadEffect();

        if (effectRoot != null)
            effectRoot.gameObject.SetActive(stunned);
    }

    private void Update()
    {
        if (strength > 0.001f || stunned)
        {
            float pulse = _tintPulseSpeed > 0f
                ? 0.75f + 0.25f * Mathf.Sin(Time.time * _tintPulseSpeed * Mathf.PI * 2f)
                : 1f;
            ApplyTint(strength * pulse);
        }

        UpdateShake();
        UpdateHeadEffect();
    }

    private void ApplyTint(float amount)
    {
        if (tint == null)
            return;

        tint.SetChannel(TintChannel.Stun, _tintColor, _tintStrength * amount);
    }

    private void UpdateShake()
    {
        // 先還原上一幀加的偏移，再依目前強度加新的，這樣敵人被其他動畫移動時也不會累積誤差
        transform.position -= appliedShake;
        appliedShake = Vector3.zero;
        tint.ExternalOffset = Vector3.zero;

        if (strength <= 0.001f)
            return;

        tint.EnsureCache();
        if (!tint.BoundsReady)
            return;

        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;

        float size = tint.ModelBounds.size.magnitude * _shakeAmount * strength;
        float t = Time.time * _shakeSpeed;
        appliedShake = right * (Mathf.Sin(t) * size) + up * (Mathf.Sin(t * 1.7f + 1f) * size * 0.5f);
        transform.position += appliedShake;
        tint.ExternalOffset = appliedShake;
    }

    // ---- 頭上特效 ----

    private void BuildHeadEffect()
    {
        tint.EnsureCache();

        GameObject root;
        if (_customEffectPrefab != null)
        {
            root = Instantiate(_customEffectPrefab);
            stars = null;
        }
        else
        {
            root = new GameObject("StunStars");
            stars = new Transform[_starCount];
            Sprite sprite = GetStarSprite();
            Bounds bounds = tint.ModelBounds;
            starRadius = tint.BoundsReady ? Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.7f : 0.5f;
            float starSize = tint.BoundsReady ? bounds.size.y * 0.12f : 0.2f;

            for (int i = 0; i < _starCount; i++)
            {
                GameObject star = new GameObject("Star" + i);
                star.transform.SetParent(root.transform, false);
                SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = _starColor;
                sr.sortingOrder = 100;
                star.transform.localScale = Vector3.one * starSize / Mathf.Max(0.0001f, sprite.bounds.size.x);
                stars[i] = star.transform;
            }
        }

        effectRoot = root.transform;
        root.SetActive(false);
    }

    private void UpdateHeadEffect()
    {
        if (effectRoot == null || !effectRoot.gameObject.activeSelf)
            return;

        float height = tint.BoundsReady ? tint.ModelBounds.size.y : 1f;
        effectRoot.position = tint.HeadPosition + Vector3.up * (height * _headOffset);

        if (stars == null)
            return;

        orbitAngle += _orbitDegreesPerSecond * Time.deltaTime;
        Camera cam = Camera.main;

        for (int i = 0; i < stars.Length; i++)
        {
            float angle = (orbitAngle + 360f * i / stars.Length) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * starRadius, Mathf.Sin(angle * 2f) * starRadius * 0.08f, Mathf.Sin(angle) * starRadius * 0.45f);
            stars[i].position = effectRoot.position + (cam != null ? cam.transform.TransformDirection(offset) : offset);
            if (cam != null)
                stars[i].rotation = cam.transform.rotation;
        }
    }

    private static Sprite GetStarSprite()
    {
        if (starSprite != null)
            return starSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "StunStarRuntime"
        };

        // 五角星：用「點在多邊形內」逐像素畫
        Vector2[] polygon = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float radius = (i % 2 == 0) ? 0.48f : 0.2f;
            float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            polygon[i] = new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius);
        }

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                pixels[y * size + x] = PointInPolygon(p, polygon) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        starSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return starSprite;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                inside = !inside;
        }

        return inside;
    }
}
