using MelonLoader;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RainbowGuard
{
    internal static class WindowHandle
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumThreadDelegate(
            [In] IntPtr hWnd,
            [In] IntPtr lParam
        );
        private static readonly EnumThreadDelegate Callback = EnumThreadCallback;
        private static IntPtr _foundHwnd;

        private static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (_foundHwnd == IntPtr.Zero)
                _foundHwnd = hWnd;

            return true;
        }

        [DllImport(
            "user32.dll",
            //SetLastError = true,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(
            [In] uint dwThreadId,
            [In] EnumThreadDelegate lpfn,
            [In] IntPtr lParam
        );

        [DllImport(
            "kernel32.dll",
            //SetLastError = true,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        private static extern uint GetCurrentThreadId();

        internal static IntPtr GetWindowHandle()
        {
            _foundHwnd = IntPtr.Zero;

            EnumThreadWindows(GetCurrentThreadId(), Callback, IntPtr.Zero);

            return _foundHwnd;
        }
    }

    internal static class DXInterop
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable IdentifierTypo
        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedMember.Global

        #region Data Structures

        internal enum HResult
        {
            S_OK = 0x00000000,
            S_FALSE = 0x00000001,
            E_INVALIDARG = unchecked((int)0x80070057),
            E_OUTOFMEMORY = unchecked((int)0x8007000E),
            E_FAIL = unchecked((int)0x80004005)
        }

        internal enum D3D11_MAP : uint
        {
            D3D11_MAP_READ = 1,
            D3D11_MAP_WRITE = 2,
            D3D11_MAP_READ_WRITE = 3,
            D3D11_MAP_WRITE_DISCARD = 4,
            D3D11_MAP_WRITE_NO_OVERWRITE = 5
        };

        [Flags]
        internal enum D3D11_MAP_FLAG : uint
        {
            D3D11_MAP_FLAG_DO_NOT_WAIT = 0x100000
        };

        private enum D3D_FEATURE_LEVEL : uint
        {
            D3D_FEATURE_LEVEL_1_0_GENERIC = 0x100,
            D3D_FEATURE_LEVEL_1_0_CORE = 0x1000,
            D3D_FEATURE_LEVEL_9_1 = 0x9100,
            D3D_FEATURE_LEVEL_9_2 = 0x9200,
            D3D_FEATURE_LEVEL_9_3 = 0x9300,
            D3D_FEATURE_LEVEL_10_0 = 0xa000,
            D3D_FEATURE_LEVEL_10_1 = 0xa100,
            D3D_FEATURE_LEVEL_11_0 = 0xb000,
            D3D_FEATURE_LEVEL_11_1 = 0xb100,
            D3D_FEATURE_LEVEL_12_0 = 0xc000,
            D3D_FEATURE_LEVEL_12_1 = 0xc100,
            D3D_FEATURE_LEVEL_12_2 = 0xc200
        }

        private enum D3D_DRIVER_TYPE : uint
        {
            D3D_DRIVER_TYPE_UNKNOWN = 0,
            D3D_DRIVER_TYPE_HARDWARE,
            D3D_DRIVER_TYPE_REFERENCE,
            D3D_DRIVER_TYPE_NULL,
            D3D_DRIVER_TYPE_SOFTWARE,
            D3D_DRIVER_TYPE_WARP
        };

        private enum DXGI_FORMAT : uint
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,

            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,


            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,

            DXGI_FORMAT_A4B4G4R4_UNORM = 191,


            DXGI_FORMAT_FORCE_UINT = 0xFFFFFFFFu
        }

        private enum DXGI_MODE_SCANLINE_ORDER : uint
        {
            DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED = 0,
            DXGI_MODE_SCANLINE_ORDER_PROGRESSIVE = 1,
            DXGI_MODE_SCANLINE_ORDER_UPPER_FIELD_FIRST = 2,
            DXGI_MODE_SCANLINE_ORDER_LOWER_FIELD_FIRST = 3
        }

        private enum DXGI_MODE_SCALING : uint
        {
            DXGI_MODE_SCALING_UNSPECIFIED = 0,
            DXGI_MODE_SCALING_CENTERED = 1,
            DXGI_MODE_SCALING_STRETCHED = 2
        }

        private enum DXGI_SWAP_EFFECT : uint
        {
            DXGI_SWAP_EFFECT_DISCARD = 0,
            DXGI_SWAP_EFFECT_SEQUENTIAL = 1,
            DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL = 3,
            DXGI_SWAP_EFFECT_FLIP_DISCARD = 4
        }

        [Flags]
        private enum DXGI_USAGE : uint
        {
            DXGI_USAGE_SHADER_INPUT = 0x00000010,
            DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020,
            DXGI_USAGE_BACK_BUFFER = 0x00000040,
            DXGI_USAGE_SHARED = 0x00000080,
            DXGI_USAGE_READ_ONLY = 0x00000100,
            DXGI_USAGE_DISCARD_ON_PRESENT = 0x00000200,
            DXGI_USAGE_UNORDERED_ACCESS = 0x00000400
        }

        [Flags]
        private enum DXGI_SWAP_CHAIN_FLAG : uint
        {
            DXGI_SWAP_CHAIN_FLAG_NONPREROTATED = 1,
            DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH = 2,
            DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE = 4,
            DXGI_SWAP_CHAIN_FLAG_RESTRICTED_CONTENT = 8,
            DXGI_SWAP_CHAIN_FLAG_RESTRICT_SHARED_RESOURCE_DRIVER = 16,
            DXGI_SWAP_CHAIN_FLAG_DISPLAY_ONLY = 32,
            DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT = 64,
            DXGI_SWAP_CHAIN_FLAG_FOREGROUND_LAYER = 128,
            DXGI_SWAP_CHAIN_FLAG_FULLSCREEN_VIDEO = 256,
            DXGI_SWAP_CHAIN_FLAG_YUV_VIDEO = 512,
            DXGI_SWAP_CHAIN_FLAG_HW_PROTECTED = 1024,
            DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING = 2048,
            DXGI_SWAP_CHAIN_FLAG_RESTRICTED_TO_ALL_HOLOGRAPHIC_DISPLAYS = 4096
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_MODE_DESC
        {
            public uint Width;
            public uint Height;
            public DXGI_RATIONAL RefreshRate;
            public DXGI_FORMAT Format;
            public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
            public DXGI_MODE_SCALING Scaling;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_SAMPLE_DESC
        {
            public uint Count;
            public uint Quality;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_SWAP_CHAIN_DESC
        {
            public DXGI_MODE_DESC BufferDesc;
            public DXGI_SAMPLE_DESC SampleDesc;
            public DXGI_USAGE BufferUsage;
            public uint BufferCount;
            public IntPtr OutputWindow;
            [MarshalAs(UnmanagedType.Bool)] public bool Windowed;
            public DXGI_SWAP_EFFECT SwapEffect;
            public DXGI_SWAP_CHAIN_FLAG Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct D3D11_MAPPED_SUBRESOURCE
        {
            public void* pData;
            public uint RowPitch;
            public uint DepthPitch;
        }

#if PRINT_CB
        [StructLayout(LayoutKind.Sequential)]
        internal struct D3D11_SHADER_DESC
        {
            public uint Version;
            public IntPtr Creator;  // LPCSTR
            public uint Flags;
            public uint ConstantBuffers;
            public uint BoundResources;
            public uint InputParameters;
            public uint OutputParameters;
            public uint InstructionCount;
            public uint TempRegisterCount;
            public uint TempArrayCount;
            public uint DefCount;
            public uint DclCount;
            public uint TextureNormalInstructions;
            public uint TextureLoadInstructions;
            public uint TextureCompInstructions;
            public uint TextureBiasInstructions;
            public uint TextureGradientInstructions;
            public uint FloatInstructionCount;
            public uint IntInstructionCount;
            public uint UintInstructionCount;
            public uint StaticFlowControlCount;
            public uint DynamicFlowControlCount;
            public uint MacroInstructionCount;
            public uint ArrayInstructionCount;
            public uint CutInstructionCount;
            public uint EmitInstructionCount;
            public uint GSOutputTopology;  // D3D_PRIMITIVE_TOPOLOGY
            public uint GSMaxOutputVertexCount;
            public uint InputPrimitive;  // D3D_PRIMITIVE
            public uint PatchConstantParameters;
            public uint cGSInstanceCount;
            public uint cControlPoints;
            public uint HSOutputPrimitive;  // D3D_TESSELLATOR_OUTPUT_PRIMITIVE
            public uint HSPartitioning;  // D3D_TESSELLATOR_PARTITIONING
            public uint TessellatorDomain;  // D3D_TESSELLATOR_DOMAIN
            public uint cBarrierInstructions;
            public uint cInterlockedInstructions;
            public uint cTextureStoreInstructions;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct D3D11_SHADER_BUFFER_DESC
        {
            public IntPtr Name;  // LPCSTR
            public uint Type;  // D3D_CBUFFER_TYPE
            public uint Variables;
            public uint Size;
            public uint uFlags;
        }
#endif

        #endregion

        internal static class Rrid
        {
            public static Guid IDXGIFactory = new("7b7166ec-21c7-44ae-b21a-c9ae321ae369");
            public static Guid WKPDID_D3DDebugObjectName = new(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);
            public static Guid WKPDID_D3DDebugObjectNameW = new(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);
#if PRINT_CB
            public static Guid IID_ID3D11ShaderReflection = new("8d536ca1-0cca-4956-a837-786963755584");
#endif
        }

        // ReSharper restore UnusedMember.Global
        // ReSharper restore UnusedMember.Local
        // ReSharper restore IdentifierTypo
        // ReSharper restore InconsistentNaming

        [DllImport(
            "dxgi.dll",
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        private static extern HResult CreateDXGIFactory(
            ref Guid riid,
            [Out] out IntPtr factory
        );

        [DllImport(
            "d3d11.dll",
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        private static extern HResult D3D11CreateDeviceAndSwapChain(
            [In] IntPtr adapter,
            D3D_DRIVER_TYPE driverType,
            IntPtr software,
            uint flags,
            [In] D3D_FEATURE_LEVEL[] featureLevels,
            uint featureLevelCount,
            uint sdkVersion,
            [In] ref DXGI_SWAP_CHAIN_DESC swapChainDesc,
            [Out] out IntPtr swapChain,
            [Out] out IntPtr device,
            [Out] out uint featureLevel,
            [Out] out IntPtr context
        );

        internal sealed class D3D11Output : IDisposable
        {
            public IntPtr SwapChain;
            public IntPtr Device;
            public IntPtr Context;

            public void Dispose()
            {
                if (SwapChain != IntPtr.Zero)
                {
                    Marshal.Release(SwapChain);
                    SwapChain = IntPtr.Zero;
                }

                if (Device != IntPtr.Zero)
                {
                    Marshal.Release(Device);
                    Device = IntPtr.Zero;
                }

                if (Context != IntPtr.Zero)
                {
                    Marshal.Release(Context);
                    Context = IntPtr.Zero;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe IntPtr GetVTableEntry(IntPtr comObject, int index) => (IntPtr)(*(void***)comObject)[index];

        internal static unsafe D3D11Output? Locate()
        {
            IntPtr factoryPtr = IntPtr.Zero;
            IntPtr adapterPtr = IntPtr.Zero;
            try
            {
                HResult err = CreateDXGIFactory(ref Rrid.IDXGIFactory, out factoryPtr);
                if (err != HResult.S_OK)
                {
                    MelonLogger.Error($"CreateDXGIFactory failed: {err}");
                    return null;
                }
                if (factoryPtr == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to create DXGI factory");
                    return null;
                }

                IntPtr enumAdaptersFnPtr = GetVTableEntry(factoryPtr, 7);
                if (enumAdaptersFnPtr == IntPtr.Zero)
                {
                    MelonLogger.Error("Could not locate EnumAdapters");
                    return null;
                }
                var enumAdapters =
                    (delegate* unmanaged[Stdcall]
                        <IntPtr, uint, out IntPtr, HResult>)
                    enumAdaptersFnPtr;

                // TODO: is adapter 0 correct
                err = enumAdapters(factoryPtr, 0, out adapterPtr);
                if (err != HResult.S_OK)
                {
                    MelonLogger.Error($"EnumAdapters failed: {err}");
                    return null;
                }
                if (adapterPtr == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to get adapter");
                    return null;
                }

                IntPtr handle = WindowHandle.GetWindowHandle();
                if (handle == IntPtr.Zero)
                {
                    MelonLogger.Error("Failed to get window handle");
                    return null;
                }

                DXGI_SWAP_CHAIN_DESC scDesc = default;
                scDesc.BufferDesc.Width = 100;
                scDesc.BufferDesc.Height = 100;
                scDesc.BufferDesc.Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                scDesc.BufferDesc.RefreshRate.Numerator = 60;
                scDesc.BufferDesc.RefreshRate.Denominator = 1;
                scDesc.SampleDesc.Count = 1;
                scDesc.BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT;
                scDesc.BufferCount = 1;
                scDesc.OutputWindow = handle;
                scDesc.Windowed = true;
                scDesc.SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD;
                scDesc.Flags = DXGI_SWAP_CHAIN_FLAG.DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;

                D3D_FEATURE_LEVEL[] featureLevels =
                {
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0
                };

                err = D3D11CreateDeviceAndSwapChain(
                    adapterPtr,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
                    IntPtr.Zero,
                    0,
                    featureLevels,
                    (uint)featureLevels.Length,
                    7,
                    ref scDesc,
                    out IntPtr swapChain,
                    out IntPtr device,
                    // ReSharper disable once UnusedVariable
                    out uint featureLevel,
                    out IntPtr context
                );
                if (err != HResult.S_OK)
                    MelonLogger.Error($"D3D11CreateDeviceAndSwapChain failed {err}");

                return new D3D11Output
                {
                    Context = context,
                    SwapChain = swapChain,
                    Device = device
                };
            }
            finally
            {
                if (adapterPtr != IntPtr.Zero)
                    Marshal.Release(adapterPtr);

                if (factoryPtr != IntPtr.Zero)
                    Marshal.Release(factoryPtr);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate HResult CreatePixelShader(
            IntPtr device,
            [In] void* shaderBytecode,
            [In] nuint bytecodeLength,
            [In] IntPtr classLinkage,
            [Out] IntPtr* pixelShader
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate void PSSetShader( // 9
            IntPtr context,
            [In] IntPtr pPixelShader,
            [In] IntPtr* ppClassInstances,
            uint numClassInstances
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate void PSSetConstantBuffers( // 16
            IntPtr context,
            [In] uint startSlot,
            [In] uint numBuffers,
            [In] IntPtr* ppConstantBuffers // Array of ID3D11Buffer
        );

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe string GetDebugName(IntPtr deviceChild)
        {
            if (deviceChild == IntPtr.Zero)
                return "<null>";

            var getPrivateData = (delegate* unmanaged[Stdcall]<IntPtr, ref Guid, ref uint, void*, HResult>)
                GetVTableEntry(deviceChild, 4);

            if (getPrivateData == null)
                return "<unnamed>";

            uint size = 256;
            byte* data = stackalloc byte[(int)size];

            HResult hr = getPrivateData(deviceChild, ref Rrid.WKPDID_D3DDebugObjectName, ref size, data);
            if (hr != HResult.S_OK || size == 0)
                return "<unnamed>";

            string name = Marshal.PtrToStringAnsi((IntPtr)data, (int)size);
            return string.IsNullOrEmpty(name) ? "<unnamed>" : name.TrimEnd('\0');
        }

#if PRINT_CB
        [DllImport(
            "d3dcompiler_47.dll",
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        internal static extern unsafe HResult D3DReflect(
            [In] void* pSrcData,
            [In] nuint srcDataSize,
            [In] ref Guid pInterface,
            [Out] out IntPtr ppReflector
        );
#endif
    }
}
