using System.Collections.Generic;
using UnityEngine;

// Dibujos pequeños en pixel art hechos desde código (efectos y candado).
// Cada letra es un píxel; el punto es transparente.
public static class PixelSprites
{
    static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'W', new Color32(255, 255, 255, 255) },
        { 'Y', new Color32(255, 220, 70, 255) },
        { 'O', new Color32(242, 140, 38, 255) },
        { 'T', new Color32(196, 164, 118, 255) }, // Tierra
        { 't', new Color32(150, 120, 86, 255) },
        { 'C', new Color32(190, 240, 255, 255) }, // Agua
        { 'c', new Color32(80, 170, 230, 255) },
        { 'G', new Color32(170, 176, 188, 255) }, // Metal
        { 'k', new Color32(40, 34, 30, 255) },
    };

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Polvo => Get("polvo",
        "..TT..",
        ".TTTT.",
        "TTTTtT",
        "TTTttt",
        ".tttt.",
        "..tt..");

    public static Sprite Gota => Get("gota",
        "..C..",
        ".CCc.",
        "CCccc",
        "Ccccc",
        ".ccc.");

    public static Sprite Onda => Get("onda",
        "....CCCC....",
        "..CC....CC..",
        ".C........C.",
        "C..........C",
        "C..........C",
        ".C........C.",
        "..CC....CC..",
        "....CCCC....");

    public static Sprite Destello => Get("destello",
        "....W....",
        ".O..W..O.",
        "..O.Y.O..",
        "...YWY...",
        "WWYWWWYWW",
        "...YWY...",
        "..O.Y.O..",
        ".O..W..O.",
        "....W....");

    public static Sprite Chispa => Get("chispa",
        ".Y.",
        "YWY",
        ".Y.");

    public static Sprite Candado => Get("candado",
        "...kkkk...",
        "..kGGGGk..",
        ".kGk..kGk.",
        ".kGk..kGk.",
        "kkkkkkkkkk",
        "kYYYYYYYYk",
        "kYYYkkYYYk",
        "kOOOkkOOOk",
        "kOOOOOOOOk",
        "kOOOOOOOOk",
        "kkkkkkkkkk");

    // La primera fila es la de arriba
    static Sprite Get(string name, params string[] rows)
    {
        if (cache.TryGetValue(name, out Sprite s) && s != null) return s;

        int w = rows[0].Length, h = rows.Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, Palette.TryGetValue(rows[y][x], out Color32 c) ? c : new Color32(0, 0, 0, 0));
        tex.Apply();

        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        s.name = name;
        cache[name] = s;
        return s;
    }
}
