using System;
using UnityEngine;

// Hace aparecer una runa al azar en el medio del río cada cierto tiempo (solo una a la vez).
public class RuneSpawner : MonoBehaviour
{
    [SerializeField] Rune runePrefab;
    public float firstDelay = 10f;
    public float interval = 20f;
    public float halfWidth = 16f;  // Qué tan lejos del centro puede aparecer (eje X)
    public float height = 1.3f;

    public static event Action<Rune> OnRuneSpawned;
    public static event Action<Unit, Rune> OnRuneTaken;

    Rune current;
    float nextSpawn;
    bool scheduled;
    bool first = true;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;
        if (current != null || runePrefab == null) return;

        if (!scheduled)
        {
            nextSpawn = Time.time + (first ? firstDelay : interval);
            scheduled = true;
            first = false;
        }

        if (Time.time >= nextSpawn)
        {
            scheduled = false;
            Spawn();
        }
    }

    void Spawn()
    {
        Vector3 p = new Vector3(UnityEngine.Random.Range(-halfWidth, halfWidth), height, 0f);
        current = Instantiate(runePrefab, p, Quaternion.identity);
        current.Setup((RuneType)UnityEngine.Random.Range(0, 4));
        OnRuneSpawned?.Invoke(current);
    }

    public static void NotifyTaken(Unit unit, Rune rune) => OnRuneTaken?.Invoke(unit, rune);
}
