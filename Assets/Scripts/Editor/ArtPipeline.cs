using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ErvasDoBrasil.EditorTools
{
    /// <summary>
    /// Pipeline de arte: tudo que cair em Assets/Art vira Sprite com as configurações certas
    /// e entra automaticamente na ArtLibrary (lida em runtime pelo nome do arquivo).
    /// Arquivos que começam com "_" (folhas de referência) são ignorados.
    /// </summary>
    public class ArtImporter : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.filterMode = Path.GetFileName(assetPath).Contains("_px") ? FilterMode.Point : FilterMode.Bilinear;

            // pivô no pé para personagens e plantas (ordenação top-down fica natural)
            var name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (name.StartsWith("player_", StringComparison.Ordinal) || name.StartsWith("plant_", StringComparison.Ordinal) ||
                name.StartsWith("npc_", StringComparison.Ordinal) || name.StartsWith("prop_", StringComparison.Ordinal))
            {
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                ti.SetTextureSettings(settings);
            }
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Any(p => p.StartsWith(ArtRoot, StringComparison.Ordinal)))
                EditorApplication.delayCall += ArtLibraryBuilder.Rebuild;
        }
    }

    /// <summary>Músicas de Assets/Audio/Resources/Music tocam em streaming (economiza memória no iPhone).</summary>
    public class MusicImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/Music/")) return;
            var ai = (AudioImporter)assetImporter;
            var settings = ai.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            ai.defaultSampleSettings = settings;
        }
    }

    [InitializeOnLoad]
    public static class ArtLibraryBuilder
    {
        public const string LibraryPath = "Assets/ScriptableObjects/Resources/ArtLibrary.asset";

        static readonly string[] ArtFolders = { "Assets/Art" };

        // o construtor estático roda no meio do carregamento do editor: o AssetDatabase só é usado depois, no delayCall
        static ArtLibraryBuilder() { EditorApplication.delayCall += RebuildIfMissing; }

        static void RebuildIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibraryPath) == null) Rebuild();
        }

        [MenuItem("Ervas do Brasil/Atualizar biblioteca de arte", priority = 1)]
        public static void Rebuild()
        {
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibraryPath);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
                lib = ScriptableObject.CreateInstance<ArtLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var entries = new List<ArtLibrary.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", ArtFolders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (key.StartsWith('_')) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null && entries.All(e => e.key != key)) entries.Add(new ArtLibrary.Entry { key = key, sprite = sprite });
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

            lib.sprites = entries;
            lib.ResetCache();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ervas do Brasil] ArtLibrary atualizada: " + entries.Count + " sprites.");
        }
    }
}
