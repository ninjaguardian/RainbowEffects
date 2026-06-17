using MelonLoader;
using RainbowGuard;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

#region Assemblies
[assembly: MelonInfo(typeof(RainbowGuard.RainbowGuard), RainbowGuardModInfo.ModName, RainbowGuardModInfo.ModVersion, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/RainbowGuard")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(RainbowGuardModInfo.MLVersion, true)]
#endregion

namespace RainbowGuard
{
    #region RainbowGuardModInfo
    /// <summary>
    /// Contains mod info.
    /// </summary>
    public static class RainbowGuardModInfo
    {
        /// <summary>
        /// Mod name.
        /// </summary>
        public const string ModName = "RainbowGuard";
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVersion = "3.0.1";
        /// <summary>
        /// MelonLoader Version.
        /// </summary>
        public const string MLVersion = "0.7.2";
    }
    #endregion

    /// <summary>
    /// The main class.
    /// </summary>
    public class RainbowGuard : MelonMod
    {
        private static readonly int ShaderID = Shader.PropertyToID("rainbowGuard");
        private static readonly int Shader2ID = Shader.PropertyToID("rainbowGuard2");
        private const float Phase120 = (float)(2d * Math.PI / 3d);
        private const float Phase240 = (float)(4d * Math.PI / 3d);

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            // TODO: maybe make the PS find the cb instead of using cb0 (like the VS)
            AssetBundle assetBundle = RumbleModdingAPI.RMAPI.AssetBundles.LoadAssetBundleFromStream(
                this,
                "RainbowGuard.rainbowGuard"
            );
            Shader rainbowGuard = assetBundle.LoadAsset<Shader>("guard.shader");
            Shader rainbowGuard2 = assetBundle.LoadAsset<Shader>("guard2.shader");
            assetBundle.Unload(false);

            rainbowGuard.hideFlags = HideFlags.HideAndDontSave;
            rainbowGuard2.hideFlags = HideFlags.HideAndDontSave;

            foreach (Material mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat == null)
                    continue;

                if (mat.name == "Hidden/VFX/Guardstone VFX/System/Output Particle Shader Graph Quad - Unlit")
                    mat.shader = rainbowGuard;
                else if (mat.name == "Hidden/VFX/Guardstone VFX/System (1)/Output Particle Unlit Quad")
                    mat.shader = rainbowGuard2;
            }
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void OnUpdate()
        {
            Vector4 v = new(
                Mathf.Sin(Time.time) * 0.5f + 0.5f,
                Mathf.Sin(Time.time + Phase120) * 0.5f + 0.5f,
                Mathf.Sin(Time.time + Phase240) * 0.5f + 0.5f,
                0
            );
            Shader.SetGlobalVector(ShaderID, v);
            Shader.SetGlobalVector(Shader2ID, v);
        }
    }
}
