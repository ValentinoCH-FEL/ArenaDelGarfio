using UnityEditor;

// Las imágenes de la portada se importan sin suavizado ni compresión, para que se vean en pixel art.
public class ImportarPortada : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Portada/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
    }
}
