using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Buffers
{
    public struct AgentTrailBuffer : IBufferElementData
    {
        public float3 Position;
        public float4 Color;
        public float RemainingLifetime;
    }
}