using SwarmSimulation.Runtime.Components.Data;
using Unity.Entities;
using UnityEngine;

namespace SwarmSimulation.Runtime.Authoring
{
    [DisallowMultipleComponent]
    public class SimulationConfigurationAuthoring : MonoBehaviour
    {
        [Header("Simulation Settings")]
        [SerializeField, Range(0.1f, 2f)] private float timeScale = 1f;
        [SerializeField] private float spatialGridSize = 15f;
        [SerializeField] private int maxNeighborsPerAgent = 64;
        [SerializeField] private float simulationBoundaryRadius = 100f;

        [Header("Rendering Settings")]
        [SerializeField] private int maxInstancesPerBatch = 1023;
        [SerializeField] private bool enableFrustumCulling = true;
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] private float cullingDistance = 200f;
        [SerializeField] private float lodThreshold = 50f;

        [Header("Debug Settings")]
        [SerializeField] private bool showSpatialGrid = false;
        [SerializeField] private int frameSmoothing = 10;

        private class SimulationConfigurationBaker : Baker<SimulationConfigurationAuthoring>
        {
            public override void Bake(SimulationConfigurationAuthoring authoring)
            {
                Entity configEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(configEntity, new SimulationConfigurationData
                {
                    GlobalTimeScale = authoring.timeScale,
                    SpatialGridCellSize = authoring.spatialGridSize,
                    MaximumNeighborsPerAgent = authoring.maxNeighborsPerAgent,
                    BoundaryRadius = authoring.simulationBoundaryRadius,
                    BoundaryCenter = authoring.transform.position,
                    EnableDebugVisualization = authoring.showSpatialGrid,
                    FrameSmoothingCount = authoring.frameSmoothing
                });

                AddComponent(configEntity, new RenderingConfigurationData
                {
                    MaximumInstancesPerBatch = authoring.maxInstancesPerBatch,
                    EnableFrustumCulling = authoring.enableFrustumCulling,
                    EnableDistanceCulling = authoring.enableDistanceCulling,
                    CullingDistance = authoring.cullingDistance,
                    LodDistanceThreshold = authoring.lodThreshold
                });
            }
        }
    }
}