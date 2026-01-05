using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentMovementData : IComponentData
    {
        public float CurrentVelocity;
        public float MaximumVelocity;
        public float AccelerationRate;
        public float DecelerationRate;
        public float3 CurrentDirection;
        public float RotationSpeed;
        public float3 CurrentAngularVelocity;
    }
}