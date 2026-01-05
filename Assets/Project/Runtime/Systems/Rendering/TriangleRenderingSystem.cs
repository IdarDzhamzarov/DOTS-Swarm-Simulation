using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwarmSimulation.Runtime.Systems.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class TriangleRenderingSystem : SystemBase
    {
        private Mesh _agentTriangleMesh;
        private Mesh _predatorTriangleMesh;
        private Material _agentMaterial;
        private Material _predatorMaterial;

        private NativeList<Matrix4x4> _agentMatrices;
        private NativeList<Matrix4x4> _predatorMatrices;

        protected override void OnCreate()
        {
            CreateTriangleMeshes();
            CreateMaterials();

            _agentMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);
            _predatorMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);

            Debug.Log("TriangleRenderingSystem created");
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!SystemAPI.HasSingleton<RenderingConfigurationData>())
            {
                EntityManager.CreateEntity(typeof(RenderingConfigurationData));
                SystemAPI.SetSingleton(new RenderingConfigurationData
                {
                    MaximumInstancesPerBatch = 1023,
                    EnableFrustumCulling = false,
                    EnableDistanceCulling = false,
                    CullingDistance = 200f,
                    LodDistanceThreshold = 50f
                });
                Debug.Log("RenderingConfigurationData created by system");
            }
        }

        private void CreateTriangleMeshes()
        {
            _agentTriangleMesh = CreateDoubleSidedTriangle(0.5f);
            _predatorTriangleMesh = CreateDoubleSidedTriangle(1.0f);

            Debug.Log($"Triangle meshes created: Agent={_agentTriangleMesh.vertexCount} vertices, " +
                     $"Predator={_predatorTriangleMesh.vertexCount} vertices");
        }

        private Mesh CreateDoubleSidedTriangle(float size)
        {
            Vector3[] vertices =
            {
                new Vector3(0, 0, size),
                new Vector3(-size * 0.5f, 0, -size),
                new Vector3(size * 0.5f, 0, -size),

                new Vector3(0, 0, size),
                new Vector3(size * 0.5f, 0, -size),
                new Vector3(-size * 0.5f, 0, -size)
            };

            int[] triangles = { 0, 1, 2, 3, 4, 5 };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void CreateMaterials()
        {
            _agentMaterial = new Material(Shader.Find("Standard"));
            _agentMaterial.color = new Color(0.1f, 0.6f, 1f, 0.9f);
            _agentMaterial.SetFloat("_Glossiness", 0.6f);
            _agentMaterial.SetFloat("_Metallic", 0.2f);
            _agentMaterial.enableInstancing = true;

            _predatorMaterial = new Material(Shader.Find("Standard"));
            _predatorMaterial.color = new Color(1f, 0.2f, 0.1f, 0.9f);
            _predatorMaterial.SetFloat("_Glossiness", 0.8f);
            _predatorMaterial.SetFloat("_Metallic", 0.4f);
            _predatorMaterial.enableInstancing = true;
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingleton(out RenderingConfigurationData renderingConfig))
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            _agentMatrices.Clear();
            _predatorMatrices.Clear();

            float3 cameraPosition = mainCamera.transform.position;

            ProcessAgentEntities(cameraPosition, renderingConfig);
            ProcessPredatorEntities(cameraPosition, renderingConfig);

            RenderAgentTriangles(renderingConfig);
            RenderPredatorTriangles(renderingConfig);
        }

        private void ProcessAgentEntities(float3 cameraPosition, RenderingConfigurationData config)
        {
            var agentQuery = SystemAPI.QueryBuilder()
                .WithAll<AgentTag, IsVisibleAgent, LocalTransform, AgentVisualData>()
                .Build();

            var localTransforms = agentQuery.ToComponentDataArray<LocalTransform>(
                Allocator.Temp);
            var visualDatas = agentQuery.ToComponentDataArray<AgentVisualData>(
                Allocator.Temp);

            for (int i = 0; i < localTransforms.Length; i++)
            {
                var transform = localTransforms[i];
                var visualData = visualDatas[i];

                if (config.EnableDistanceCulling)
                {
                    float distanceToCamera = math.distance(transform.Position, cameraPosition);
                    if (distanceToCamera > config.CullingDistance)
                        continue;
                }

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transform.Position,
                    transform.Rotation,
                    Vector3.one * visualData.SizeMultiplier * 0.5f
                );

                _agentMatrices.Add(matrix);
            }

            localTransforms.Dispose();
            visualDatas.Dispose();
        }

        private void ProcessPredatorEntities(float3 cameraPosition, RenderingConfigurationData config)
        {
            var predatorQuery = SystemAPI.QueryBuilder()
                .WithAll<PredatorTag, IsVisibleAgent, LocalTransform, AgentVisualData>()
                .Build();

            var localTransforms = predatorQuery.ToComponentDataArray<LocalTransform>(
                Allocator.Temp);
            var visualDatas = predatorQuery.ToComponentDataArray<AgentVisualData>(
                Allocator.Temp);

            for (int i = 0; i < localTransforms.Length; i++)
            {
                var transform = localTransforms[i];
                var visualData = visualDatas[i];

                if (config.EnableDistanceCulling)
                {
                    float distanceToCamera = math.distance(transform.Position, cameraPosition);
                    if (distanceToCamera > config.CullingDistance)
                        continue;
                }

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transform.Position,
                    transform.Rotation,
                    Vector3.one * visualData.SizeMultiplier
                );

                _predatorMatrices.Add(matrix);
            }

            localTransforms.Dispose();
            visualDatas.Dispose();
        }

        private void RenderAgentTriangles(RenderingConfigurationData config)
        {
            if (_agentMatrices.Length == 0) return;

            RenderBatch(_agentMatrices, _agentTriangleMesh, _agentMaterial, config);
        }

        private void RenderPredatorTriangles(RenderingConfigurationData config)
        {
            if (_predatorMatrices.Length == 0) return;

            RenderBatch(_predatorMatrices, _predatorTriangleMesh, _predatorMaterial, config);
        }

        private void RenderBatch(NativeList<Matrix4x4> matrices, Mesh mesh, Material material,
            RenderingConfigurationData config)
        {
            int batchCount = (matrices.Length + config.MaximumInstancesPerBatch - 1) /
                           config.MaximumInstancesPerBatch;

            Matrix4x4[] matrixArray = matrices.ToArray();

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int startIndex = batchIndex * config.MaximumInstancesPerBatch;
                int count = math.min(config.MaximumInstancesPerBatch,
                    matrixArray.Length - startIndex);

                Graphics.DrawMeshInstanced(
                    mesh,                           // 1. Mesh
                    0,                              // 2. Submesh index
                    material,                       // 3. Material
                    matrixArray,                    // 4. Matrices array
                    count,                          // 5. Count
                    null,                           // 6. Material properties (null)
                    ShadowCastingMode.Off,          // 7. Shadow casting mode
                    false,                          // 8. Receive shadows
                    0,                              // 9. Layer
                    null,                           // 10. Camera (null = main)
                    LightProbeUsage.Off,            // 11. Light probe usage
                    null                            // 12. Light probe proxy volume
                );
            }
        }

        protected override void OnDestroy()
        {
            _agentMatrices.Dispose();
            _predatorMatrices.Dispose();

            SafeDestroy(_agentTriangleMesh);
            SafeDestroy(_predatorTriangleMesh);
            SafeDestroy(_agentMaterial);
            SafeDestroy(_predatorMaterial);
        }

        private void SafeDestroy(Object obj)
        {
            if (obj != null && Application.isPlaying)
                Object.Destroy(obj);
        }
    }
}