using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmSimulation.Runtime.Authoring
{
    public class TestAgentAuthoring : MonoBehaviour
    {
        [SerializeField] private bool isPredator = false;

        private class TestAgentBaker : Baker<TestAgentAuthoring>
        {
            public override void Bake(TestAgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Базовые компоненты
                AddComponent(entity, new AgentMovementData
                {
                    CurrentVelocity = 0f,
                    MaximumVelocity = authoring.isPredator ? 8f : 5f,
                    AccelerationRate = 2f,
                    DecelerationRate = 3f,
                    CurrentDirection = math.forward(),
                    RotationSpeed = 3f,
                    CurrentAngularVelocity = float3.zero
                });

                AddComponent(entity, new AgentSpatialData
                {
                    NeighborDetectionRadius = authoring.isPredator ? 30f : 10f,
                    CollisionAvoidanceRadius = 2f,
                    PersonalSpaceRadius = 1.5f,
                    CurrentGridCellHash = 0,
                    PreviousPosition = float3.zero
                });

                AddComponent(entity, new AgentVisualData
                {
                    BaseColor = authoring.isPredator ? new float4(1, 0, 0, 1) : new float4(0, 1, 1, 1),
                    CurrentColor = authoring.isPredator ? new float4(1, 0, 0, 1) : new float4(0, 1, 1, 1),
                    ColorTransitionSpeed = 5f,
                    SizeMultiplier = authoring.isPredator ? 1.5f : 1f,
                    VisibilityRange = 100f
                });

                // Теги
                if (authoring.isPredator)
                {
                    AddComponent<PredatorTag>(entity);
                }
                else
                {
                    AddComponent<AgentTag>(entity);
                }

                AddComponent<IsActiveAgent>(entity);
                AddComponent<IsVisibleAgent>(entity);
                AddComponent<RequiresSpatialUpdateTag>(entity);
                AddComponent<RequiresBehaviorUpdateTag>(entity);
            }
        }
    }
}