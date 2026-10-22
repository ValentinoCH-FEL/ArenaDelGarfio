using System.Collections.Generic;
using UnityEngine;

// Runa que flota en el río. Hay que engancharla con el garfio para traerla y recibir su poder.
public enum RuneType { Velocidad, DobleDanio, Oro, Curacion }

[RequireComponent(typeof(Collider))]
public class Rune : MonoBehaviour
{
    public static readonly List<Rune> Active = new List<Rune>();
    public static readonly string[] Names = { "Velocidad", "Doble daño", "Oro", "Curación" };
    static readonly Color[] Colors =
    {
        new Color(0.2f, 0.9f, 1f),   // Velocidad
        new Color(1f, 0.25f, 0.25f), // Doble daño
        new Color(1f, 0.85f, 0.2f),  // Oro
        new Color(0.3f, 1f, 0.4f),   // Curación
    };

    [SerializeField] Renderer visual;
    public float hasteSeconds = 8f;
    public float doubleDamageSeconds = 10f;
    public int gold = 150;

    public RuneType Type { get; private set; }
    public bool IsGrabbed { get; private set; }
    public string DisplayName => Names[(int)Type];

    Vector3 basePosition;

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    public void Setup(RuneType type)
    {
        Type = type;
        basePosition = transform.position;
        if (visual != null) visual.material.SetColor("_BaseColor", Colors[(int)type]);
    }

    void Update()
    {
        if (IsGrabbed) return;
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.2f;
    }

    public void Grab()
    {
        IsGrabbed = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void ApplyTo(Unit unit)
    {
        switch (Type)
        {
            case RuneType.Velocidad: unit.Status.AddHaste(hasteSeconds); break;
            case RuneType.DobleDanio: unit.Status.AddDoubleDamage(doubleDamageSeconds); break;
            case RuneType.Oro: if (unit.Wallet != null) unit.Wallet.Add(gold); break;
            case RuneType.Curacion: unit.Health.Heal(unit.Health.Max); break;
        }
        AudioManager.Play(Sfx.Runa);
        RuneSpawner.NotifyTaken(unit, this);
        Destroy(gameObject);
    }
}
