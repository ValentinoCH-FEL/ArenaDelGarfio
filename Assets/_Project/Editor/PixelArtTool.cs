using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramienta del editor que pasa el juego a pixel art (vista 2D desde arriba).
// Menú: Arena del Garfio > ...
//  - "Ajustar dibujos": configura la importación de los PNG de Resources/PixelArt (sin suavizado, 16 px = 1 unidad).
//  - "Convertir arenas y prefabs": cambia suelos, río, muros y objetos de las 3 arenas por pixel art.
// Si editas un PNG (por ejemplo en Piskel o Aseprite) y lo guardas con el mismo nombre, el juego lo usa solo.
public static class PixelArtTool
{
    const string Root = "Assets/_Project";
    const string ArtDir = Root + "/Resources/PixelArt";
    const string MatDir = ArtDir + "/Materiales";

    static readonly Dictionary<string, string> SceneThemes = new Dictionary<string, string>
    {
        { Root + "/Scenes/Arena01.unity", "pradera" },   // Nivel 1
        { Root + "/Scenes/Arena02.unity", "canon" },     // Nivel 2
        { Root + "/Scenes/Arena03.unity", "puna" },      // Nivel 3
    };

    [MenuItem("Arena del Garfio/Ajustar dibujos (pixel art)")]
    public static void ConfigureTextures()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            string file = Path.GetFileNameWithoutExtension(path);
            bool isProp = path.Contains("/Objetos/") && file != "soga";
            bool repeat = path.Contains("/Texturas/") || file == "soga";

            imp.textureType = isProp ? TextureImporterType.Sprite : TextureImporterType.Default;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.alphaIsTransparency = true;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            if (isProp)
            {
                imp.spriteImportMode = SpriteImportMode.Single;
                var s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                bool tall = file.StartsWith("queuna") || file == "cactus";
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = tall ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.5f);
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
            }
            imp.SaveAndReimport();
        }
        Debug.Log("PIXELART_TEXTURAS_OK");
    }

    [MenuItem("Arena del Garfio/Convertir arenas y prefabs a pixel art")]
    public static void ConvertAll()
    {
        ConfigureTextures();
        EnsureFolder(MatDir);
        EnsureFolder(MatDir + "/Escenas");
        SpriteMat();
        ConvertProjectMaterials();
        ConvertPrefabs();
        foreach (var kv in SceneThemes) ConvertScene(kv.Key, kv.Value);
        AssetDatabase.SaveAssets();
        Debug.Log("PIXELART_TODO_OK");
    }

    // ---------------------------------------------------------------------------------------------
    // Materiales
    // ---------------------------------------------------------------------------------------------

    static Shader UnlitShader => Shader.Find("Universal Render Pipeline/Unlit");

    static Material SpriteMat()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return GetOrCreateMat(MatDir + "/PA_Sprite.mat", sh, null);
    }

    static Material TileMat(string path, string textureName, Vector2 tiling)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Texturas/" + textureName + ".png");
        if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Objetos/" + textureName + ".png");
        var m = GetOrCreateMat(path, UnlitShader, tex);
        m.mainTextureScale = tiling;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GetOrCreateMat(string path, Shader shader, Texture tex)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        if (tex != null)
        {
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Los materiales con luces (Lit) no se ven en la versión web: se pasan a Unlit con el mismo color.
    static void ConvertProjectMaterials()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null || m.shader.name != "Universal Render Pipeline/Lit") continue;

            Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            bool transparent = m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;

            m.shader = UnlitShader;
            m.SetColor("_BaseColor", color);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(m);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Prefabs: garfio, red, red de atrapado y runa
    // ---------------------------------------------------------------------------------------------

    static void ConvertPrefabs()
    {
        Material soga = TileMat(MatDir + "/PA_Soga.mat", "soga", Vector2.one);

        EditPrefab("Garfio", root =>
        {
            var lr = root.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.sharedMaterial = soga;
                lr.textureMode = LineTextureMode.Tile;
            }
            HideMeshes(root);
            AddFlat(root.transform, "Sprite2D", "garfio", 2500, 1.6f);
        });

        EditPrefab("Red", root =>
        {
            float size = FootprintOf(root, 1.6f);
            HideMeshes(root);
            AddFlat(root.transform, "Sprite2D", "red", 2500, size);
        });

        EditPrefab("RedAtrapado", root =>
        {
            HideMeshes(root);
            AddFlat(root.transform, "Red2D", "red", 2600, 1.4f);
        });

        EditPrefab("Runa", root =>
        {
            HideMeshes(root);
            var sr = AddFlat(root.transform, "Sprite2D", "runa", 2400, 1.3f);
            var rune = root.GetComponent<Rune>();
            if (rune != null && sr != null)
            {
                var so = new SerializedObject(rune);
                so.FindProperty("visual").objectReferenceValue = sr;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        });
    }

    static void EditPrefab(string name, System.Action<GameObject> edit)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        if (!File.Exists(path)) { Debug.LogWarning("No existe " + path); return; }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform old = root.transform.Find("Sprite2D") ?? root.transform.Find("Red2D");
        if (old == null) edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static float FootprintOf(GameObject go, float fallback)
    {
        var rs = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        if (rs.Length == 0) return fallback;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float s = Mathf.Max(b.size.x, b.size.z);
        return s > 0.1f ? s : fallback;
    }

    static void HideMeshes(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = false;
    }

    // Sprite acostado como hijo (su "arriba" apunta hacia adelante del objeto). worldSize = ancho en unidades.
    static SpriteRenderer AddFlat(Transform parent, string name, string spriteName, int order, float worldSize)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/Objetos/" + spriteName + ".png");
        if (sprite == null) { Debug.LogWarning("Falta el sprite " + spriteName); return null; }
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        float parentScale = Mathf.Max(0.001f, parent.lossyScale.x);
        go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x) / parentScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = SpriteMat();
        sr.sortingOrder = order;
        return sr;
    }

    // ---------------------------------------------------------------------------------------------
    // Escenas
    // ---------------------------------------------------------------------------------------------

    static void ConvertScene(string scenePath, string theme)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (GameObject.Find("Sprites2D") != null)
        {
            Debug.Log("PIXELART_ESCENA_YA_CONVERTIDA " + scenePath);
            return;
        }
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        var container = new GameObject("Sprites2D").transform;
        var done = new HashSet<GameObject>();
        int count = 0;

        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var r in renderers)
        {
            if (!r.enabled) continue;
            if (r.GetComponentInParent<Unit>() != null || r.GetComponentInParent<Canvas>() != null) continue;
            if (r.GetComponentInParent<Rune>() != null) continue;

            GameObject go = r.gameObject;
            string n = go.name;
            string matName = r.sharedMaterial != null ? r.sharedMaterial.name : "";
            Bounds b = r.bounds;

            // Superficies que se repiten: suelo, río, muros y bloques de hielo
            if (n == "Suelo")
            {
                r.sharedMaterial = TileMat($"{MatDir}/Escenas/{sceneName}_Suelo.mat", "suelo_" + theme, new Vector2(b.size.x / 2f, b.size.z / 2f));
                continue;
            }
            if (n == "Rio" || matName.Contains("Agua") || matName.Contains("RioHelado"))
            {
                string tex = theme == "puna" ? "rio_helado" : (matName.Contains("Oasis") ? "agua_oasis" : "agua_rio");
                r.sharedMaterial = TileMat($"{MatDir}/Escenas/{sceneName}_{n}_{count++}.mat", tex, new Vector2(b.size.x / 2f, Mathf.Max(1f, b.size.z / 2f)));
                if (go.GetComponent<RiverFlow>() == null)
                {
                    var flow = go.AddComponent<RiverFlow>();
                    if (theme == "puna") flow.speed = new Vector2(0.03f, 0f); // El río helado casi no se mueve
                }
                continue;
            }
            if (n.StartsWith("Pared") || n.Contains("Muro") || matName.Contains("Muro") || matName.Contains("Piedra"))
            {
                r.sharedMaterial = TileMat($"{MatDir}/Escenas/{sceneName}_{n}_{count++}.mat", "muro_" + theme, new Vector2(Mathf.Max(1f, b.size.x / 2f), Mathf.Max(1f, b.size.z / 2f)));
                continue;
            }
            if (n.Contains("BloqueHielo") || matName.Contains("Hielo"))
            {
                r.sharedMaterial = TileMat($"{MatDir}/PA_Hielo.mat", "bloque_hielo", Vector2.one);
                continue;
            }

            // Objetos: se apaga el modelo 3D y se pone su dibujo encima
            Bush bush = go.GetComponentInParent<Bush>();
            GameObject root = bush != null ? bush.gameObject : (PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go);
            if (n == "Roca" || (go.transform.parent != null && go.transform.parent.name == "Roca")) root = n == "Roca" ? go : go.transform.parent.gameObject;
            if (!done.Add(root)) continue;

            ConvertProp(root, bush != null, theme, container);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"PIXELART_ESCENA_OK {scenePath} objetos={done.Count}");
    }

    static void ConvertProp(GameObject root, bool isGameplayBush, string theme, Transform container)
    {
        var rs = root.GetComponentsInChildren<MeshRenderer>().Where(x => x.enabled).ToArray();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var x in rs) b.Encapsulate(x.bounds);
        if (isGameplayBush)
        {
            var col = root.GetComponent<Collider>();
            if (col != null) b = col.bounds;
        }

        string key = (root.name + " " + MeshNameOf(rs[0])).ToLowerInvariant();
        float foot = Mathf.Max(b.size.x, b.size.z);
        string sprite;
        bool tall = false, onTop = false, ground = false;
        float k = 1f;
        string suffix = theme == "canon" ? "_seco" : (theme == "puna" ? "_helado" : "");

        if (isGameplayBush) { sprite = "ichu_grande" + suffix; onTop = true; k = 1.1f; }
        else if (key.Contains("roca") || key.Contains("rock") || key.Contains("stone"))
        {
            sprite = (foot < 1.3f ? "roca_chica_" : (foot > 3f ? "roca_grande_" : "roca_")) + theme;
            k = 1.15f;
            ground = foot < 1.3f;
        }
        else if (key.Contains("palm")) { sprite = "cactus"; tall = true; k = 0.6f; }
        else if (key.Contains("tree")) { sprite = theme == "puna" ? "queuna_nevada" : (theme == "canon" ? "cactus" : "queuna"); tall = true; if (theme == "canon") k = 0.6f; }
        else if (key.Contains("bush")) { sprite = "ichu_grande" + suffix; k = 1.1f; }
        else if (key.Contains("grass")) { sprite = theme == "canon" ? "ichu_seco" : "ichu"; ground = true; k = 1.3f; }
        else if (key.Contains("flower") || key.Contains("mushroom"))
        {
            sprite = key.Contains("yellow") ? "cantuta_amarilla" : "cantuta_roja";
            ground = true;
            k = 1.6f;
        }
        else if (key.Contains("log") || key.Contains("stump")) { sprite = "tronco"; ground = true; }
        else { sprite = "roca_chica_" + theme; ground = true; }

        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/Objetos/" + sprite + ".png");
        if (sp == null) { Debug.LogWarning("Falta el sprite " + sprite); return; }

        foreach (var x in rs) x.enabled = false;

        var go = new GameObject(sprite);
        go.transform.SetParent(container, false);
        Vector3 pos = tall ? new Vector3(b.center.x, 0.06f, b.center.z - foot * 0.25f)
                           : new Vector3(b.center.x, ground ? 0.03f : 0.05f, b.center.z);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
        float scale = Mathf.Clamp(foot * k / sp.bounds.size.x, 0.35f, 5f);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sharedMaterial = SpriteMat();
        if (onTop)
        {
            sr.sortingOrder = 3000;                       // Los arbustos tapan a quien está dentro
            sr.color = new Color(1f, 1f, 1f, 0.88f);
        }
        else sr.sortingOrder = ground ? -200 : PixelArt.DepthOrder(pos.z);
    }

    static string MeshNameOf(MeshRenderer r)
    {
        var mf = r.GetComponent<MeshFilter>();
        return mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "";
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
