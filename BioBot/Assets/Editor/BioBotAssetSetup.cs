#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BioBot
{
    public sealed class BioBotAssetSetup : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Assets/Campaign/Resources/BioBot/"))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            bool painted = assetPath.EndsWith("/EstuaryDusk.png") || assetPath.EndsWith("/PatrolSubmersible.png")
                || assetPath.EndsWith("/StarterGuardians.png") || assetPath.EndsWith("/CoastalHostiles.png");
            importer.filterMode = painted || assetPath.EndsWith("/TidalSilhouettes.png")
                ? FilterMode.Bilinear : FilterMode.Point;
            if (assetPath.EndsWith("/TidalRoster.png")) importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
        }
    }
}
#endif
