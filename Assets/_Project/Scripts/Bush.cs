using System.Collections.Generic;
using UnityEngine;

// Arbusto: quien está dentro queda escondido (los bots no lo ven y el jugador no ve a los enemigos dentro).
[RequireComponent(typeof(Collider))]
public class Bush : MonoBehaviour
{
    static readonly List<Bush> all = new List<Bush>();

    Collider area;

    void Awake()
    {
        area = GetComponent<Collider>();
        area.isTrigger = true; // Se puede caminar a través
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public static bool IsInsideAnyBush(Vector3 position)
    {
        foreach (var bush in all)
        {
            Bounds b = bush.area.bounds;
            position.y = b.center.y;
            if (b.Contains(position)) return true;
        }
        return false;
    }
}
