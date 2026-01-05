using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using Unity.Entities;
using UnityEngine;

namespace SwarmSimulation.Runtime.Authoring
{
    [DisallowMultipleComponent]
    public class AgentFactoryAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private GameObject predatorPrefab;
        [SerializeField] private int initialAgentCount = 1000;
        [SerializeField] private int initialPredatorCount = 3;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float spawnHeight = 10f;

        private class AgentFactoryBaker : Baker<AgentFactoryAuthoring>
        {
            public override void Bake(AgentFactoryAuthoring authoring)
            {
                if (authoring.agentPrefab == null)
                {
                    Debug.LogError("Agent prefab is not assigned in AgentFactoryAuthoring!");
                    return;
                }

                var factoryEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                var agentPrefabEntity = GetEntity(authoring.agentPrefab, TransformUsageFlags.Dynamic);
                Entity predatorPrefabEntity = Entity.Null;

                if (authoring.predatorPrefab != null)
                {
                    predatorPrefabEntity = GetEntity(authoring.predatorPrefab, TransformUsageFlags.Dynamic);
                }

                AddComponent(factoryEntity, new AgentFactoryData
                {
                    AgentPrefabEntity = agentPrefabEntity,
                    PredatorPrefabEntity = predatorPrefabEntity,
                    InitialAgentCount = authoring.initialAgentCount,
                    InitialPredatorCount = authoring.initialPredatorCount,
                    SpawnRadius = authoring.spawnRadius,
                    SpawnHeight = authoring.spawnHeight,
                    UseStaggeredSpawning = true,
                    SpawnBatchSize = 100,
                    SpawnedAgentCount = 0,
                    SpawnedPredatorCount = 0
                });

                AddComponent<RequiresAgentSpawningTag>(factoryEntity);

                Debug.Log($"AgentFactory baked: {authoring.initialAgentCount} agents, " +
                         $"{authoring.initialPredatorCount} predators");
            }
        }
    }
}