using System;
using System.Numerics;
using ClockworkHolographicClient.Common;
using Windows.UI.Input.Spatial;
using System.Collections.Generic;

namespace ClockworkHolographicClient.Content
{
    /// <summary>
    /// This sample renderer instantiates a basic rendering pipeline.
    /// </summary>
    internal class SpritesRenderer : Disposer
    {
        // Cached reference to device resources.
        private DeviceResources                     deviceResources;

        // Direct3D resources for cube geometry.
        private SharpDX.Direct3D11.InputLayout      inputLayout;
        private SharpDX.Direct3D11.VertexShader     vertexShader;
        private SharpDX.Direct3D11.GeometryShader   geometryShader;
        private SharpDX.Direct3D11.PixelShader      pixelShader;


 

        // Variables used with the rendering loop.
        private bool                                loadingComplete = false;
        private float                               degreesPerSecond = 45.0f;


        // If the current D3D Device supports VPRT, we can avoid using a geometry
        // shader just to set the render target array index.
        private bool                                usingVprtShaders = false;
        private List<Sprite> sprites;
        private Spritesheet animationEngine;

        /// <summary>
        /// Loads vertex and pixel shaders from files and instantiates the cube geometry.
        /// </summary>
        public SpritesRenderer(DeviceResources deviceResources)
        {
            this.deviceResources  = deviceResources;

            animationEngine = new Spritesheet();
            //animationEngine.addObject("background", "idle", 1.05f, 0, 0,false);
            //animationEngine.addObject("paradog", "RunR", 1, 0, 0, false);
            //animationEngine.addObject("pipe", "normal", 0.95f, -0.1f, 0.1f, false);
            //animationEngine.addObject("pipe", "reverse", 0.95f,0.1f, 0.1f, false);
            animationEngine.setUp(30);

            ClockworkSocket.setup(animationEngine);

            //sprites = new List<Sprite>();
            //sprites.Add(new Sprite(1, 0, 0, "Assets/img/ninjacatRex.png"));
            //sprites.Add(new Sprite(-1, 0, 0, "Assets/img/ninjacatRex.png"));
            //sprites.Add(new Sprite(0, 0, -1, "Assets/img/ninjacatRex.png"));
            //sprites.Add(new Sprite(0, 0, 1, "Assets/img/ninjacatRex.png"));
            //foreach (var sprite in sprites)
            //{
            //    this.ToDispose(sprite);
            //}


            this.CreateDeviceDependentResourcesAsync();

        }

        /// <summary>
        /// Called once per frame, rotates the cube and calculates the model and view matrices.
        /// </summary>
        public void Update(StepTimer timer, SpatialPointerPose pose)
        {
            // Loading is asynchronous. Resources must be created before they can be updated.
            if (!loadingComplete)
            {
                return;
            }
            animationEngine.updateObjects();
            foreach (var sprite in animationEngine.getObjects())
            {
                sprite.Update(timer, deviceResources,pose);
            }
        }

        /// <summary>
        /// Renders one frame using the vertex and pixel shaders.
        /// On devices that do not support the D3D11_FEATURE_D3D11_OPTIONS3::
        /// VPAndRTArrayIndexFromAnyShaderFeedingRasterizer optional feature,
        /// a pass-through geometry shader is also used to set the render 
        /// target array index.
        /// </summary>
        public void Render()
        {
            // Loading is asynchronous. Resources must be created before drawing can occur.
            if (!this.loadingComplete)
            {
                return;
            }

            var context = this.deviceResources.D3DDeviceContext;

            foreach (var sprite in animationEngine.getObjects())
            {
                sprite.Render(context, inputLayout,vertexShader, usingVprtShaders, geometryShader, pixelShader);
            }

            
        }

        /// <summary>
        /// Creates device-based resources to store a constant buffer, cube
        /// geometry, and vertex and pixel shaders. In some cases this will also 
        /// store a geometry shader.
        /// </summary>
        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            usingVprtShaders = deviceResources.D3DDeviceSupportsVprt;

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            
            // On devices that do support the D3D11_FEATURE_D3D11_OPTIONS3::
            // VPAndRTArrayIndexFromAnyShaderFeedingRasterizer optional feature
            // we can avoid using a pass-through geometry shader to set the render
            // target array index, thus avoiding any overhead that would be 
            // incurred by setting the geometry shader stage.
            var vertexShaderFileName = usingVprtShaders ? "Content\\Shaders\\VPRTVertexShader.cso" : "Content\\Shaders\\VertexShader.cso";

            // Load the compiled vertex shader.
            var vertexShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFileName));

            // After the vertex shader file is loaded, create the shader and input layout.
            vertexShader = this.ToDispose(new SharpDX.Direct3D11.VertexShader(
                deviceResources.D3DDevice,
                vertexShaderByteCode));

            SharpDX.Direct3D11.InputElement[] vertexDesc =
            {
                new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,  0, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
                new SharpDX.Direct3D11.InputElement("COLOR",    0, SharpDX.DXGI.Format.R32G32_Float, 12, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
            };

            inputLayout = this.ToDispose(new SharpDX.Direct3D11.InputLayout(
                deviceResources.D3DDevice,
                vertexShaderByteCode,
                vertexDesc));
            
            if (!usingVprtShaders)
            {
                // Load the compiled pass-through geometry shader.
                var geometryShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\GeometryShader.cso"));

                // After the pass-through geometry shader file is loaded, create the shader.
                geometryShader = this.ToDispose(new SharpDX.Direct3D11.GeometryShader(
                    deviceResources.D3DDevice,
                    geometryShaderByteCode));
            }

            // Load the compiled pixel shader.
            var pixelShaderByteCode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\PixelShader.cso"));

            // After the pixel shader file is loaded, create the shader.
            pixelShader = this.ToDispose(new SharpDX.Direct3D11.PixelShader(
                deviceResources.D3DDevice,
                pixelShaderByteCode));


            foreach (var sprite in animationEngine.getObjects())
            {
                sprite.CreateDeviceDependentResourcesAsync(deviceResources);
            }

            animationEngine.resourcesLoaded(deviceResources);

            // Once the cube is loaded, the object is ready to be rendered.
            loadingComplete = true;
        }

        /// <summary>
        /// Releases device-based resources.
        /// </summary>
        public void ReleaseDeviceDependentResources()
        {
            loadingComplete = false;
            usingVprtShaders = false;
            this.RemoveAndDispose(ref vertexShader);
            this.RemoveAndDispose(ref inputLayout);
            this.RemoveAndDispose(ref pixelShader);
            this.RemoveAndDispose(ref geometryShader);
            foreach (var sprite in animationEngine.getObjects())
            {
                sprite.ReleaseDeviceDependentResources();
            }
        }

    }
}
