using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tanks.Client.Bootstrap
{
    public struct Vtx
    {
        public float3 Position;
        public float2 UV;
    }

    public sealed class VignetteSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        private NativeArray<Vtx> _vertices;
        private NativeArray<ushort> _indices;

        private Material _gradientMaterial;
        private Texture2D _gradient;
        private Mesh _vignetteMesh;

        public void Handle(TilemapGenerated message)
        {
            PositiveFloat width = message.Settings.WallWidth + 2;
            RecalculateVignette(default, message.Bounds.max, width);
        }

        private void RecalculateVignette(float3 min, float3 max, PositiveFloat width)
        {
            float3 blO = min;
            float3 tlO = new float3(min.x, max.y, 0);
            float3 trO = max;
            float3 brO = new float3(max.x, min.y, 0);

            float3 offset = new float3(width, width, 0);

            float3 blI = blO + offset;
            float3 tlI = tlO + new float3(offset.x, -offset.y, 0);
            float3 trI = trO - offset;
            float3 brI = brO + new float3(-offset.x, offset.y, 0);

            _vertices[0] = new Vtx { Position = blO, UV = new float2(0, 0) };
            _vertices[1] = new Vtx { Position = tlO, UV = new float2(0, 0) };
            _vertices[2] = new Vtx { Position = trO, UV = new float2(0, 0) };
            _vertices[3] = new Vtx { Position = brO, UV = new float2(0, 0) };

            _vertices[4] = new Vtx { Position = blI, UV = new float2(1, 1) };
            _vertices[5] = new Vtx { Position = tlI, UV = new float2(1, 1) };
            _vertices[6] = new Vtx { Position = trI, UV = new float2(1, 1) };
            _vertices[7] = new Vtx { Position = brI, UV = new float2(1, 1) };

            _vignetteMesh.SetVertexBufferData(_vertices, 0, 0, _vertices.Length);
            _vignetteMesh.RecalculateBounds();
        }

        private void CreateGradient()
        {
            _gradient = new Texture2D(1, 128, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };

            Color32[] pixels = new Color32[128];

            for (int i = 0; i < 128; i++)
            {
                float t = i / 127f;
                float alpha = math.cos(t * math.PIHALF);
                pixels[i] = new Color32(0, 0, 0, (byte)math.round(alpha * 255));
            }

            _gradient.SetPixelData(pixels, mipLevel: 0);
            _gradient.Apply(false, false);
        }

        private void CreateVignette()
        {
            CreateGradient();

            GameObject vignette = new GameObject("Vignette",
                typeof(MeshFilter), typeof(MeshRenderer));

            // Should be on top of others
            vignette.transform.position = Vector3.back * 5;

            var filter = vignette.GetComponent<MeshFilter>();
            var renderer = vignette.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            _gradientMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "Gradient",
                renderQueue = (int)RenderQueue.Transparent
            };

            _gradientMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _gradientMaterial.SetTexture("_BaseMap", _gradient);
            _gradientMaterial.SetColor("_BaseColor", Color.white);
            _gradientMaterial.SetFloat("_SrcBlend", 5);
            _gradientMaterial.SetFloat("_DstBlend", 10);
            _gradientMaterial.SetFloat("_Surface", 1);
            _gradientMaterial.SetFloat("_ZWrite", 0);
            _gradientMaterial.SetFloat("_DstBlendAlpha", 10);
            _gradientMaterial.SetFloat("_Cull", 0);

            _vignetteMesh = new Mesh
            {
                name = "VignetteMesh",
                indexFormat = IndexFormat.UInt16
            };

            SetupMeshBuffers();
            filter.sharedMesh = _vignetteMesh;
            renderer.sharedMaterial = _gradientMaterial;
        }

        private void SetupMeshBuffers()
        {
            _vertices = new NativeArray<Vtx>(8, Allocator.Persistent);
            _indices = new NativeArray<ushort>(24, Allocator.Persistent);

            _vignetteMesh.SetVertexBufferParams(_vertices.Length,
                new VertexAttributeDescriptor(
                    VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(
                    VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));

            int i = 0;

            // Bottom
            _indices[i++] = 0; _indices[i++] = 3; _indices[i++] = 7;
            _indices[i++] = 0; _indices[i++] = 7; _indices[i++] = 4;

            // Right
            _indices[i++] = 3; _indices[i++] = 2; _indices[i++] = 6;
            _indices[i++] = 3; _indices[i++] = 6; _indices[i++] = 7;

            // Top
            _indices[i++] = 2; _indices[i++] = 1; _indices[i++] = 5;
            _indices[i++] = 2; _indices[i++] = 5; _indices[i++] = 6;

            // Left
            _indices[i++] = 1; _indices[i++] = 0; _indices[i++] = 4;
            _indices[i++] = 1; _indices[i++] = 4; _indices[i++] = 5;

            _vignetteMesh.SetIndexBufferParams(_indices.Length, IndexFormat.UInt16);
            _vignetteMesh.SetIndexBufferData(_indices, 0, 0, _indices.Length);
            _vignetteMesh.SetSubMesh(0, new SubMeshDescriptor(0, _indices.Length));
        }

        private void Start()
        {
            CreateVignette();
            Behaviour.RegisterMessageHandler(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_vignetteMesh) Destroy(_vignetteMesh);
            if (_gradient) Destroy(_gradient);
            if (_gradientMaterial) Destroy(_gradientMaterial);

            if (_vertices.IsCreated) _vertices.Dispose();
            if (_indices.IsCreated) _indices.Dispose();
        }
    }
}
