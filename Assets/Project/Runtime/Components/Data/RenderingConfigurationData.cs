using Unity.Entities;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct RenderingConfigurationData : IComponentData
    {
        public int MaximumInstancesPerBatch;
        public bool EnableFrustumCulling;
        public bool EnableDistanceCulling;
        public float CullingDistance;
        public float LodDistanceThreshold;
    }
}