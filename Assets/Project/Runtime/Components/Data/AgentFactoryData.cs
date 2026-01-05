using Unity.Entities;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentFactoryData : IComponentData
    {
        public Entity AgentPrefabEntity;
        public Entity PredatorPrefabEntity;
        public int InitialAgentCount;
        public int InitialPredatorCount;
        public float SpawnRadius;
        public float SpawnHeight;
        public bool UseStaggeredSpawning;
        public int SpawnBatchSize;
        public int SpawnedAgentCount;
        public int SpawnedPredatorCount;
    }
}