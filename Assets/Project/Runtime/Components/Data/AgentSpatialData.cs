using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentSpatialData : IComponentData
    {
        public float NeighborDetectionRadius;
        public float CollisionAvoidanceRadius;
        public float PersonalSpaceRadius;
        public int CurrentGridCellHash;
        public float3 PreviousPosition;
    }
}
