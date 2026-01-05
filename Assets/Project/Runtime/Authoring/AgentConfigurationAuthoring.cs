using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmSimulation.Runtime.Authoring
{
    [DisallowMultipleComponent]
    public class AgentConfigurationAuthoring : MonoBehaviour
    {
        [Header("Movement Configuration")]
        [SerializeField, Range(0.1f, 20f)] private float baseSpeed = 5f;
        [SerializeField, Range(0.1f, 10f)] private float rotationSpeed = 3f;
        [SerializeField, Range(0.1f, 10f)] private float acceleration = 2f;

        [Header("Spatial Configuration")]
        [SerializeField, Range(1f, 50f)] private float neighborRadius = 10f;
        [SerializeField, Range(0.5f, 10f)] private float avoidanceRadius = 2f;

        [Header("Behavior Configuration")]
        [SerializeField, Range(0f, 5f)] private float separationWeight = 1.5f;
        [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
        [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;
        [SerializeField, Range(0f, 5f)] private float boundaryWeight = 0.5f;

        [Header("Visual Configuration")]
        [SerializeField] private Color agentColor = Color.cyan;
        [SerializeField, Range(0.5f, 3f)] private float sizeMultiplier = 1f;

        private class AgentConfigurationBaker : Baker<AgentConfigurationAuthoring>
        {
            public override void Bake(AgentConfigurationAuthoring authoring)
            {
                Entity agentEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(agentEntity, new AgentMovementData
                {
                    CurrentVelocity = 0f,
                    MaximumVelocity = authoring.baseSpeed,
                    AccelerationRate = authoring.acceleration,
                    DecelerationRate = authoring.acceleration * 1.5f,
                    CurrentDirection = math.forward(),
                    RotationSpeed = authoring.rotationSpeed,
                    CurrentAngularVelocity = float3.zero
                });

                AddComponent(agentEntity, new AgentSpatialData
                {
                    NeighborDetectionRadius = authoring.neighborRadius,
                    CollisionAvoidanceRadius = authoring.avoidanceRadius,
                    PersonalSpaceRadius = authoring.avoidanceRadius * 0.7f,
                    CurrentGridCellHash = 0,
                    PreviousPosition = float3.zero
                });

                AddComponent(agentEntity, new AgentBehaviorWeights
                {
                    SeparationWeight = authoring.separationWeight,
                    AlignmentWeight = authoring.alignmentWeight,
                    CohesionWeight = authoring.cohesionWeight,
                    BoundaryAvoidanceWeight = authoring.boundaryWeight,
                    TargetAttractionWeight = 0.3f,
                    PredatorAvoidanceWeight = 2f
                });

                AddComponent(agentEntity, new AgentStateData
                {
                    CurrentEnergy = 100f,
                    MaximumEnergy = 100f,
                    EnergyConsumptionRate = 0.1f,
                    FearLevel = 0f,
                    AggressionLevel = 0f,
                    CuriosityLevel = 1f
                });

                AddComponent(agentEntity, new AgentVisualData
                {
                    BaseColor = new float4(authoring.agentColor.r, authoring.agentColor.g,
                                          authoring.agentColor.b, authoring.agentColor.a),
                    CurrentColor = new float4(authoring.agentColor.r, authoring.agentColor.g,
                                            authoring.agentColor.b, authoring.agentColor.a),
                    ColorTransitionSpeed = 5f,
                    SizeMultiplier = authoring.sizeMultiplier,
                    VisibilityRange = 100f
                });

                AddComponent<AgentTag>(agentEntity);
                AddComponent<IsActiveAgent>(agentEntity);
                AddComponent<IsVisibleAgent>(agentEntity);
                AddComponent<RequiresSpatialUpdateTag>(agentEntity);
                AddComponent<RequiresBehaviorUpdateTag>(agentEntity);
                AddComponent<RequiresRenderingUpdateTag>(agentEntity);

                DynamicBuffer<NeighborEntityBuffer> neighborBuffer =
                    AddBuffer<NeighborEntityBuffer>(agentEntity);
                neighborBuffer.EnsureCapacity(128);

                DynamicBuffer<AgentTrailBuffer> trailBuffer =
                    AddBuffer<AgentTrailBuffer>(agentEntity);
                trailBuffer.EnsureCapacity(20);
            }
        }
    }
}