using UnityEngine;

// Dibuja a un personaje con su sprite en pixel art (en vez del modelo 3D) y lo anima:
// quieto, caminar, lanzar y golpeado. Se agrega solo desde Unit.
// Hojas en Resources/PixelArt/Personajes/<tipo>_<equipo>.png con 8 cuadros de 32x32:
//   0-1 quieto · 2-5 caminar · 6 lanzar · 7 golpeado
public class SpriteCharacter : MonoBehaviour
{
    public static readonly string[] Kinds = { "equilibrado", "veloz", "tanque" };

    const int FrameIdle = 0, FrameWalk = 2, FrameThrow = 6, FrameHurt = 7;

    Unit unit;
    Health health;
    HookAbility hook;
    NetAbility net;
    SpriteRenderer body;
    Sprite[] frames;
    float animTime, throwUntil, hurtUntil, lastHealth;
    bool facingLeft;

    public int Kind { get; private set; }

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        hook = GetComponent<HookAbility>();
        net = GetComponent<NetAbility>();

        HideModels();
        body = PixelArt.CreateFlat(transform, "Sprite2D", null, 1000);
        body.transform.localScale = Vector3.one * 1.15f;
        SetKind(0);
    }

    // 0 = Equilibrado (guerrero), 1 = Veloz (chasqui), 2 = Tanque (orejón)
    public void SetKind(int kind)
    {
        Kind = Mathf.Clamp(kind, 0, Kinds.Length - 1);
        string team = unit != null && unit.team == TeamId.Rojo ? "rojo" : "azul";
        frames = PixelArt.Frames("Personajes/" + Kinds[Kind] + "_" + team, 32);
        if (unit != null && unit.Status != null) unit.Status.Refresh();
    }

    // Apaga los modelos 3D; deja el anillo, el escudo y la barra de vida.
    void HideModels()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            string n = r.gameObject.name;
            if (n == "Anillo" || n == "EscudoVisual") continue;
            if (r.GetComponentInParent<Canvas>() != null) continue;

            r.enabled = false;

            // La red que aparece al quedar atrapado se cambia por su dibujo
            Transform netRoot = FindNetRoot(r.transform);
            if (netRoot != null && netRoot.Find("Red2D") == null)
            {
                var netSprite = PixelArt.CreateFlat(netRoot, "Red2D", PixelArt.Single("Objetos/red"), 2600);
                netSprite.transform.SetPositionAndRotation(transform.position + new Vector3(0f, -0.85f, 0.45f), Quaternion.Euler(90f, 0f, 0f));
                float s = netRoot.lossyScale.x > 0.001f ? 1.4f / netRoot.lossyScale.x : 1.4f;
                netSprite.transform.localScale = Vector3.one * s;
            }
        }
    }

    Transform FindNetRoot(Transform t)
    {
        for (; t != null && t != transform; t = t.parent)
            if (t.name.Contains("RedAtrapado")) return t;
        return null;
    }

    void OnEnable()
    {
        throwUntil = hurtUntil = 0f;
        if (hook != null) hook.OnFired += OnThrow;
        if (net != null) net.OnFired += OnThrow;
        if (health != null)
        {
            lastHealth = health.Current;
            health.OnChanged += OnHealthChanged;
        }
    }

    void OnDisable()
    {
        if (hook != null) hook.OnFired -= OnThrow;
        if (net != null) net.OnFired -= OnThrow;
        if (health != null) health.OnChanged -= OnHealthChanged;
    }

    void OnThrow() { throwUntil = Time.time + 0.3f; }

    void OnHealthChanged(float current, float max)
    {
        if (current < lastHealth - 0.01f) hurtUntil = Time.time + 0.15f;
        lastHealth = current;
    }

    void LateUpdate()
    {
        if (body == null || frames == null || frames.Length < 8) return;

        Vector3 v = unit != null ? unit.Velocity : Vector3.zero;
        v.y = 0f;
        float speed = v.magnitude;

        // Mira hacia donde apunta (el personaje gira hacia el mouse o el objetivo)
        Vector3 f = transform.forward;
        if (Mathf.Abs(f.x) > 0.15f) facingLeft = f.x < 0f;

        bool canWalk = unit == null || (!unit.Status.IsPulled && unit.Status.CanMove);
        int frame;
        if (Time.time < hurtUntil) frame = FrameHurt;
        else if (Time.time < throwUntil) frame = FrameThrow;
        else if (speed > 0.4f && canWalk)
        {
            animTime += Time.deltaTime * Mathf.Lerp(7f, 12f, speed / 8f);
            frame = FrameWalk + (int)animTime % 4;
        }
        else
        {
            animTime += Time.deltaTime * 2f;
            frame = FrameIdle + (int)animTime % 2;
        }

        body.sprite = frames[frame];
        body.flipX = facingLeft;

        // Acostado en el suelo, con los pies un poco por debajo del centro del personaje
        Vector3 p = transform.position;
        body.transform.SetPositionAndRotation(new Vector3(p.x, p.y - 0.9f, p.z - 0.35f), Quaternion.Euler(90f, 0f, 0f));
        body.sortingOrder = PixelArt.DepthOrder(p.z - 0.35f);
    }
}
