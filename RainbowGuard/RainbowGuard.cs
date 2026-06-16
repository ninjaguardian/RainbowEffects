using System;
using MelonLoader;
using MelonLoader.Utils;
using RainbowGuard;
using System.IO;
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
        public const string ModVersion = "2.0.0";
        /// <summary>
        /// MelonLoader Version.
        /// </summary>
        public const string MLVersion = "0.7.3";
    }
    #endregion

    /// <summary>
    /// The main class.
    /// </summary>
    public class RainbowGuard : MelonMod
    {
        private static readonly int ShaderID = Shader.PropertyToID("rainbowGuard");
        private const float Phase120 = (float)(2d * Math.PI / 3d);
        private const float Phase240 = (float)(4d * Math.PI / 3d);

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            Shader rainbowGuard = RumbleModdingAPI.RMAPI.AssetBundles.LoadAssetFromFile<Shader>(
                Path.Combine(MelonEnvironment.UserDataDirectory, RainbowGuardModInfo.ModName, "rainbowGuard"),
                "guard.shader"
            );
            rainbowGuard.hideFlags = HideFlags.HideAndDontSave;

            foreach (Material mat in Resources.FindObjectsOfTypeAll<Material>())
                if (mat?.name == "Hidden/VFX/Guardstone VFX/System/Output Particle Shader Graph Quad - Unlit")
                    mat.shader = rainbowGuard;
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            Shader.SetGlobalVector(ShaderID, new Vector4(
                Mathf.Sin(Time.time) * 0.5f + 0.5f,
                Mathf.Sin(Time.time + Phase120) * 0.5f + 0.5f,
                Mathf.Sin(Time.time + Phase240) * 0.5f + 0.5f,
                0
            ));
        }
    }
}
