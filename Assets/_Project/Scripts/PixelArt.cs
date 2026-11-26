using System.Collections.Generic;
using UnityEngine;

// Carga los dibujos en pixel art desde Resources/PixelArt y los convierte en sprites.
// 16 píxeles = 1 unidad del mundo.
public static class PixelArt
{
    public const float PixelsPerUnit = 16f;

    static readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();
    static Material spriteMaterial;

    // Material para todos los sprites (sin luces, funciona también en la versión web)
    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMaterial == null) spriteMaterial = Resources.Load<Material>("PixelArt/Materiales/PA_Sprite");
            return spriteMaterial;
        }
    }

    // Corta una tira de cuadros (todos del mismo ancho) en sprites con el pivote en los pies.
    public static Sprite[] Frames(string path, int frameWidth, float pivotY = 1.5f / 32f)
    {
        if (frameCache.TryGetValue(path, out Sprite[] cached)) return cached;

        Texture2D tex = Resources.Load<Texture2D>("PixelArt/" + path);
        if (tex == null)
        {
            Debug.LogWarning("No encuentro el dibujo PixelArt/" + path);
            return null;
        }
        tex.filterMode = FilterMode.Point;

        int count = Mathf.Max(1, tex.width / frameWidth);
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * frameWidth, 0, frameWidth, tex.height);
            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, pivotY), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            frames[i].name = path + "_" + i;
        }
        frameCache[path] = frames;
        return frames;
    }

    public static Sprite Single(string path) => Resources.Load<Sprite>("PixelArt/" + path);

    // Crea un sprite acostado en el suelo (la cámara mira desde arriba).
    public static SpriteRenderer CreateFlat(Transform parent, string name, Sprite sprite, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = SpriteMaterial;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    // Orden de dibujo según la profundidad: lo que está más abajo en pantalla se dibuja encima.
    public static int DepthOrder(float worldZ) => 1000 - Mathf.RoundToInt(worldZ * 10f);
}
