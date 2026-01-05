using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SwarmSimulation.Runtime.Systems.Behavior
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentBehaviorSystem))]
    public partial struct PredatorBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState systemState)
        {
            systemState.RequireForUpdate<SimulationConfigurationData>();
            systemState.RequireForUpdate<PredatorTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            if (!SystemAPI.TryGetSingleton(out SimulationConfigurationData simulationConfig))
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            var predatorBehaviorJob = new CalculatePredatorBehaviorJob
            {
                DeltaTime = deltaTime * simulationConfig.GlobalTimeScale,
                SimulationBoundaryCenter = simulationConfig.BoundaryCenter,
                SimulationBoundaryRadius = simulationConfig.BoundaryRadius
            }.ScheduleParallel(systemState.Dependency);

            systemState.Dependency = predatorBehaviorJob;
        }

        [BurstCompile]
        [WithAll(typeof(PredatorTag), typeof(IsActiveAgent))]
        public partial struct CalculatePredatorBehaviorJob : IJobEntity
        {
            public float DeltaTime;
            public float3 SimulationBoundaryCenter;
            public float SimulationBoundaryRadius;

            [BurstCompile]
            private void Execute(ref AgentMovementData movementData, ref PredatorBehaviorData predatorData,
                in AgentSpatialData spatialData, in DynamicBuffer<NeighborEntityBuffer> neighborBuffer,
                in LocalTransform localTransform)
            {
                float3 currentPosition = localTransform.Position;

                float3 chaseForce = CalculateChaseForce(predatorData, neighborBuffer, currentPosition);
                float3 boundaryForce = CalculateBoundaryForce(currentPosition,
                    SimulationBoundaryCenter, SimulationBoundaryRadius);

                float3 combinedForce = chaseForce + boundaryForce * 0.3f;
                UpdatePredatorVelocity(ref movementData, combinedForce, DeltaTime);
                UpdatePredatorState(ref predatorData, DeltaTime);
            }

            private float3 CalculateChaseForce(PredatorBehaviorData predatorData,
                DynamicBuffer<NeighborEntityBuffer> neighbors, float3 currentPosition)
            {
                float3 chaseDirection = float3.zero;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < neighbors.Length; i++)
                {
                    float distance = math.sqrt(neighbors[i].DistanceSquared);
                    if (distance < closestDistance && distance < predatorData.TargetLockDistance)
                    {
                        closestDistance = distance;
                        chaseDirection = neighbors[i].RelativePosition;
                    }
                }

                if (closestDistance < float.MaxValue)
                {
                    return SafeNormalize(chaseDirection);
                }

                return WanderDirection(currentPosition, DeltaTime);
            }

            private float3 CalculateBoundaryForce(float3 currentPosition,
                float3 boundaryCenter, float boundaryRadius)
            {
                float3 toCenter = boundaryCenter - currentPosition;
                float distanceToCenter = math.length(toCenter);

                if (distanceToCenter > boundaryRadius * 0.8f)
                {
                    return SafeNormalize(toCenter) * (distanceToCenter / boundaryRadius);
                }

                return float3.zero;
            }

            private void UpdatePredatorVelocity(ref AgentMovementData movementData,
                float3 desiredDirection, float deltaTime)
            {
                float3 currentDirection = movementData.CurrentDirection;
                float currentSpeed = movementData.CurrentVelocity;

                float3 newDirection = math.lerp(currentDirection, desiredDirection,
                    movementData.RotationSpeed * deltaTime);
                movementData.CurrentDirection = SafeNormalize(newDirection);

                float targetSpeed = movementData.MaximumVelocity *
                    math.clamp(math.length(desiredDirection), 0.7f, 1f);
                float acceleration = movementData.AccelerationRate * deltaTime;

                if (targetSpeed > currentSpeed)
                {
                    movementData.CurrentVelocity = math.min(currentSpeed + acceleration, targetSpeed);
                }
                else
                {
                    movementData.CurrentVelocity = math.max(currentSpeed -
                        movementData.DecelerationRate * deltaTime, targetSpeed);
                }
            }

            private void UpdatePredatorState(ref PredatorBehaviorData predatorData, float deltaTime)
            {
                predatorData.HungerLevel = math.min(predatorData.HungerLevel + deltaTime * 0.1f,
                    predatorData.MaximumHunger);
            }

            private float3 WanderDirection(float3 currentPosition, float deltaTime)
            {
                float wanderRadius = 5f;
                float wanderJitter = 1f;

                float3 wanderTarget = new float3(
                    math.sin(deltaTime * 100f + currentPosition.x) * wanderRadius,
                    0,
                    math.cos(deltaTime * 100f + currentPosition.z) * wanderRadius
                );

                wanderTarget += new float3(
                    math.sin(deltaTime * 50f) * wanderJitter,
                    0,
                    math.cos(deltaTime * 50f) * wanderJitter
                );

                return SafeNormalize(wanderTarget);
            }

            private float3 SafeNormalize(float3 vector)
            {
                float magnitude = math.length(vector);
                return magnitude > 1e-6f ? vector / magnitude : float3.zero;
            }
        }
    }
}