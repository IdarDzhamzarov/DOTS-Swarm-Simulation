using Unity.Entities;
using Unity.Mathematics;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentVisualData : IComponentData
    {
        public float4 BaseColor;
        public float4 CurrentColor;
        public float ColorTransitionSpeed;
        public float SizeMultiplier;
        public float VisibilityRange;
    }
}