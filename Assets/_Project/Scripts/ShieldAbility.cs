using UnityEngine;

// Escudo (tecla F): durante un momento, el garfio enemigo que te toque se refleja:
// vuelve a su dueño, le hace daño y lo deja atrapado. También bloquea la red.
public class ShieldAbility : MonoBehaviour
{
    public float duration = 1.2f;
    public float cooldown = 10f;
    public float reflectDamage = 25f;
    public float reflectRoot = 1.5f;
    public bool usePlayerInput = true; // Apagado en los bots

    public bool IsActive => Time.time < activeUntil;
    public bool IsReady => Time.time >= nextReadyTime && !IsActive;
    public float SecondsLeft => Mathf.Max(0f, nextReadyTime - Time.time);

    float activeUntil, nextReadyTime;
    Unit owner;
    PlayerController player;
    GameObject visual;

    void Awake()
    {
        owner = GetComponent<Unit>();
        if (GetComponent<BotController>() != null) usePlayerInput = false;
        Transform old = transform.Find("EscudoVisual");
        if (old != null) visual = old.gameObject;
        if (visual != null) visual.SetActive(false);
    }

    void OnDisable()
    {
        activeUntil = 0f;
        if (visual != null) visual.SetActive(false);
    }

    void Update()
    {
        UpdateVisual();

        if (!usePlayerInput) return;
        if (player == null) player = GetComponent<PlayerController>();
        if (player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (InputMap.Shield(player.playerIndex)) TryActivate();
    }

    public bool TryActivate()
    {
        if (!IsReady || owner == null) return false;
        if (!owner.Status.CanAct || owner.Status.IsPhased) return false;

        activeUntil = Time.time + duration;
        nextReadyTime = Time.time + cooldown;
        AudioManager.Play(Sfx.Dash);
        return true;
    }

    // Un aro celeste que late alrededor del personaje mientras el escudo está activo
    void UpdateVisual()
    {
        bool show = IsActive;
        if (show && visual == null) CreateVisual();
        if (visual == null) return;

        if (visual.activeSelf != show) visual.SetActive(show);
        if (show)
        {
            float s = 2.4f + Mathf.Sin(Time.time * 18f) * 0.25f;
            visual.transform.localScale = new Vector3(s, 0.05f, s);
        }
    }

    void CreateVisual()
    {
        Transform ring = transform.Find("Anillo");
        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        if (ringRenderer == null) return;

        visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "EscudoVisual";
        DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.92f, 0f);

        var mat = new Material(ringRenderer.sharedMaterial);
        mat.SetColor("_BaseColor", new Color(0.3f, 0.95f, 1f));
        var r = visual.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
