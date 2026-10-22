using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Estados de una unidad: atrapado en la red, jalado por un garfio, en fase (invisible e
// intocable, como Puck) o escondido en un arbusto.
public class StatusEffects : MonoBehaviour
{
    [SerializeField] GameObject rootVisual; // Opcional: la red que aparece encima cuando está atrapado

    public bool IsRooted => Time.time < rootedUntil;
    public bool IsPhased => Time.time < phasedUntil;
    public bool IsPulled { get; private set; }
    public bool IsHidden { get; private set; }

    public bool IsTargetable => !IsPhased;                        // En fase, el garfio y la red lo atraviesan
    public bool CanMove => !IsPulled && !IsRooted && !IsPhased;
    public bool CanAct => !IsPulled && !IsRooted;                  // Atrapado en la red no puede hacer nada

    // Poderes de las runas
    public bool HasHaste => Time.time < hasteUntil;
    public bool HasDoubleDamage => Time.time < doubleDamageUntil;
    public float SpeedMultiplier => HasHaste ? 1.5f : 1f;
    public float DamageMultiplier => HasDoubleDamage ? 2f : 1f;

    float rootedUntil;
    float phasedUntil;
    float hasteUntil;
    float doubleDamageUntil;
    CharacterController cc;
    NavMeshAgent agent;
    Unit unit;
    readonly List<Renderer> renderers = new List<Renderer>();
    Canvas[] canvases;
    bool invisible;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        unit = GetComponent<Unit>();

        // Solo guardamos los renderers que ya estaban encendidos
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.enabled && (rootVisual == null || !r.transform.IsChildOf(rootVisual.transform)))
                renderers.Add(r);
        canvases = GetComponentsInChildren<Canvas>(true);

        if (rootVisual != null) rootVisual.SetActive(false);
    }

    void Update()
    {
        IsHidden = Bush.IsInsideAnyBush(transform.position);

        if (rootVisual != null && rootVisual.activeSelf != IsRooted)
            rootVisual.SetActive(IsRooted);

        UpdateVisibility();
    }

    // En fase desaparece para todos; en un arbusto, el enemigo desaparece para el jugador
    void UpdateVisibility()
    {
        Unit player = Unit.FindPlayer();
        bool hiddenInBush = IsHidden && unit != null && player != null && unit.IsEnemyOf(player);
        bool hide = IsPhased || hiddenInBush;
        if (hide == invisible) return;

        invisible = hide;
        foreach (var r in renderers) if (r != null) r.enabled = !hide;
        foreach (var c in canvases) if (c != null) c.enabled = !hide;
    }

    public void Root(float seconds)
    {
        rootedUntil = Mathf.Max(rootedUntil, Time.time + seconds);
        phasedUntil = 0f;
    }

    public void AddHaste(float seconds) { hasteUntil = Mathf.Max(hasteUntil, Time.time + seconds); }
    public void AddDoubleDamage(float seconds) { doubleDamageUntil = Mathf.Max(doubleDamageUntil, Time.time + seconds); }

    public void BeginPhase(float seconds) { phasedUntil = Time.time + seconds; }
    public void EndPhase() { phasedUntil = 0f; }

    public void BeginPull()
    {
        IsPulled = true;
        phasedUntil = 0f;
        if (cc != null) cc.enabled = false;
        if (agent != null) agent.enabled = false;
    }

    public void EndPull()
    {
        if (!IsPulled) return;
        IsPulled = false;

        if (gameObject.activeInHierarchy &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            if (agent != null) transform.position = hit.position;
            else
            {
                Vector3 p = transform.position;
                p.x = hit.position.x;
                p.z = hit.position.z;
                transform.position = p;
            }
        }

        if (cc != null) cc.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            if (gameObject.activeInHierarchy) agent.Warp(transform.position);
        }
    }

    public void ClearAll()
    {
        rootedUntil = 0f;
        phasedUntil = 0f;
        hasteUntil = 0f;
        doubleDamageUntil = 0f;
        if (IsPulled) EndPull();
        IsHidden = false;
    }
}
