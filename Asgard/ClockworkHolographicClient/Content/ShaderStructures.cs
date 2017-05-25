using System.Numerics;

namespace ClockworkHolographicClient.Content
{
    /// <summary>
    /// Constant buffer used to send hologram position transform to the shader pipeline.
    /// </summary>
    internal struct ModelConstantBuffer
    {
        public Matrix4x4 model;
    }

    /// <summary>
    /// Used to send per-vertex data to the vertex shader.
    /// </summary>
    internal struct VertexPositionTexture
    {
        public VertexPositionTexture(Vector3 pos, Vector2 tex)
        {
            this.pos   = pos;
            this.tex = tex;
        }

        public Vector3 pos;
        public Vector2 tex;
    };
}
