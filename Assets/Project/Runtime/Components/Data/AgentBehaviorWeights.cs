using Unity.Entities;

namespace SwarmSimulation.Runtime.Components.Data
{
    public struct AgentBehaviorWeights : IComponentData
    {
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float BoundaryAvoidanceWeight;
        public float TargetAttractionWeight;
        public float PredatorAvoidanceWeight;
    }
}