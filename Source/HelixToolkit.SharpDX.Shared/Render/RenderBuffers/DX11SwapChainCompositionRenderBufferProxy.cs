﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using System.Linq;
using System;
#if !NETFX_CORE && !WINUI_NET5_0
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#elif WINUI_NET5_0
namespace HelixToolkit.WinUI
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Render
    {
        using Core2D;
        using Utilities;
        /// <summary>
        /// 
        /// </summary>
        public class DX11SwapChainCompositionRenderBufferProxy : DX11RenderBufferProxyBase
        {
            private SwapChain2 swapChain;
            /// <summary>
            /// Gets the swap chain.
            /// </summary>
            /// <value>
            /// The swap chain.
            /// </value>
            public SwapChain2 SwapChain { get { return swapChain; } }

            /// <summary>
            /// Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy"/> class.
            /// </summary>
            /// <param name="deviceResource"></param>
            public DX11SwapChainCompositionRenderBufferProxy(IDeviceResources deviceResource) : base(deviceResource)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DX11SwapChainRenderBufferProxy"/> class.
            /// </summary>
            /// <param name="deviceResource"></param>
            /// <param name="useDepthStencilBuffer"></param>
            public DX11SwapChainCompositionRenderBufferProxy(IDeviceResources deviceResource, bool useDepthStencilBuffer)
                : base(deviceResource, useDepthStencilBuffer)
            {
            }

            /// <summary>
            /// Called when [create render target and depth buffers].
            /// </summary>
            /// <param name="width">The width.</param>
            /// <param name="height">The height.</param>
            /// <returns></returns>
            protected override ShaderResourceViewProxy OnCreateBackBuffer(int width, int height)
            {
                if (swapChain == null || swapChain.IsDisposed)
                {
                    swapChain = CreateSwapChain();
                }
                else
                {
                    swapChain.ResizeBuffers(swapChain.Description1.BufferCount, TargetWidth, TargetHeight, swapChain.Description.ModeDescription.Format, swapChain.Description.Flags);
                }

                var backBuffer = Collect(new ShaderResourceViewProxy(Device, Texture2D.FromSwapChain<Texture2D>(swapChain, 0)));
                d2dTarget = Collect(new D2DTargetProxy());
                d2dTarget.Initialize(swapChain, DeviceContext2D);
                return backBuffer;
            }

            private SwapChain2 CreateSwapChain()
            {
                var desc = CreateSwapChainDescription();
                using (var dxgiDevice2 = Device.QueryInterface<global::SharpDX.DXGI.Device2>())
                using (var dxgiAdapter = dxgiDevice2.Adapter)
                using (var dxgiFactory2 = dxgiAdapter.GetParent<Factory2>())
                {
                    // The CreateSwapChain method is used so we can descend
                    // from this class and implement a swapchain for a desktop
                    // or a Windows 8 AppStore app
                    using (global::SharpDX.DXGI.SwapChain1 swapChain1 = new global::SharpDX.DXGI.SwapChain1(dxgiFactory2, Device, ref desc))
                    {
                        return swapChain1.QueryInterface<global::SharpDX.DXGI.SwapChain2>();
                    }
                }
            }

            /// <summary>
            /// Creates the swap chain description.
            /// </summary>
            /// <returns>A swap chain description</returns>
            /// <remarks>
            /// This method can be overloaded in order to modify default parameters.
            /// </remarks>
            protected virtual SwapChainDescription1 CreateSwapChainDescription()
            {
                int sampleCount = 1;
                int sampleQuality = 0;
                // SwapChain description

                var desc = new SwapChainDescription1()
                {
                    Width = Math.Max(1, TargetWidth),
                    Height = Math.Max(1, TargetHeight),
                    // B8G8R8A8_UNorm gives us better performance 
                    Format = Format,
                    Stereo = false,
                    SampleDescription = new SampleDescription(sampleCount, sampleQuality),
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = 2,
                    SwapEffect = SwapEffect.FlipSequential,
                    Scaling = Scaling.Stretch,
                    Flags = SwapChainFlags.None
                };
                return desc;
            }

            private readonly PresentParameters presentParams = new PresentParameters();

            /// <summary>
            /// Presents this instance.
            /// </summary>
            /// <returns></returns>
            public override bool Present()
            {
                var res = swapChain.Present(VSyncInterval, PresentFlags.None, presentParams);
                if (res.Success)
                {
                    return true;
                }
                else
                {
                    var desc = ResultDescriptor.Find(res);
                    if (desc == global::SharpDX.DXGI.ResultCode.DeviceRemoved || desc == global::SharpDX.DXGI.ResultCode.DeviceReset || desc == global::SharpDX.DXGI.ResultCode.DeviceHung)
                    {
                        RaiseOnDeviceLost();
                    }
                    else
                    {
                        swapChain.Present(VSyncInterval, PresentFlags.Restart, presentParams);
                    }
                    return false;
                }
            }
            /// <summary>
            /// Must release swapchain at last after all its created resources have been released.
            /// </summary>
            public override void DisposeAndClear()
            {
                base.DisposeAndClear();
                Disposer.RemoveAndDispose(ref swapChain);
            }
        }
    }

}
