using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwarmSimulation.Runtime.Systems.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AgentRenderingSystem : SystemBase
    {
        private Mesh _agentMesh;
        private Material _agentMaterial;
        private Material _predatorMaterial;

        private NativeList<Matrix4x4> _agentMatrices;
        private NativeList<Matrix4x4> _predatorMatrices;

        protected override void OnCreate()
        {
            _agentMesh = CreateArrowMesh();
            _agentMaterial = CreateAgentMaterial();
            _predatorMaterial = CreatePredatorMaterial();

            _agentMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);
            _predatorMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);

            EntityManager.CreateEntity(typeof(RenderingConfigurationData));
            SystemAPI.SetSingleton(new RenderingConfigurationData
            {
                MaximumInstancesPerBatch = 1023,
                EnableFrustumCulling = true,
                EnableDistanceCulling = true,
                CullingDistance = 200f,
                LodDistanceThreshold = 50f
            });
        }

        protected override void OnUpdate()
        {
            RenderingConfigurationData renderingConfig = SystemAPI.GetSingleton<RenderingConfigurationData>();

            _agentMatrices.Clear();
            _predatorMatrices.Clear();

            ProcessAgentEntitiesWithQuery(renderingConfig);
            ProcessPredatorEntitiesWithQuery(renderingConfig);

            RenderAgentBatch(renderingConfig);
            RenderPredatorBatch(renderingConfig);
        }

        private void ProcessAgentEntitiesWithQuery(RenderingConfigurationData renderingConfig)
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

                if (!IsEntityVisible(transform.Position, renderingConfig))
                    continue;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transform.Position,
                    transform.Rotation,
                    Vector3.one * visualData.SizeMultiplier
                );

                _agentMatrices.Add(matrix);
            }

            localTransforms.Dispose();
            visualDatas.Dispose();
        }

        private void ProcessPredatorEntitiesWithQuery(RenderingConfigurationData renderingConfig)
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

                if (!IsEntityVisible(transform.Position, renderingConfig))
                    continue;

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

        private bool IsEntityVisible(float3 position, RenderingConfigurationData config)
        {
            if (config.EnableDistanceCulling && Camera.main != null)
            {
                float distanceToCamera = math.distance(position, Camera.main.transform.position);
                if (distanceToCamera > config.CullingDistance) return false;
            }

            return true;
        }

        private void RenderAgentBatch(RenderingConfigurationData renderingConfig)
        {
            if (_agentMatrices.Length == 0) return;

            int batchCount = (_agentMatrices.Length + renderingConfig.MaximumInstancesPerBatch - 1) /
                           renderingConfig.MaximumInstancesPerBatch;

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int startIndex = batchIndex * renderingConfig.MaximumInstancesPerBatch;
                int count = math.min(renderingConfig.MaximumInstancesPerBatch,
                    _agentMatrices.Length - startIndex);

                Matrix4x4[] batchMatrices = new Matrix4x4[count];
                for (int i = 0; i < count; i++)
                {
                    batchMatrices[i] = _agentMatrices[startIndex + i];
                }

                Graphics.DrawMeshInstanced(_agentMesh, 0, _agentMaterial,
                    batchMatrices, count, null, ShadowCastingMode.Off, false);
            }
        }

        private void RenderPredatorBatch(RenderingConfigurationData renderingConfig)
        {
            if (_predatorMatrices.Length == 0) return;

            int batchCount = (_predatorMatrices.Length + renderingConfig.MaximumInstancesPerBatch - 1) /
                           renderingConfig.MaximumInstancesPerBatch;

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int startIndex = batchIndex * renderingConfig.MaximumInstancesPerBatch;
                int count = math.min(renderingConfig.MaximumInstancesPerBatch,
                    _predatorMatrices.Length - startIndex);

                Matrix4x4[] batchMatrices = new Matrix4x4[count];
                for (int i = 0; i < count; i++)
                {
                    batchMatrices[i] = _predatorMatrices[startIndex + i];
                }

                Graphics.DrawMeshInstanced(_agentMesh, 0, _predatorMaterial,
                    batchMatrices, count, null, ShadowCastingMode.Off, false);
            }
        }

        private Mesh CreateArrowMesh()
        {
            Vector3[] vertices =
            {
                new Vector3(0, 0, 1),        // Нос
                new Vector3(-0.5f, 0, -0.5f), // Левое крыло
                new Vector3(0.5f, 0, -0.5f),  // Правое крыло
                new Vector3(0, 0.5f, -0.5f),  // Верхнее крыло
                new Vector3(0, 0, -1)        // Хвост
            };

            int[] triangles =
            {
                0, 1, 2,   // Нос (нижний)
                0, 2, 3,   // Нос (правый)
                0, 3, 1,   // Нос (левый)
                1, 4, 2,   // Хвост (нижний)
                2, 4, 3,   // Хвост (правый)
                3, 4, 1    // Хвост (левый)
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Material CreateAgentMaterial()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.cyan;
            material.enableInstancing = true;
            return material;
        }

        private Material CreatePredatorMaterial()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.red;
            material.enableInstancing = true;
            return material;
        }

        protected override void OnDestroy()
        {
            _agentMatrices.Dispose();
            _predatorMatrices.Dispose();

            if (_agentMesh != null) Object.Destroy(_agentMesh);
            if (_agentMaterial != null) Object.Destroy(_agentMaterial);
            if (_predatorMaterial != null) Object.Destroy(_predatorMaterial);
        }
    }
}