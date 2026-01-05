using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SwarmSimulation.Runtime.Authoring
{
    [DisallowMultipleComponent]
    public class PredatorConfigurationAuthoring : MonoBehaviour
    {
        [SerializeField, Range(1f, 15f)] private float chaseSpeed = 8f;
        [SerializeField, Range(10f, 100f)] private float detectionRange = 30f;
        [SerializeField, Range(0.5f, 5f)] private float attackCooldown = 1f;
        [SerializeField] private Color predatorColor = Color.red;
        [SerializeField, Range(1f, 5f)] private float sizeMultiplier = 2f;

        private class PredatorConfigurationBaker : Baker<PredatorConfigurationAuthoring>
        {
            public override void Bake(PredatorConfigurationAuthoring authoring)
            {
                Entity predatorEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(predatorEntity, new AgentMovementData
                {
                    CurrentVelocity = 0f,
                    MaximumVelocity = authoring.chaseSpeed,
                    AccelerationRate = 3f,
                    DecelerationRate = 4.5f,
                    CurrentDirection = math.forward(),
                    RotationSpeed = 2f,
                    CurrentAngularVelocity = float3.zero
                });

                AddComponent(predatorEntity, new AgentSpatialData
                {
                    NeighborDetectionRadius = authoring.detectionRange,
                    CollisionAvoidanceRadius = 3f,
                    PersonalSpaceRadius = 5f,
                    CurrentGridCellHash = 0,
                    PreviousPosition = float3.zero
                });

                AddComponent(predatorEntity, new PredatorBehaviorData
                {
                    AttackRange = 2f,
                    AttackCooldown = authoring.attackCooldown,
                    LastAttackTime = -authoring.attackCooldown,
                    CurrentTarget = Entity.Null,
                    TargetLockDistance = 5f,
                    HungerLevel = 0f,
                    MaximumHunger = 100f
                });

                AddComponent(predatorEntity, new AgentVisualData
                {
                    BaseColor = new float4(authoring.predatorColor.r, authoring.predatorColor.g,
                                          authoring.predatorColor.b, authoring.predatorColor.a),
                    CurrentColor = new float4(authoring.predatorColor.r, authoring.predatorColor.g,
                                            authoring.predatorColor.b, authoring.predatorColor.a),
                    ColorTransitionSpeed = 10f,
                    SizeMultiplier = authoring.sizeMultiplier,
                    VisibilityRange = 150f
                });

                AddComponent<PredatorTag>(predatorEntity);
                AddComponent<IsActiveAgent>(predatorEntity);
                AddComponent<IsVisibleAgent>(predatorEntity);
                AddComponent<RequiresSpatialUpdateTag>(predatorEntity);
                AddComponent<RequiresBehaviorUpdateTag>(predatorEntity);
                AddComponent<RequiresRenderingUpdateTag>(predatorEntity);

                DynamicBuffer<NeighborEntityBuffer> neighborBuffer =
                    AddBuffer<NeighborEntityBuffer>(predatorEntity);
                neighborBuffer.EnsureCapacity(256);
            }
        }
    }
}