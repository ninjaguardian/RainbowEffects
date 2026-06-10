using MelonLoader;
using MelonLoader.NativeUtils;
using RainbowGuard;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public const string ModVersion = "0.0.1";
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
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PresentDelegate(IntPtr swapChain, uint syncInterval, uint flags);
        private static NativeHook<PresentDelegate>? _hookInstance;

        [DllImport("RainbowGuardCpp.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitMod();

        //[DllImport("RainbowGuardCpp.dll", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int DeInitMod();

        private bool _didInit;

        /// <inheritdoc/>
        public override void OnEarlyInitializeMelon() {
            if (_didInit)
                return;

            // TODO: what about d3d12?
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            {
                MelonLogger.Error("This only works on Direct3D11!");
                return;
            }

            CallbackStore.SetLogCallback(CallbackStore.Msg, CallbackStore.Warn, CallbackStore.Err);

            // TODO: split initmod into multiple
            IntPtr target = InitMod();
            if (target == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to initialize natives.");
                return;
            }

            IntPtr detour;
            unsafe
            {
                detour = (IntPtr)(delegate* unmanaged[Stdcall]
                    <IntPtr, uint, uint, int>)&PresentHook;
            }
            _hookInstance = new NativeHook<PresentDelegate>(target, detour);
            _hookInstance.Attach();

            _didInit = true;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int PresentHook(IntPtr swapChain, uint syncInterval, uint flags)
        {
            MelonLogger.Msg("Call");

            return _hookInstance?.Trampoline(swapChain, syncInterval, flags) ?? 0;
        }

        /// <inheritdoc/>
        public override void OnDeinitializeMelon()
        {
            CallbackStore.SetLogCallback(null, null, null);
            _hookInstance?.Detach();
            _hookInstance = null;
        }
    }

    internal static class CallbackStore
    {
        [DllImport("RainbowGuardCpp.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetLogCallback(LogCallback? cb_msg, LogCallback? cb_warn, LogCallback? cb_err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

        internal static readonly LogCallback Msg = MelonLogger.Msg;
        internal static readonly LogCallback Warn = MelonLogger.Warning;
        internal static readonly LogCallback Err = MelonLogger.Error;
    }
}
