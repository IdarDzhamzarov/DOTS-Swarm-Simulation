using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Buffers
{
    public struct NeighborEntityBuffer : IBufferElementData
    {
        public Entity Entity;
        public float DistanceSquared;
        public float3 RelativePosition;
    }
}