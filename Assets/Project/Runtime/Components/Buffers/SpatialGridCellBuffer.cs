using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Buffers
{
    public struct SpatialGridCellBuffer : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
    }
}