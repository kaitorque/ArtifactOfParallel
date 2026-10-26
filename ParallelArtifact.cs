using System.IO;
using System.Reflection;
using R2API;
using RoR2;
using UnityEngine;

namespace ArtifactOfParallel
{
    internal static class ParallelArtifact
    {
        private const string NameToken = "KAITORQUE_ARTIFACT_PARALLEL_NAME";
        private const string DescriptionToken = "KAITORQUE_ARTIFACT_PARALLEL_DESC";

        public static ArtifactDef Def { get; private set; }

        public static bool Enabled => Def && RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Def);

        public static void Register()
        {
            LanguageAPI.Add(NameToken, "Artifact of Parallel");
            LanguageAPI.Add(DescriptionToken,
                "Chests, shrines and shops are split between players. You only see your own; anything that drops is shared. Total loot stays the same.");

            Def = ScriptableObject.CreateInstance<ArtifactDef>();
            Def.cachedName = "KaitorqueParallel";
            Def.nameToken = NameToken;
            Def.descriptionToken = DescriptionToken;
#pragma warning disable CS0618
            Def.smallIconSelectedSprite = LoadSprite("texArtifactParallelEnabled.png");
            Def.smallIconDeselectedSprite = LoadSprite("texArtifactParallelDisabled.png");
#pragma warning restore CS0618

            if (!ContentAddition.AddArtifactDef(Def))
            {
                ArtifactOfParallelPlugin.Log.LogError("Failed to register the artifact.");
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArtifactOfParallel." + fileName);
            if (stream == null)
            {
                ArtifactOfParallelPlugin.Log.LogError($"Missing embedded icon {fileName}.");
                return null;
            }

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadImage(bytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
