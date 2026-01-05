using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct SimulationConfigurationData : IComponentData
    {
        public float GlobalTimeScale;
        public float SpatialGridCellSize;
        public int MaximumNeighborsPerAgent;
        public float BoundaryRadius;
        public float3 BoundaryCenter;
        public bool EnableDebugVisualization;
        public int FrameSmoothingCount;
    }
}