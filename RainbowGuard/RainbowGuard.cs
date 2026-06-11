using MelonLoader;
using MelonLoader.NativeUtils;
using RainbowGuard;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        public const string ModVersion = "1.0.0";
        /// <summary>
        /// MelonLoader Version.
        /// </summary>
        public const string MLVersion = "0.7.3";
        /// <summary>
        /// The native dll used by this mod.
        /// </summary>
        public const string NativeDll = "RainbowGuardCpp.dll";
    }
    #endregion

    /// <summary>
    /// The main class.
    /// </summary>
    public class RainbowGuard : MelonMod
    {
        private bool _didLoad;

        /// <inheritdoc/>
        public override void OnEarlyInitializeMelon() {
            // TODO: what about d3d12?
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            {
                MelonLogger.Error("This mod only works on Direct3D11!");
                return;
            }

            CallbackStore.Init();
            CreatePixelShaderStore.Init();

            _didLoad = true;
        }

        /// <inheritdoc/>
        public override void OnDeinitializeMelon()
        {
            if (!_didLoad)
                return;

            CallbackStore.DeInit();
            CreatePixelShaderStore.DeInit();
        }
    }

    internal static class CreatePixelShaderStore
    {
        [DllImport(RainbowGuardModInfo.NativeDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetCreatePixelShader();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate int CreatePixelShader(
            IntPtr device,
            void* shaderBytecode,
            nuint bytecodeLength,
            IntPtr classLinkage,
            IntPtr* pixelShader
        );
        private static NativeHook<CreatePixelShader>? _hookInstance;

        internal static void Init()
        {
            if (_hookInstance != null)
                return;

            IntPtr createPixelShader = GetCreatePixelShader();
            if (createPixelShader == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get CreatePixelShader.");
                return;
            }

            IntPtr detour;
            unsafe
            {
                detour = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, void*, nuint, IntPtr, IntPtr*, int>)&Hook;
            }
            _hookInstance = new NativeHook<CreatePixelShader>(createPixelShader, detour);
            _hookInstance.Attach();
        }

        internal static void DeInit()
        {
            _hookInstance?.Detach();
            _hookInstance = null;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Hook(
            IntPtr device,
            void* shaderBytecode,
            nuint bytecodeLength,
            IntPtr classLinkage,
            IntPtr* pixelShader
        )
        {
            if (shaderBytecode != null && bytecodeLength > 0)
            {
                // TODO: get faster hashing system
                ReadOnlySpan<byte> span = new(shaderBytecode, (int)bytecodeLength);

                byte[] hash = SHA256.HashData(span);

                MelonLogger.Msg($"Shader hash: {Convert.ToHexString(hash)}");
            }

            return _hookInstance!.Trampoline(
                device,
                shaderBytecode,
                bytecodeLength,
                classLinkage,
                pixelShader
            );
        }
    }

    internal static class CallbackStore
    {
        [DllImport(RainbowGuardModInfo.NativeDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetLogCallback(LogCallback? msg, LogCallback? warn, LogCallback? err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

        private static readonly LogCallback Msg = MelonLogger.Msg;
        private static readonly LogCallback Warn = MelonLogger.Warning;
        private static readonly LogCallback Err = MelonLogger.Error;

        internal static void Init() => SetLogCallback(Msg, Warn, Err);

        internal static void DeInit() => SetLogCallback(null, null, null);
    }
}
