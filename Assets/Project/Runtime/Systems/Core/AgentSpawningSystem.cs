using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
using UnityEngine;

namespace SwarmSimulation.Runtime.Systems.Core
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AgentSpawningSystem : ISystem
    {
        private Random _randomGenerator;

        [BurstCompile]
        public void OnCreate(ref SystemState systemState)
        {
            _randomGenerator = Random.CreateFromIndex(1);
            systemState.RequireForUpdate<AgentFactoryData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            foreach (var (factoryData, factoryEntity) in
                     SystemAPI.Query<RefRW<AgentFactoryData>>()
                     .WithAll<RequiresAgentSpawningTag>()
                     .WithEntityAccess())
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

                int agentsToSpawn = factoryData.ValueRW.InitialAgentCount - factoryData.ValueRW.SpawnedAgentCount;
                if (agentsToSpawn > 0)
                {
                    Debug.Log($"Spawning {agentsToSpawn} agents...");

                    for (int i = 0; i < agentsToSpawn; i++)
                    {
                        SpawnAgent(ref factoryData.ValueRW, ecb);
                    }
                    factoryData.ValueRW.SpawnedAgentCount += agentsToSpawn;
                }

                if (factoryData.ValueRW.PredatorPrefabEntity != Entity.Null)
                {
                    int predatorsToSpawn = factoryData.ValueRW.InitialPredatorCount -
                                          factoryData.ValueRW.SpawnedPredatorCount;
                    if (predatorsToSpawn > 0)
                    {
                        Debug.Log($"Spawning {predatorsToSpawn} predators...");

                        for (int i = 0; i < predatorsToSpawn; i++)
                        {
                            SpawnPredator(ref factoryData.ValueRW, ecb);
                        }
                        factoryData.ValueRW.SpawnedPredatorCount += predatorsToSpawn;
                    }
                }

                ecb.Playback(systemState.EntityManager);
                ecb.Dispose();

                if (factoryData.ValueRW.SpawnedAgentCount >= factoryData.ValueRW.InitialAgentCount &&
                    factoryData.ValueRW.SpawnedPredatorCount >= factoryData.ValueRW.InitialPredatorCount)
                {
                    systemState.EntityManager.RemoveComponent<RequiresAgentSpawningTag>(factoryEntity);
                    Debug.Log($"Spawning complete: {factoryData.ValueRW.SpawnedAgentCount} agents, " +
                            $"{factoryData.ValueRW.SpawnedPredatorCount} predators");
                }
            }
        }

        private void SpawnAgent(ref AgentFactoryData factoryData, EntityCommandBuffer ecb)
        {
            Entity agent = ecb.Instantiate(factoryData.AgentPrefabEntity);

            float3 position = GetRandomSpawnPosition(factoryData.SpawnRadius, factoryData.SpawnHeight);
            quaternion rotation = quaternion.LookRotation(_randomGenerator.NextFloat3Direction(), math.up());

            ecb.SetComponent(agent, new LocalTransform
            {
                Position = position,
                Rotation = rotation,
                Scale = 1f
            });
        }

        private void SpawnPredator(ref AgentFactoryData factoryData, EntityCommandBuffer ecb)
        {
            Entity predator = ecb.Instantiate(factoryData.PredatorPrefabEntity);

            float3 position = GetRandomSpawnPosition(factoryData.SpawnRadius * 0.3f,
                                                     factoryData.SpawnHeight * 0.5f);
            quaternion rotation = quaternion.LookRotation(_randomGenerator.NextFloat3Direction(), math.up());

            ecb.SetComponent(predator, new LocalTransform
            {
                Position = position,
                Rotation = rotation,
                Scale = 2f
            });
        }

        private float3 GetRandomSpawnPosition(float radius, float height)
        {
            float2 circle = _randomGenerator.NextFloat2Direction() * _randomGenerator.NextFloat(0, radius);
            return new float3(circle.x, _randomGenerator.NextFloat(-height, height), circle.y);
        }
    }
}