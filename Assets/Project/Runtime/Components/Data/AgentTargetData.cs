using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentTargetData : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float TargetPriority;
        public float TargetReachedThreshold;
    }
}
