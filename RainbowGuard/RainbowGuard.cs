using MelonLoader;
using MelonLoader.NativeUtils;
using MelonLoader.Utils;
using RainbowGuard;
using System;
using System.Collections.Generic;
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

            D3D11Output? output = Locate();
            if (output == null)
            {
                MelonLogger.Error("Could not generate D3D11 device");
                return;
            }

            using (output)
            {
                CreatePixelShaderStore.Init(output);
                HookCB.Init(output);
                HookShader.Init(output);
            }

            _didLoad = true;
        }

        /// <inheritdoc/>
        public override void OnDeinitializeMelon()
        {
            if (!_didLoad)
                return;

            CreatePixelShaderStore.DeInit();
            HookCB.DeInit();
            HookShader.DeInit();
        }
    }

    internal static class HookShader
    {
        private static NativeHook<PSSetShader>? _hookInstance;

        internal static readonly object Lock = new();
        internal static readonly Dictionary<IntPtr, IntPtr> CurrentPsByContext = [];
        internal const string Match = "Hidden/VFX/Guardstone VFX/System/Output Particle Shader Graph Quad - Unlit";

        internal static void Init(D3D11Output output)
        {
            if (_hookInstance != null)
                return;

            if (output.Context == IntPtr.Zero)
            {
                MelonLogger.Error("Could not generate D3D11 context methods");
                return;
            }

            IntPtr setShader = GetVTableEntry(output.Context, 9);
            if (setShader == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get PSSetShader");
                return;
            }

            IntPtr detour;
            unsafe
            {
                detour = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, uint, void>)&Hook;
            }
            _hookInstance = new NativeHook<PSSetShader>(setShader, detour);
            _hookInstance.Attach();
        }

        internal static void DeInit()
        {
            _hookInstance?.Detach();
            _hookInstance = null;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe void Hook(IntPtr context, IntPtr pPixelShader, IntPtr* ppClassInstances, uint numClassInstances)
        {
            _hookInstance!.Trampoline(
                context,
                pPixelShader,
                ppClassInstances,
                numClassInstances
            );

            lock (Lock)
                CurrentPsByContext[context] = pPixelShader;
        }
    }

    internal static class HookCB
    {
        private static NativeHook<PSSetConstantBuffers>? _hookInstance;
        private const string BufferName = "ConstantBuffer-733-1632";

        internal static void Init(D3D11Output output)
        {
            if (_hookInstance != null)
                return;

            if (output.Context == IntPtr.Zero)
            {
                MelonLogger.Error("Could not generate D3D11 context methods");
                return;
            }

            IntPtr setConstantBuffers = GetVTableEntry(output.Context, 16);
            if (setConstantBuffers == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get PSSetConstantBuffers");
                return;
            }

            IntPtr detour;
            unsafe
            {
                detour = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr*, void>)&Hook;
            }
            _hookInstance = new NativeHook<PSSetConstantBuffers>(setConstantBuffers, detour);
            _hookInstance.Attach();
        }

        internal static void DeInit()
        {
            _hookInstance?.Detach();
            _hookInstance = null;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe void Hook(IntPtr context, uint startSlot, uint numBuffers, IntPtr* ppConstantBuffers)
        {
            _hookInstance!.Trampoline(
                context,
                startSlot,
                numBuffers,
                ppConstantBuffers
            );

            IntPtr currentPs;
            lock (HookShader.Lock)
                HookShader.CurrentPsByContext.TryGetValue(context, out currentPs);
            if (currentPs == IntPtr.Zero || GetDebugName(currentPs) != HookShader.Match)
                return;

            IntPtr mapPtr = GetVTableEntry(context, 14);
            if (mapPtr == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get Map");
                return;
            }
            var map = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, D3D11_MAP, D3D11_MAP_FLAG, out D3D11_MAPPED_SUBRESOURCE, HResult>)mapPtr;

            IntPtr unmapPtr = GetVTableEntry(context, 15);
            if (unmapPtr == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get Unmap");
                return;
            }
            var unmap = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, void>)unmapPtr;

            for (uint i = 0; i < numBuffers; i++)
            {
                IntPtr resource = ppConstantBuffers[i];
                if (GetDebugName(resource) != BufferName)
                    continue;

                HResult err = map(context, resource, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0, out D3D11_MAPPED_SUBRESOURCE pMappedResource);
                if (err != HResult.S_OK)
                {
                    MelonLogger.Error($"Could not map buffer {err}");
                    continue;
                }

                float* f = (float*)pMappedResource.pData;
                double t = Environment.TickCount * 0.0005;

                f[0] = (float)(Math.Sin(t * 2.0) * 0.5 + 0.5);
                f[1] = (float)(Math.Sin(t * 2.0 + 2.094) * 0.5 + 0.5);
                f[2] = (float)(Math.Sin(t * 2.0 + 4.188) * 0.5 + 0.5);
                // f[3] is unused

                unmap(context, resource, 0);
            }
        }
    }

    internal static class CreatePixelShaderStore
    {
        private static NativeHook<CreatePixelShader>? _hookInstance;
        // ReSharper disable StringLiteralTypo
        private static readonly byte[] MatchShader = File.ReadAllBytes(Path.Combine(MelonEnvironment.UserDataDirectory, RainbowGuardModInfo.ModName, "match.dxbc"));
        private static readonly byte[] NewShader = File.ReadAllBytes(Path.Combine(MelonEnvironment.UserDataDirectory, RainbowGuardModInfo.ModName, "new.dxbc"));
        // ReSharper restore StringLiteralTypo

        internal static void Init(D3D11Output output)
        {
            if (_hookInstance != null)
                return;

            if (output.Device == IntPtr.Zero)
            {
                MelonLogger.Error("Could not generate D3D11 device methods");
                return;
            }

            IntPtr createPixelShader = GetVTableEntry(output.Device, 15);
            if (createPixelShader == IntPtr.Zero)
            {
                MelonLogger.Error("Failed to get CreatePixelShader");
                return;
            }

            IntPtr detour;
            unsafe
            {
                detour = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, void*, nuint, IntPtr, IntPtr*, HResult>)&Hook;
            }
            _hookInstance = new NativeHook<CreatePixelShader>(createPixelShader, detour);
            _hookInstance.Attach();
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
        private static unsafe HResult Hook(
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
