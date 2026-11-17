using UnityEditor;

namespace Obscure.Editor
{
    // VFX sheets in Resources/Fx are sliced at runtime: keep them pixel-exact (no filtering, compression, mips or NPOT scaling).
    public class FxImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Fx/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.filterMode = UnityEngine.FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 4096;
        }
    }
}
