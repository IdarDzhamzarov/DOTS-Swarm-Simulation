using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using SwarmSimulation.Runtime.Systems.Spatial;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SwarmSimulation.Runtime.Systems.Behavior
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialPartitioningSystem))]
    public partial struct AgentBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState systemState)
        {
            systemState.RequireForUpdate<SimulationConfigurationData>();
            systemState.RequireForUpdate<AgentTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            if (!SystemAPI.TryGetSingleton(out SimulationConfigurationData simulationConfig))
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer.ParallelWriter parallelCommandBuffer = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(systemState.WorldUnmanaged).AsParallelWriter();

            var behaviorCalculationJob = new CalculateAgentBehaviorJob
            {
                DeltaTime = deltaTime * simulationConfig.GlobalTimeScale,
                SimulationBoundaryCenter = simulationConfig.BoundaryCenter,
                SimulationBoundaryRadius = simulationConfig.BoundaryRadius,
                ParallelCommandBuffer = parallelCommandBuffer
            }.ScheduleParallel(systemState.Dependency);

            var movementApplicationJob = new ApplyAgentMovementJob
            {
                DeltaTime = deltaTime * simulationConfig.GlobalTimeScale
            }.ScheduleParallel(behaviorCalculationJob);

            systemState.Dependency = movementApplicationJob;
        }

        [BurstCompile]
        [WithAll(typeof(AgentTag), typeof(IsActiveAgent), typeof(RequiresBehaviorUpdateTag))]
        public partial struct CalculateAgentBehaviorJob : IJobEntity
        {
            public float DeltaTime;
            public float3 SimulationBoundaryCenter;
            public float SimulationBoundaryRadius;
            public EntityCommandBuffer.ParallelWriter ParallelCommandBuffer;

            [BurstCompile]
            private void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInQuery] int entityIndex,
                Entity entity, ref AgentMovementData movementData, ref AgentStateData stateData,
                in AgentBehaviorWeights behaviorWeights, in AgentSpatialData spatialData,
                in DynamicBuffer<NeighborEntityBuffer> neighborBuffer, in LocalTransform localTransform)
            {
                float3 currentPosition = localTransform.Position;
                float3 currentVelocity = movementData.CurrentDirection * movementData.CurrentVelocity;

                float3 separationForce = CalculateSeparationForce(currentPosition, spatialData, neighborBuffer);
                float3 alignmentForce = CalculateAlignmentForce(currentVelocity, neighborBuffer);
                float3 cohesionForce = CalculateCohesionForce(currentPosition, neighborBuffer);
                float3 boundaryForce = CalculateBoundaryForce(currentPosition,
                    SimulationBoundaryCenter, SimulationBoundaryRadius);

                float3 combinedForce = CombineBehaviorForces(separationForce, alignmentForce,
                    cohesionForce, boundaryForce, behaviorWeights, stateData.FearLevel);

                UpdateAgentVelocity(ref movementData, combinedForce, DeltaTime);
                UpdateAgentState(ref stateData, DeltaTime);

                ParallelCommandBuffer.RemoveComponent<RequiresBehaviorUpdateTag>(chunkIndex, entity);
                ParallelCommandBuffer.AddComponent<RequiresSpatialUpdateTag>(chunkIndex, entity);
            }

            [BurstCompile]
            private float3 CalculateSeparationForce(float3 currentPosition,
                AgentSpatialData spatialData, DynamicBuffer<NeighborEntityBuffer> neighbors)
            {
                float3 separationVector = float3.zero;
                int separationCount = 0;
                float personalSpaceSquared = spatialData.PersonalSpaceRadius *
                    spatialData.PersonalSpaceRadius;

                for (int i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i].DistanceSquared < personalSpaceSquared)
                    {
                        separationVector += neighbors[i].RelativePosition;
                        separationCount++;
                    }
                }

                if (separationCount > 0)
                {
                    separationVector = SafeNormalize(separationVector / separationCount) * -1f;
                }

                return separationVector;
            }

            [BurstCompile]
            private float3 CalculateAlignmentForce(float3 currentVelocity,
                DynamicBuffer<NeighborEntityBuffer> neighbors)
            {
                if (neighbors.Length == 0) return float3.zero;

                float3 averageVelocity = float3.zero;
                for (int i = 0; i < neighbors.Length; i++)
                {
                    averageVelocity += currentVelocity;
                }

                averageVelocity /= neighbors.Length;
                return SafeNormalize(averageVelocity);
            }

            [BurstCompile]
            private float3 CalculateCohesionForce(float3 currentPosition,
                DynamicBuffer<NeighborEntityBuffer> neighbors)
            {
                if (neighbors.Length == 0) return float3.zero;

                float3 centerOfMass = float3.zero;
                for (int i = 0; i < neighbors.Length; i++)
                {
                    centerOfMass += currentPosition + neighbors[i].RelativePosition;
                }

                centerOfMass /= neighbors.Length;
                return SafeNormalize(centerOfMass - currentPosition);
            }

            [BurstCompile]
            private float3 CalculateBoundaryForce(float3 currentPosition,
                float3 boundaryCenter, float boundaryRadius)
            {
                float3 toCenter = boundaryCenter - currentPosition;
                float distanceToCenter = math.length(toCenter);

                if (distanceToCenter > boundaryRadius * 0.9f)
                {
                    return SafeNormalize(toCenter) * (distanceToCenter / boundaryRadius);
                }

                return float3.zero;
            }

            [BurstCompile]
            private float3 CombineBehaviorForces(float3 separation, float3 alignment,
                float3 cohesion, float3 boundary, AgentBehaviorWeights weights, float fearLevel)
            {
                float3 combinedForce = separation * weights.SeparationWeight +
                                      alignment * weights.AlignmentWeight +
                                      cohesion * weights.CohesionWeight +
                                      boundary * weights.BoundaryAvoidanceWeight;

                combinedForce *= math.max(1f - fearLevel * 0.5f, 0.5f);
                return SafeNormalize(combinedForce);
            }

            [BurstCompile]
            private void UpdateAgentVelocity(ref AgentMovementData movementData,
                float3 desiredDirection, float deltaTime)
            {
                float3 currentDirection = movementData.CurrentDirection;
                float currentSpeed = movementData.CurrentVelocity;

                float3 newDirection = math.lerp(currentDirection, desiredDirection,
                    movementData.RotationSpeed * deltaTime);
                movementData.CurrentDirection = SafeNormalize(newDirection);

                float targetSpeed = movementData.MaximumVelocity *
                    math.clamp(math.length(desiredDirection), 0.5f, 1f);
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

            [BurstCompile]
            private void UpdateAgentState(ref AgentStateData stateData, float deltaTime)
            {
                stateData.CurrentEnergy = math.max(stateData.CurrentEnergy -
                    stateData.EnergyConsumptionRate * deltaTime, 0f);
                stateData.FearLevel = math.max(stateData.FearLevel - deltaTime * 0.5f, 0f);
            }

            [BurstCompile]
            private float3 SafeNormalize(float3 vector)
            {
                float magnitude = math.length(vector);
                return magnitude > 1e-6f ? vector / magnitude : float3.zero;
            }
        }

        [BurstCompile]
        [WithAll(typeof(AgentTag), typeof(IsActiveAgent))]
        public partial struct ApplyAgentMovementJob : IJobEntity
        {
            public float DeltaTime;

            [BurstCompile]
            private void Execute(ref LocalTransform localTransform, in AgentMovementData movementData)
            {
                float3 newPosition = localTransform.Position +
                    movementData.CurrentDirection * movementData.CurrentVelocity * DeltaTime;

                quaternion targetRotation = quaternion.LookRotation(movementData.CurrentDirection, math.up());
                quaternion newRotation = math.slerp(localTransform.Rotation, targetRotation,
                    movementData.RotationSpeed * DeltaTime);

                localTransform.Position = newPosition;
                localTransform.Rotation = newRotation;
            }
        }
    }
}