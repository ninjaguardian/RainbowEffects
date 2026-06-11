using MelonLoader;
using MelonLoader.NativeUtils;
using MelonLoader.Utils;
using RainbowGuard;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using static RainbowGuard.DXInterop;

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
                MelonLogger.Error("This mod only works on Direct3D11");
                return;
            }

            CreatePixelShaderStore.Init();

            _didLoad = true;
        }

        /// <inheritdoc/>
        public override void OnDeinitializeMelon()
        {
            if (!_didLoad)
                return;

            CreatePixelShaderStore.DeInit();
        }
    }

    internal static class CreatePixelShaderStore
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate int CreatePixelShader(
            IntPtr device,
            void* shaderBytecode,
            nuint bytecodeLength,
            IntPtr classLinkage,
            IntPtr* pixelShader
        );
        private static NativeHook<CreatePixelShader>? _hookInstance;
        // ReSharper disable StringLiteralTypo
        private static readonly byte[] MatchShader = File.ReadAllBytes(Path.Combine(MelonEnvironment.UserDataDirectory, RainbowGuardModInfo.ModName, "match.dxbc"));
        private static readonly byte[] NewShader = File.ReadAllBytes(Path.Combine(MelonEnvironment.UserDataDirectory, RainbowGuardModInfo.ModName, "new.dxbc"));
        // ReSharper restore StringLiteralTypo

        internal static void Init()
        {
            if (_hookInstance != null)
                return;

            D3D11Output? output = Locate();
            if (output == null || output.Device == IntPtr.Zero)
            {
                MelonLogger.Error("Could not generate D3D11 device");
                return;
            }

            using (output)
            {
                IntPtr createPixelShader = GetVTableEntry(output.Device, 15);
                if (createPixelShader == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to get CreatePixelShader");
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
        }

        internal static void DeInit()
        {
            _hookInstance?.Detach();
            _hookInstance = null;
        }

#if PRINT_CB
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe void PrintConstantBuffers(void* shaderBytecode, nuint bytecodeLength)
        {
            IntPtr pReflection = IntPtr.Zero;
            try
            {
                HResult err = D3DReflect(shaderBytecode, bytecodeLength, ref Rrid.IID_ID3D11ShaderReflection, out pReflection);
                if (err != HResult.S_OK)
                {
                    MelonLogger.Error($"Failed to reflect shader {err}");
                    return;
                }
                if (pReflection == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to reflect shader");
                    return;
                }

                IntPtr shaderDescPtr = GetVTableEntry(pReflection, 3);
                if (shaderDescPtr == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to get shader description function");
                    return;
                }
                var shaderDesc = (delegate* unmanaged[Stdcall]<IntPtr, out D3D11_SHADER_DESC, HResult>)shaderDescPtr;

                err = shaderDesc(pReflection, out D3D11_SHADER_DESC desc);
                if (err != HResult.S_OK)
                {
                    MelonLogger.Error($"Failed to get shader description {err}");
                    return;
                }

                IntPtr getConstantBufferByIndexPtr = GetVTableEntry(pReflection, 4);
                if (getConstantBufferByIndexPtr == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to get GetConstantBufferByIndex");
                    return;
                }
                var getConstantBufferByIndex =
                    (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr>)
                    getConstantBufferByIndexPtr;

                for (uint i = 0; i < desc.ConstantBuffers; i++)
                {
                    IntPtr pCB = getConstantBufferByIndex(pReflection, i);
                    if (pCB == IntPtr.Zero)
                    {
                        MelonLogger.Warning($"Failed to get constant buffer for index {i}");
                        continue;
                    }

                    IntPtr bufferDescPtr = GetVTableEntry(pCB, 0);
                    if (bufferDescPtr == IntPtr.Zero)
                    {
                        MelonLogger.Error($"Failed to get buffer description function for index {i}");
                        continue;
                    }
                    var bufferDesc = (delegate* unmanaged[Stdcall]<IntPtr, out D3D11_SHADER_BUFFER_DESC, HResult>)bufferDescPtr;

                    err = bufferDesc(pCB, out D3D11_SHADER_BUFFER_DESC cbDesc);
                    if (err != HResult.S_OK)
                    {
                        MelonLogger.Warning($"Failed to get constant buffer description for index {i}, {err}");
                        continue;
                    }

                    string? name = cbDesc.Name != IntPtr.Zero
                        ? Marshal.PtrToStringAnsi(cbDesc.Name)
                        : "<null>";

                    MelonLogger.Msg($"Constant Buffer {i}: {name}, Size: {cbDesc.Size}");
                }
            }
            finally
            {
                if (pReflection != IntPtr.Zero)
                    Marshal.Release(pReflection);
            }
        }
#endif

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe int Hook(
            IntPtr device,
            void* shaderBytecode,
            nuint bytecodeLength,
            IntPtr classLinkage,
            IntPtr* pixelShader
        )
        {
#if PRINT_CB
            PrintConstantBuffers(shaderBytecode, bytecodeLength);
#endif

            if (
                shaderBytecode == null ||
                bytecodeLength != (nuint)MatchShader.Length ||
                !new ReadOnlySpan<byte>(shaderBytecode, MatchShader.Length).SequenceEqual(MatchShader)
            )
            {
                return _hookInstance!.Trampoline(
                    device,
                    shaderBytecode,
                    bytecodeLength,
                    classLinkage,
                    pixelShader
                );
            }


            MelonLogger.Msg("MATCH!");

            fixed (byte* pNewShader = NewShader)
            {
                return _hookInstance!.Trampoline(
                    device,
                    pNewShader,
                    (nuint)NewShader.Length,
                    classLinkage,
                    pixelShader
                );
            }
        }
    }
}
