using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using SwarmSimulation.Runtime.Systems.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SwarmSimulation.Runtime.Systems.Spatial
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentSpawningSystem))]
    public partial struct SpatialPartitioningSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, SpatialEntityData> _spatialGridMap;
        private NativeParallelMultiHashMap<int, SpatialEntityData> _predatorGridMap;

        [BurstCompile]
        public void OnCreate(ref SystemState systemState)
        {
            int initialCapacity = 10000;
            _spatialGridMap = new NativeParallelMultiHashMap<int, SpatialEntityData>(
                initialCapacity, Allocator.Persistent);
            _predatorGridMap = new NativeParallelMultiHashMap<int, SpatialEntityData>(
                initialCapacity, Allocator.Persistent);

            systemState.RequireForUpdate<SimulationConfigurationData>();
            systemState.RequireForUpdate<AgentTag>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState systemState)
        {
            _spatialGridMap.Dispose();
            _predatorGridMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            if (!SystemAPI.TryGetSingleton(out SimulationConfigurationData simulationConfig))
                return;

            _spatialGridMap.Clear();
            _predatorGridMap.Clear();

            EntityCommandBuffer.ParallelWriter parallelCommandBuffer = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(systemState.WorldUnmanaged).AsParallelWriter();

            var populateGridJob = new PopulateSpatialGridJob
            {
                SpatialGridMap = _spatialGridMap.AsParallelWriter(),
                PredatorGridMap = _predatorGridMap.AsParallelWriter(),
                CellSize = simulationConfig.SpatialGridCellSize
            }.ScheduleParallel(systemState.Dependency);

            var updateNeighborsJob = new UpdateNeighborRelationshipsJob
            {
                SpatialGridMap = _spatialGridMap,
                PredatorGridMap = _predatorGridMap,
                CellSize = simulationConfig.SpatialGridCellSize,
                ParallelCommandBuffer = parallelCommandBuffer
            }.ScheduleParallel(populateGridJob);

            systemState.Dependency = updateNeighborsJob;
        }

        public struct SpatialEntityData
        {
            public Entity Entity;
            public float3 Position;
            public float DetectionRadius;
        }

        [BurstCompile]
        [WithAll(typeof(AgentTag), typeof(IsActiveAgent))]
        public partial struct PopulateSpatialGridJob : IJobEntity
        {
            [WriteOnly] public NativeParallelMultiHashMap<int, SpatialEntityData>.ParallelWriter SpatialGridMap;
            [WriteOnly] public NativeParallelMultiHashMap<int, SpatialEntityData>.ParallelWriter PredatorGridMap;
            public float CellSize;

            [BurstCompile]
            private void Execute([EntityIndexInQuery] int entityIndex, Entity entity,
                in LocalTransform localTransform, in AgentSpatialData spatialData)
            {
                int gridCellHash = CalculateGridCellHash(localTransform.Position, CellSize);

                SpatialGridMap.Add(gridCellHash, new SpatialEntityData
                {
                    Entity = entity,
                    Position = localTransform.Position,
                    DetectionRadius = spatialData.NeighborDetectionRadius
                });
            }

            private int CalculateGridCellHash(float3 position, float cellSize)
            {
                int3 gridCell = (int3)math.floor(position / cellSize);
                return gridCell.x * 73856093 ^ gridCell.y * 19349663 ^ gridCell.z * 83492791;
            }
        }

        [BurstCompile]
        [WithAll(typeof(AgentTag), typeof(IsActiveAgent), typeof(RequiresSpatialUpdateTag))]
        public partial struct UpdateNeighborRelationshipsJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<int, SpatialEntityData> SpatialGridMap;
            [ReadOnly] public NativeParallelMultiHashMap<int, SpatialEntityData> PredatorGridMap;
            public float CellSize;
            public EntityCommandBuffer.ParallelWriter ParallelCommandBuffer;

            [BurstCompile]
            private void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInQuery] int entityIndex,
                Entity entity, ref DynamicBuffer<NeighborEntityBuffer> neighborBuffer,
                in LocalTransform localTransform, in AgentSpatialData spatialData)
            {
                neighborBuffer.Clear();

                int centerCellHash = CalculateGridCellHash(localTransform.Position, CellSize);
                int searchRadiusCells = (int)math.ceil(spatialData.NeighborDetectionRadius / CellSize);

                ProcessAdjacentGridCells(centerCellHash, searchRadiusCells, entity,
                    localTransform.Position, spatialData, ref neighborBuffer);

                if (neighborBuffer.Length > 0)
                {
                    ParallelCommandBuffer.RemoveComponent<RequiresSpatialUpdateTag>(chunkIndex, entity);
                }
            }

            private void ProcessAdjacentGridCells(int centerCellHash, int searchRadius,
                Entity currentEntity, float3 currentPosition, AgentSpatialData spatialData,
                ref DynamicBuffer<NeighborEntityBuffer> neighborBuffer)
            {
                float neighborRadiusSquared = spatialData.NeighborDetectionRadius *
                    spatialData.NeighborDetectionRadius;

                for (int xOffset = -searchRadius; xOffset <= searchRadius; xOffset++)
                    for (int yOffset = -searchRadius; yOffset <= searchRadius; yOffset++)
                        for (int zOffset = -searchRadius; zOffset <= searchRadius; zOffset++)
                        {
                            float3 cellOffset = new float3(xOffset, yOffset, zOffset);
                            float3 neighborCellPosition = currentPosition + cellOffset * CellSize;
                            int neighborCellHash = CalculateGridCellHash(neighborCellPosition, CellSize);

                            if (SpatialGridMap.TryGetFirstValue(neighborCellHash,
                                out SpatialEntityData neighborData,
                                out NativeParallelMultiHashMapIterator<int> iterator))
                            {
                                do
                                {
                                    if (neighborData.Entity == currentEntity) continue;

                                    float3 positionDifference = neighborData.Position - currentPosition;
                                    float distanceSquared = math.lengthsq(positionDifference);

                                    if (distanceSquared <= neighborRadiusSquared)
                                    {
                                        neighborBuffer.Add(new NeighborEntityBuffer
                                        {
                                            Entity = neighborData.Entity,
                                            DistanceSquared = distanceSquared,
                                            RelativePosition = positionDifference
                                        });
                                    }
                                }
                                while (SpatialGridMap.TryGetNextValue(out neighborData, ref iterator));
                            }
                        }
            }

            private int CalculateGridCellHash(float3 position, float cellSize)
            {
                int3 gridCell = (int3)math.floor(position / cellSize);
                return gridCell.x * 73856093 ^ gridCell.y * 19349663 ^ gridCell.z * 83492791;
            }
        }
    }
}