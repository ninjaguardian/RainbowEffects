using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UIFramework;
using UIFramework.UiExtensions;
using UnityEngine;
using BuildInfo = RainbowEffects.BuildInfo;

#region Assemblies
[assembly: MelonInfo(typeof(RainbowEffects.RainbowEffects), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(BuildInfo.MLVersion, true)]
[assembly: MelonOptionalDependencies(UIFramework.BuildInfo.Name)]
#endregion

namespace RainbowEffects
{
    #region RainbowEffectsModInfo
    /// <summary>
    /// Contains mod info
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>
        /// Mod name
        /// </summary>
        public const string ModName = "RainbowEffects";
        /// <summary>
        /// Mod version
        /// </summary>
        public const string ModVersion = "1.0.0";
        /// <summary>
        /// MelonLoader Version
        /// </summary>
        public const string MLVersion = "0.7.2";
        /// <summary>
        /// Mod author
        /// </summary>
        public const string Author = "ninjaguardian";
        /// <summary>
        /// Mod download link
        /// </summary>
        public const string DownloadLink = "https://thunderstore.io/c/rumble/p/ninjaguardian/RainbowEffects";
        /// <summary>
        /// Config file name
        /// </summary>
        public const string ConfigFile = "config.cfg";
    }
    #endregion

    /// <summary>
    /// The main class
    /// </summary>
    public class RainbowEffects : MelonMod
    {
        private const float Phase120 = (float)(2d * Math.PI / 3d);
        private const float Phase240 = (float)(4d * Math.PI / 3d);

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            // TODO: maybe make the PS find the cb instead of using cb0 (like the VS)
            AssetBundle assetBundle = RumbleModdingAPI.RMAPI.AssetBundles.LoadAssetBundleFromStream(
                this,
                "RainbowEffects.rainbowGuard"
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
            if (_mainEffect!.Mode.Value == ColorMode.Rainbow)
            {
                float t = Time.time * _mainEffect.Speed.Value + _mainEffect.Offset.Value;
                _mainEffect.SetShader(new Vector4(
                    Mathf.Sin(t) * 0.5f + 0.5f,
                    Mathf.Sin(t + Phase120) * 0.5f + 0.5f,
                    Mathf.Sin(t + Phase240) * 0.5f + 0.5f,
                    0
                ));
            }

            if (_particles!.Mode.Value == ColorMode.Rainbow)
            {
                float t = Time.time * _particles.Speed.Value + _particles.Offset.Value;
                _particles.SetShader(new Vector4(
                    Mathf.Sin(t) * 0.5f + 0.5f,
                    Mathf.Sin(t + Phase120) * 0.5f + 0.5f,
                    Mathf.Sin(t + Phase240) * 0.5f + 0.5f,
                    0
                ));
            }
        }

        private static readonly string ConfigDir = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.ModName);
        private static readonly string Config = Path.Combine(ConfigDir, BuildInfo.ConfigFile);

        private Effect? _mainEffect;
        private Effect? _particles;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            bool uiPresent = RegisteredMelons.Any(m => m.Info.Name == UIFramework.BuildInfo.Name);
            Effect.UIPresent = uiPresent;

            if (!Directory.Exists(ConfigDir))
                Directory.CreateDirectory(ConfigDir);

            _mainEffect = new Effect("Main Effect", "rainbowGuard");
            _particles = new Effect("Particles", "rainbowGuard2");

            if (uiPresent)
                RegisterUI();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterUI()
            => UI.RegisterMelon(this, _mainEffect!.Category, _particles!.Category);

        internal static MelonPreferences_Category CreateCategory(string categoryID, string categoryName)
        {
            MelonPreferences_Category cat = MelonPreferences.CreateCategory(BuildInfo.ModName + '_' + categoryID, categoryName);
            cat.SetFilePath(Config);
            return cat;
        }

        internal static MelonPreferences_Entry<T> CreateEntry<T>(MelonPreferences_Category category, string entryID, T defaultValue, string entryName, string description, ValueValidator? validator = null)
            => category.CreateEntry(category.Identifier + '_' + entryID, defaultValue, entryName, description, validator: validator);
    }

    internal enum ColorMode
    {
        Rainbow,
        Static
    }

    internal class Effect
    {
        public static bool UIPresent;
        private readonly int _shaderID;
        internal readonly MelonPreferences_Category Category;
        internal readonly MelonPreferences_Entry<float> Speed;
        internal readonly MelonPreferences_Entry<float> Offset;
        internal readonly MelonPreferences_Entry<Vector3> Color;
        internal readonly MelonPreferences_Entry<ColorMode> Mode;

        internal Effect(string name, string property)
        {
            // TODO: fix discard issues
            _shaderID = Shader.PropertyToID(property);

            string lowName = name.ToLower();

            Category = RainbowEffects.CreateCategory(name.Replace(" ", null), name);

            Speed = RainbowEffects.CreateEntry(Category, nameof(Speed), 1f, "Speed", "Speed of the rainbow");
            Offset = RainbowEffects.CreateEntry(Category, nameof(Offset), 0f, "Offset", "Offset of the rainbow");

            Color = RainbowEffects.CreateEntry(Category, nameof(Color), new Vector3(1f, 0.205078766f, 0f), "Color", "The color of the " + lowName, new VectorRange(0f, 1f));
            Color.OnEntryValueChanged.Subscribe((_, newColor) =>
                SetShader(newColor)
            );

            Mode = RainbowEffects.CreateEntry(Category, nameof(Mode), ColorMode.Rainbow, "Mode", "The color mode of the " + lowName, UIPresent ? Ui() : null);
            Mode.OnEntryValueChanged.Subscribe((_, newMode) => {
                if (newMode == ColorMode.Static)
                    SetShader(Color.Value);
            });
            ModeToggled(Mode.Value);

            if (Mode.Value == ColorMode.Static)
                SetShader(Color.Value);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ValueValidator Ui()
            => new UserEditNotifier { OnUserEdit = ModeToggled };

        internal void SetShader(Vector4 v)
            => Shader.SetGlobalVector(_shaderID, v);

        private void ModeToggled(object newValue)
        {
            switch (newValue)
            {
                case ColorMode.Rainbow:
                    Speed.IsHidden = false;
                    Offset.IsHidden = false;

                    Color.IsHidden = true;
                    break;
                case ColorMode.Static:
                    Speed.IsHidden = true;
                    Offset.IsHidden = true;

                    Color.IsHidden = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, "Value not present in enum " + nameof(ColorMode));
            }
        }
    }

    internal class VectorRange : ValueValidator
    {
        private readonly float _minValue;
        private readonly float _maxValue;

        public VectorRange(float minValue, float maxValue)
        {
            if (maxValue < minValue)
                throw new ArgumentException($"Min value ({minValue}) must be less than or equal to max value ({maxValue})!");

            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override bool IsValid(object value)
        {
            return value switch
            {
                Vector4 v => IsValid(v.x) && IsValid(v.y) && IsValid(v.z) && IsValid(v.w),
                Vector3 v => IsValid(v.x) && IsValid(v.y) && IsValid(v.z),
                Vector2 v => IsValid(v.x) && IsValid(v.y),
                _ => throw new ArgumentException($"Unsupported type: {value.GetType()}")
            };
        }

        private bool IsValid(float value)
            => value >= _minValue && value <= _maxValue;

        private float EnsureValid(float value)
            => value < _minValue
                ? _minValue
                : value > _maxValue
                    ? _maxValue
                    : value;

        public override object EnsureValid(object value)
        {
            switch (value)
            {
                case Vector4 v:
                    v.x = EnsureValid(v.x);
                    v.y = EnsureValid(v.y);
                    v.z = EnsureValid(v.z);
                    v.w = EnsureValid(v.w);
                    return v;
                case Vector3 v:
                    v.x = EnsureValid(v.x);
                    v.y = EnsureValid(v.y);
                    v.z = EnsureValid(v.z);
                    return v;
                case Vector2 v:
                    v.x = EnsureValid(v.x);
                    v.y = EnsureValid(v.y);
                    return v;
                default:
                    throw new ArgumentException($"Unsupported type: {value.GetType()}");
            }
        }
    }
}
