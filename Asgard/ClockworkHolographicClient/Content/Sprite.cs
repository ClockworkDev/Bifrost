using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ClockworkHolographicClient.Common;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;
using SharpDX;
using Windows.UI.Xaml.Media.Imaging;
using SharpDX.DXGI;
using System.IO;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;

namespace ClockworkHolographicClient.Content
{
    class Sprite : Disposer
    {
        private System.Numerics.Vector3 position;
        private string spriteImg;
        private DeviceResources cachedResources;
        ShaderResourceView textureView;


        private SharpDX.Direct3D11.Buffer modelConstantBuffer;
        // System resources for cube geometry.
        private ModelConstantBuffer modelConstantBufferData;
        private int indexCount = 0;
        private SharpDX.Direct3D11.Buffer indexBuffer;
        private SharpDX.Direct3D11.Buffer vertexBuffer;

        float imageWidth, imageHeight;

        bool hasLoaded;


        public Sprite(float x, float y, float z, string spriteImg)
        {
            position = new System.Numerics.Vector3(x * Spritesheet.positionScaleFactor, y * Spritesheet.positionScaleFactor, z * Spritesheet.positionScaleFactor);
            this.spriteImg = spriteImg;
            hasLoaded = false;
        }

        public void Update(StepTimer timer, DeviceResources deviceResources, SpatialPointerPose pose)
        {
            if (pose != null)
            {
                cachedResources = deviceResources;
                var headPosition = pose.Head.Position;
                //Calculate the rotation for billboarding
                SharpDX.Vector3 facingNormal = new SharpDX.Vector3(headPosition.X - position.X, headPosition.Y - position.Y, headPosition.Z - position.Z);
                facingNormal.Normalize();

                SharpDX.Vector3 xAxisRotation = new SharpDX.Vector3(facingNormal.Z, 0, -facingNormal.X);
                xAxisRotation.Normalize();
                SharpDX.Vector3 yAxisRotation = SharpDX.Vector3.Cross(facingNormal, xAxisRotation);
                yAxisRotation.Normalize();

                Matrix4x4 modelRotation = new Matrix4x4(xAxisRotation.X, xAxisRotation.Y, xAxisRotation.Z, 0,
                    yAxisRotation.X, yAxisRotation.Y, yAxisRotation.Z, 0,
                    facingNormal.X, facingNormal.Y, facingNormal.Z, 0,
                    0, 0, 0, 1);

                // Position the cube.
                Matrix4x4 modelTranslation = Matrix4x4.CreateTranslation(position);


                // Multiply to get the transform matrix.
                // Note that this transform does not enforce a particular coordinate system. The calling
                // class is responsible for rendering this content in a consistent manner.
                Matrix4x4 modelTransform = modelRotation * modelTranslation;

                // The view and projection matrices are provided by the system; they are associated
                // with holographic cameras, and updated on a per-camera basis.
                // Here, we provide the model transform for the sample hologram. The model transform
                // matrix is transposed to prepare it for the shader.
                this.modelConstantBufferData.model = Matrix4x4.Transpose(modelTransform);


                // Use the D3D device context to update Direct3D device-based resources.
                var context = deviceResources.D3DDeviceContext;

                // Update the model transform buffer for the hologram.
                context.UpdateSubresource(ref this.modelConstantBufferData, this.modelConstantBuffer);
            }
        }

        internal void setTextureCoordinates(float x, float y, float w, float h)
        {
            if (hasLoaded == false || cachedResources == null)
            {
                return;
            }
            float u1 = x / imageWidth;
            float u2 = (x + w) / imageWidth;
            float v1 = y / imageHeight;
            float v2 = (y + h) / imageHeight;
            var height = imageHeight * Spritesheet.textureScaleFactor * (v2 - v1) / 2;
            var width = imageWidth * Spritesheet.textureScaleFactor * (u2 - u1) / 2;
            VertexPositionTexture[] cubeVertices =
           {
                new VertexPositionTexture(new System.Numerics.Vector3(-1f*width, -1f*height, 0f), new System.Numerics.Vector2(u1, v2)),
                new VertexPositionTexture(new System.Numerics.Vector3(1f*width, -1f*height,  0f), new System.Numerics.Vector2(u2, v2)),
                new VertexPositionTexture(new System.Numerics.Vector3(-1f*width,  1f*height, 0f), new System.Numerics.Vector2(u1, v1)),
                new VertexPositionTexture(new System.Numerics.Vector3(1f*width,  1f*height,  0f), new System.Numerics.Vector2(u2, v1)),
            };
            DataStream stream;
            var dataBox = cachedResources.D3DDeviceContext.MapSubresource(vertexBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.WriteRange(cubeVertices);
            cachedResources.D3DDeviceContext.UnmapSubresource(vertexBuffer, 0); //to update the data on GPU

            stream.Dispose();
            //cachedResources.D3DDeviceContext.UpdateSubresource(cubeVertices, vertexBuffer);
        }

        public void CreateDeviceDependentResourcesAsync(DeviceResources deviceResources)
        {

            byte[] bytes = Convert.FromBase64String(spriteImg);
            Stream stream = new MemoryStream(bytes);
            var factory = new SharpDX.WIC.ImagingFactory2();
            var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
              factory,
              stream,
              SharpDX.WIC.DecodeOptions.CacheOnDemand
              );

            var formatConverter = new SharpDX.WIC.FormatConverter(factory);

            formatConverter.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);

            SharpDX.WIC.BitmapSource bitmapSource = formatConverter;

            imageWidth = bitmapSource.Size.Width;
            imageHeight = bitmapSource.Size.Height;

            var height = bitmapSource.Size.Height * Spritesheet.textureScaleFactor;
            var width = bitmapSource.Size.Width * Spritesheet.textureScaleFactor;

            BlendStateDescription blendSdesc = new BlendStateDescription();
            blendSdesc.IndependentBlendEnable = false;
            blendSdesc.AlphaToCoverageEnable = false;
            blendSdesc.RenderTarget[0].IsBlendEnabled = true;
            blendSdesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendSdesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendSdesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendSdesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blendSdesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendSdesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendSdesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            BlendState blendS = new BlendState(deviceResources.D3DDevice, blendSdesc);
            deviceResources.D3DDeviceContext.OutputMerger.SetBlendState(blendS);

            // Load mesh vertices. Each vertex has a position and a color.
            // Note that the cube size has changed from the default DirectX app
            // template. Windows Holographic is scaled in meters, so to draw the
            // cube at a comfortable size we made the cube width 0.2 m (20 cm).
            VertexPositionTexture[] cubeVertices =
            {
                new VertexPositionTexture(new System.Numerics.Vector3(-1f*width, -1f*height, 0f), new System.Numerics.Vector2(0.0f, 1.0f)),
                new VertexPositionTexture(new System.Numerics.Vector3(1f*width, -1f*height,  0f), new System.Numerics.Vector2(1.0f, 1.0f)),
                new VertexPositionTexture(new System.Numerics.Vector3(-1f*width,  1f*height, 0f), new System.Numerics.Vector2(0.0f, 0.0f)),
                new VertexPositionTexture(new System.Numerics.Vector3(1f*width,  1f*height,  0f), new System.Numerics.Vector2(1.0f, 0.0f))
            };

            BufferDescription mdescription = new BufferDescription(sizeof(float) * 5 * cubeVertices.Length, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            vertexBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
                    deviceResources.D3DDevice,
                    cubeVertices,
                   mdescription));
            //vertexBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
            //    deviceResources.D3DDevice,
            //    SharpDX.Direct3D11.BindFlags.VertexBuffer,
            //    cubeVertices,
            //    0,
            //    ResourceUsage.Dynamic, CpuAccessFlags.Write));



            // Load mesh indices. Each trio of indices represents
            // a triangle to be rendered on the screen.
            // For example: 0,2,1 means that the vertices with indexes
            // 0, 2 and 1 from the vertex buffer compose the 
            // first triangle of this mesh.
            ushort[] cubeIndices =
            {
                2,1,0, // -x
                2,3,1,
                //back face
                2,0,1, // -x
                2,1,3,
            };

            indexCount = cubeIndices.Length;
            indexBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.IndexBuffer,
                cubeIndices));
            // Create a constant buffer to store the model matrix.
            modelConstantBuffer = this.ToDispose(SharpDX.Direct3D11.Buffer.Create(
                deviceResources.D3DDevice,
                SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                ref modelConstantBufferData));

            //Load the image

            Texture2D texture = TextureLoader.CreateTexture2DFromBitmap(deviceResources.D3DDevice, bitmapSource);
            textureView = new ShaderResourceView(deviceResources.D3DDevice, texture);
            deviceResources.D3DDeviceContext.PixelShader.SetShaderResource(0, textureView);
            //Load the sampler
            SamplerStateDescription samplerDesc = new SamplerStateDescription();
            samplerDesc.AddressU = TextureAddressMode.Wrap;
            samplerDesc.AddressV = TextureAddressMode.Wrap;
            samplerDesc.AddressW = TextureAddressMode.Wrap;
            samplerDesc.ComparisonFunction = Comparison.Never;
            samplerDesc.Filter = Filter.MinMagMipLinear;
            samplerDesc.MaximumLod = float.MaxValue;
            SamplerState sampler = new SamplerState(deviceResources.D3DDevice, samplerDesc);
            deviceResources.D3DDeviceContext.PixelShader.SetSampler(0, sampler);

            hasLoaded = true;
        }

        internal void Render(DeviceContext3 context, InputLayout inputLayout, VertexShader vertexShader, bool usingVprtShaders, GeometryShader geometryShader, PixelShader pixelShader)
        {
            //Update the texture
            context.PixelShader.SetShaderResource(0, textureView);
            // Each vertex is one instance of the VertexPositionColor struct.
            int stride = SharpDX.Utilities.SizeOf<VertexPositionTexture>();
            int offset = 0;
            var bufferBinding = new SharpDX.Direct3D11.VertexBufferBinding(this.vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(
                this.indexBuffer,
                SharpDX.DXGI.Format.R16_UInt, // Each index is one 16-bit unsigned integer (short).
                0);
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = inputLayout;

            // Attach the vertex shader.
            context.VertexShader.SetShader(vertexShader, null, 0);
            // Apply the model constant buffer to the vertex shader.
            context.VertexShader.SetConstantBuffers(0, this.modelConstantBuffer);

            if (!usingVprtShaders)
            {
                // On devices that do not support the D3D11_FEATURE_D3D11_OPTIONS3::
                // VPAndRTArrayIndexFromAnyShaderFeedingRasterizer optional feature,
                // a pass-through geometry shader is used to set the render target 
                // array index.
                context.GeometryShader.SetShader(geometryShader, null, 0);
            }

            // Attach the pixel shader.
            context.PixelShader.SetShader(pixelShader, null, 0);

            // Draw the objects.
            context.DrawIndexedInstanced(
                indexCount,     // Index count per instance.
                2,              // Instance count.
                0,              // Start index location.
                0,              // Base vertex location.
                0               // Start instance location.
                );
        }

        /// <summary>
        /// Releases device-based resources.
        /// </summary>
        public void ReleaseDeviceDependentResources()
        {
            this.RemoveAndDispose(ref modelConstantBuffer);
            this.RemoveAndDispose(ref vertexBuffer);
            this.RemoveAndDispose(ref indexBuffer);
        }

        public void setPosition(float x, float y, float z)
        {
            position = new System.Numerics.Vector3(x * Spritesheet.positionScaleFactor, y * Spritesheet.positionScaleFactor, z * Spritesheet.positionScaleFactor);
        }
    }
}
