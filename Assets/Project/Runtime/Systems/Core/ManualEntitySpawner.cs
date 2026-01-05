using SwarmSimulation.Runtime.Components.Buffers;
using SwarmSimulation.Runtime.Components.Data;
using SwarmSimulation.Runtime.Components.Tags;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ManualEntitySpawner : MonoBehaviour
{
    [Header("=== СПАВН АГЕНТОВ ===")]
    [SerializeField] private int agentCount = 100;
    [SerializeField] private float agentSpeed = 25f;
    [SerializeField] private float agentAcceleration = 15f;
    [SerializeField] private float agentRotationSpeed = 8f;
    [SerializeField] private float agentNeighborRadius = 15f;
    [SerializeField] private float agentAvoidanceRadius = 2.5f;
    [SerializeField] private Color agentColor = Color.cyan;
    [SerializeField] private float agentSize = 0.8f;

    [Header("=== СПАВН ХИЩНИКОВ ===")]
    [SerializeField] private int predatorCount = 5;
    [SerializeField] private float predatorSpeed = 40f;
    [SerializeField] private float predatorAcceleration = 25f;
    [SerializeField] private float predatorRotationSpeed = 6f;
    [SerializeField] private float predatorDetectionRadius = 40f;
    [SerializeField] private Color predatorColor = Color.red;
    [SerializeField] private float predatorSize = 1.5f;

    [Header("=== ОБЩИЕ НАСТРОЙКИ ===")]
    [SerializeField] private float spawnRadius = 80f;
    [SerializeField] private float spawnHeight = 30f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool randomizeSpeed = true;
    [SerializeField] private float speedRandomness = 0.3f;

    [Header("=== ПОВЕДЕНИЕ ===")]
    [SerializeField] private float separationWeight = 1.8f;
    [SerializeField] private float alignmentWeight = 1.2f;
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float boundaryWeight = 0.7f;

    private EntityManager entityManager;
    private World world;
    private List<Entity> spawnedEntities;

    private void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("DOTS World не создан! Добавьте SimulationBootstrap на сцену.");
            return;
        }

        entityManager = world.EntityManager;
        spawnedEntities = new List<Entity>();

        CreateRequiredSingletons();

        if (spawnOnStart)
        {
            SpawnAllEntities();
        }

        Debug.Log($"ManualEntitySpawner готов. Используйте клавиши для управления.");
    }

    private void CreateRequiredSingletons()
    {
        if (!entityManager.CreateEntityQuery(typeof(SimulationConfigurationData))
                         .HasSingleton<SimulationConfigurationData>())
        {
            Entity configEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(configEntity, new SimulationConfigurationData
            {
                GlobalTimeScale = 1f,
                SpatialGridCellSize = 20f,
                MaximumNeighborsPerAgent = 128,
                BoundaryRadius = spawnRadius * 1.5f,
                BoundaryCenter = Vector3.zero,
                EnableDebugVisualization = false,
                FrameSmoothingCount = 10
            });
            Debug.Log("Создан SimulationConfigurationData");
        }

        if (!entityManager.CreateEntityQuery(typeof(RenderingConfigurationData))
                         .HasSingleton<RenderingConfigurationData>())
        {
            Entity renderingEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(renderingEntity, new RenderingConfigurationData
            {
                MaximumInstancesPerBatch = 1023,
                EnableFrustumCulling = false,
                EnableDistanceCulling = false,
                CullingDistance = 500f,
                LodDistanceThreshold = 100f
            });
            Debug.Log("Создан RenderingConfigurationData");
        }
    }

    private void SpawnAllEntities()
    {
        Debug.Log($"Начинаю спавн: {agentCount} агентов и {predatorCount} хищников...");

        for (int i = 0; i < agentCount; i++)
        {
            CreateAgentEntity(i);
        }

        for (int i = 0; i < predatorCount; i++)
        {
            CreatePredatorEntity(i);
        }

        Debug.Log($"Спавн завершен! Создано {agentCount} агентов и {predatorCount} хищников.");

        UpdateEntityCount();
    }

    private void CreateAgentEntity(int index)
    {
        try
        {
            Entity entity = entityManager.CreateEntity();
            spawnedEntities.Add(entity);

            float3 spawnPos = GetRandomSpawnPosition(spawnRadius, spawnHeight);
            float3 randomDirection = UnityEngine.Random.onUnitSphere;
            randomDirection.y *= 0.3f;

            var transform = LocalTransform.FromPositionRotationScale(
                spawnPos,
                quaternion.LookRotation(randomDirection, math.up()),
                1f
            );
            entityManager.AddComponentData(entity, transform);

            float speedVariation = randomizeSpeed ?
                UnityEngine.Random.Range(1f - speedRandomness, 1f + speedRandomness) : 1f;
            float finalSpeed = agentSpeed * speedVariation;
            float finalAcceleration = agentAcceleration * speedVariation;

            entityManager.AddComponentData(entity, new AgentMovementData
            {
                CurrentVelocity = finalSpeed * UnityEngine.Random.Range(0.3f, 0.8f),
                MaximumVelocity = finalSpeed,
                AccelerationRate = finalAcceleration,
                DecelerationRate = finalAcceleration * 0.6f,
                CurrentDirection = randomDirection,
                RotationSpeed = agentRotationSpeed,
                CurrentAngularVelocity = float3.zero
            });

            entityManager.AddComponentData(entity, new AgentSpatialData
            {
                NeighborDetectionRadius = agentNeighborRadius,
                CollisionAvoidanceRadius = agentAvoidanceRadius,
                PersonalSpaceRadius = agentAvoidanceRadius * 0.7f,
                CurrentGridCellHash = 0,
                PreviousPosition = spawnPos
            });

            entityManager.AddComponentData(entity, new AgentBehaviorWeights
            {
                SeparationWeight = separationWeight,
                AlignmentWeight = alignmentWeight,
                CohesionWeight = cohesionWeight,
                BoundaryAvoidanceWeight = boundaryWeight,
                TargetAttractionWeight = 0.3f,
                PredatorAvoidanceWeight = 2.5f
            });

            entityManager.AddComponentData(entity, new AgentStateData
            {
                CurrentEnergy = 100f,
                MaximumEnergy = 100f,
                EnergyConsumptionRate = 0.05f,
                FearLevel = 0f,
                AggressionLevel = 0f,
                CuriosityLevel = 1f
            });

            entityManager.AddComponentData(entity, new AgentVisualData
            {
                BaseColor = new float4(agentColor.r, agentColor.g, agentColor.b, agentColor.a),
                CurrentColor = new float4(agentColor.r, agentColor.g, agentColor.b, agentColor.a),
                ColorTransitionSpeed = 8f,
                SizeMultiplier = agentSize,
                VisibilityRange = 200f
            });

            entityManager.AddComponent<AgentTag>(entity);
            entityManager.AddComponent<IsActiveAgent>(entity);
            entityManager.SetComponentEnabled<IsActiveAgent>(entity, true);
            entityManager.AddComponent<IsVisibleAgent>(entity);
            entityManager.SetComponentEnabled<IsVisibleAgent>(entity, true);

            entityManager.AddComponent<RequiresSpatialUpdateTag>(entity);
            entityManager.AddComponent<RequiresBehaviorUpdateTag>(entity);
            entityManager.AddComponent<RequiresRenderingUpdateTag>(entity);

            DynamicBuffer<NeighborEntityBuffer> neighborBuffer =
                entityManager.AddBuffer<NeighborEntityBuffer>(entity);
            neighborBuffer.EnsureCapacity(256);

            DynamicBuffer<AgentTrailBuffer> trailBuffer =
                entityManager.AddBuffer<AgentTrailBuffer>(entity);
            trailBuffer.EnsureCapacity(30);

            if (index % 5 == 0)
            {
                trailBuffer.Add(new AgentTrailBuffer
                {
                    Position = spawnPos,
                    Color = new float4(agentColor.r, agentColor.g, agentColor.b, 0.5f),
                    RemainingLifetime = 2f
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при создании агента {index}: {e.Message}");
        }
    }

    private void CreatePredatorEntity(int index)
    {
        try
        {
            Entity entity = entityManager.CreateEntity();
            spawnedEntities.Add(entity);

            float3 spawnPos = GetRandomSpawnPosition(spawnRadius * 0.4f, spawnHeight * 0.3f);
            float3 randomDirection = UnityEngine.Random.onUnitSphere;

            var transform = LocalTransform.FromPositionRotationScale(
                spawnPos,
                quaternion.LookRotation(randomDirection, math.up()),
                1f
            );
            entityManager.AddComponentData(entity, transform);

            float speedVariation = randomizeSpeed ?
                UnityEngine.Random.Range(1f - speedRandomness * 0.5f, 1f + speedRandomness * 0.5f) : 1f;
            float finalSpeed = predatorSpeed * speedVariation;
            float finalAcceleration = predatorAcceleration * speedVariation;

            entityManager.AddComponentData(entity, new AgentMovementData
            {
                CurrentVelocity = finalSpeed * UnityEngine.Random.Range(0.5f, 0.9f),
                MaximumVelocity = finalSpeed,
                AccelerationRate = finalAcceleration,
                DecelerationRate = finalAcceleration * 0.4f,
                CurrentDirection = randomDirection,
                RotationSpeed = predatorRotationSpeed,
                CurrentAngularVelocity = float3.zero
            });

            entityManager.AddComponentData(entity, new AgentSpatialData
            {
                NeighborDetectionRadius = predatorDetectionRadius,
                CollisionAvoidanceRadius = predatorSize * 1.5f,
                PersonalSpaceRadius = predatorSize * 2f,
                CurrentGridCellHash = 0,
                PreviousPosition = spawnPos
            });

            entityManager.AddComponentData(entity, new PredatorBehaviorData
            {
                AttackRange = predatorSize * 1.2f,
                AttackCooldown = 0.8f,
                LastAttackTime = -10f,
                CurrentTarget = Entity.Null,
                TargetLockDistance = predatorDetectionRadius * 0.5f,
                HungerLevel = UnityEngine.Random.Range(0f, 30f),
                MaximumHunger = 100f
            });

            entityManager.AddComponentData(entity, new AgentBehaviorWeights
            {
                SeparationWeight = 0.5f,
                AlignmentWeight = 0.8f,
                CohesionWeight = 0.3f,
                BoundaryAvoidanceWeight = 0.2f,
                TargetAttractionWeight = 3f,
                PredatorAvoidanceWeight = 0f
            });

            entityManager.AddComponentData(entity, new AgentStateData
            {
                CurrentEnergy = 150f,
                MaximumEnergy = 150f,
                EnergyConsumptionRate = 0.15f,
                FearLevel = 0f,
                AggressionLevel = 0.8f,
                CuriosityLevel = 0.5f
            });

            entityManager.AddComponentData(entity, new AgentVisualData
            {
                BaseColor = new float4(predatorColor.r, predatorColor.g, predatorColor.b, predatorColor.a),
                CurrentColor = new float4(predatorColor.r, predatorColor.g, predatorColor.b, predatorColor.a),
                ColorTransitionSpeed = 15f,
                SizeMultiplier = predatorSize,
                VisibilityRange = 300f
            });

            entityManager.AddComponent<PredatorTag>(entity);
            entityManager.AddComponent<AgentTag>(entity);
            entityManager.AddComponent<IsActiveAgent>(entity);
            entityManager.SetComponentEnabled<IsActiveAgent>(entity, true);
            entityManager.AddComponent<IsVisibleAgent>(entity);
            entityManager.SetComponentEnabled<IsVisibleAgent>(entity, true);

            entityManager.AddComponent<RequiresSpatialUpdateTag>(entity);
            entityManager.AddComponent<RequiresBehaviorUpdateTag>(entity);
            entityManager.AddComponent<RequiresRenderingUpdateTag>(entity);

            DynamicBuffer<NeighborEntityBuffer> neighborBuffer =
                entityManager.AddBuffer<NeighborEntityBuffer>(entity);
            neighborBuffer.EnsureCapacity(512);

            entityManager.AddComponentData(entity, new AgentTargetData
            {
                TargetEntity = Entity.Null,
                TargetPosition = spawnPos + randomDirection * 20f,
                TargetPriority = 1f,
                TargetReachedThreshold = 2f
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при создании хищника {index}: {e.Message}");
        }
    }

    float3 GetRandomSpawnPosition(float radius, float height)
    {
        Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
        float yPos = UnityEngine.Random.Range(-height * 0.3f, height * 0.7f); 

        return new float3(
            circle.x,
            yPos,
            circle.y
        );
    }

    private void Update()
    {
        HandleHotkeys();

        if (Time.frameCount % 60 == 0) 
        {
            UpdateEntityCount();
        }
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            CreateAgentEntity(agentCount);
            agentCount++;
            Debug.Log($"Добавлен агент. Всего: {agentCount}");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            CreatePredatorEntity(predatorCount);
            predatorCount++;
            Debug.Log($"Добавлен хищник. Всего: {predatorCount}");
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemoveAllEntities<AgentTag>();
            agentCount = 0;
            Debug.Log("Все агенты удалены");
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            RemoveAllEntities<PredatorTag>();
            predatorCount = 0;
            Debug.Log("Все хищники удалены");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RemoveAllEntities<AgentTag>();
            RemoveAllEntities<PredatorTag>();

            StartCoroutine(RespawnAfterDelay(0.1f));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowDetailedStatistics();
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            ModifyAllSpeed(1.2f);
            Debug.Log("Скорость увеличена на 20%");
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ModifyAllSpeed(0.8f);
            Debug.Log("Скорость уменьшена на 20%");
        }
    }

    System.Collections.IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnAllEntities();
    }

    private void RemoveAllEntities<T>() where T : unmanaged, IComponentData
    {
        if (entityManager == null) return;

        var query = entityManager.CreateEntityQuery(typeof(T));
        var entities = query.ToEntityArray(Allocator.TempJob);

        foreach (var entity in entities)
        {
            entityManager.DestroyEntity(entity);
            spawnedEntities.Remove(entity);
        }

        entities.Dispose();
    }

    private void ModifyAllSpeed(float multiplier)
    {
        if (entityManager == null) return;

        using (var query = entityManager.CreateEntityQuery(typeof(AgentMovementData), typeof(AgentTag)))
        {
            var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var movement = entityManager.GetComponentData<AgentMovementData>(entity);
                movement.MaximumVelocity *= multiplier;
                movement.AccelerationRate *= multiplier;
                movement.CurrentVelocity = math.min(
                    movement.CurrentVelocity * multiplier,
                    movement.MaximumVelocity
                );
                entityManager.SetComponentData(entity, movement);
            }
            entities.Dispose();
        }

        using (var query = entityManager.CreateEntityQuery(typeof(AgentMovementData), typeof(PredatorTag)))
        {
            var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var movement = entityManager.GetComponentData<AgentMovementData>(entity);
                movement.MaximumVelocity *= multiplier;
                movement.AccelerationRate *= multiplier;
                movement.CurrentVelocity = math.min(
                    movement.CurrentVelocity * multiplier,
                    movement.MaximumVelocity
                );
                entityManager.SetComponentData(entity, movement);
            }
            entities.Dispose();
        }
    }

   private void UpdateEntityCount()
    {
        if (entityManager == null) return;

        using (var agentQuery = entityManager.CreateEntityQuery(typeof(AgentTag)))
        using (var predatorQuery = entityManager.CreateEntityQuery(typeof(PredatorTag)))
        {
            int agents = agentQuery.CalculateEntityCount();
            int predators = predatorQuery.CalculateEntityCount();
            int total = spawnedEntities.Count;

            agentCount = agents;
            predatorCount = predators;

            Debug.Log($"Сущности: {agents} агентов, {predators} хищников, всего {total}");
        }
    }

    private void ShowDetailedStatistics()
    {
        if (entityManager == null) return;

        float avgAgentSpeed = 0f;
        float avgPredatorSpeed = 0f;
        int agentSpeedCount = 0;
        int predatorSpeedCount = 0;

        using (var query = entityManager.CreateEntityQuery(typeof(AgentMovementData), typeof(AgentTag)))
        {
            var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var movement = entityManager.GetComponentData<AgentMovementData>(entity);
                avgAgentSpeed += movement.CurrentVelocity;
                agentSpeedCount++;
            }
            entities.Dispose();
        }

        using (var query = entityManager.CreateEntityQuery(typeof(AgentMovementData), typeof(PredatorTag)))
        {
            var entities = query.ToEntityArray(Allocator.TempJob);
            foreach (var entity in entities)
            {
                var movement = entityManager.GetComponentData<AgentMovementData>(entity);
                avgPredatorSpeed += movement.CurrentVelocity;
                predatorSpeedCount++;
            }
            entities.Dispose();
        }

        if (agentSpeedCount > 0) avgAgentSpeed /= agentSpeedCount;
        if (predatorSpeedCount > 0) avgPredatorSpeed /= predatorSpeedCount;

        Debug.Log($"=== ПОДРОБНАЯ СТАТИСТИКА ===");
        Debug.Log($"Агентов: {agentSpeedCount}, Средняя скорость: {avgAgentSpeed:F1}");
        Debug.Log($"Хищников: {predatorSpeedCount}, Средняя скорость: {avgPredatorSpeed:F1}");
        Debug.Log($"FPS: {1f / Time.deltaTime:F1}");
        Debug.Log($"Память: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUI.Box(new Rect(10, 10, 300, 280), "Swarm Simulation Controls");

        int y = 40;
        GUI.Label(new Rect(20, y, 280, 20), $"Агентов: {agentCount}, Хищников: {predatorCount}");
        y += 25;

        GUI.Label(new Rect(20, y, 280, 20), "A: Добавить агента");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "P: Добавить хищника");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "DEL: Удалить всех агентов");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "END: Удалить всех хищников");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "R: Перезапустить спавн");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "S: Показать статистику");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), "+/-: Изменить скорость");
        y += 20;

        if (entityManager != null)
        {
            float currentAgentSpeed = 0f;
            int sampleCount = 0;

            using (var query = entityManager.CreateEntityQuery(typeof(AgentMovementData), typeof(AgentTag)))
            {
                var entities = query.ToEntityArray(Allocator.TempJob);
                foreach (var entity in entities)
                {
                    if (sampleCount++ > 10) break;
                    var movement = entityManager.GetComponentData<AgentMovementData>(entity);
                    currentAgentSpeed += movement.CurrentVelocity;
                }
                entities.Dispose();
            }

            if (sampleCount > 0)
            {
                currentAgentSpeed /= sampleCount;
                GUI.Label(new Rect(20, y, 280, 20), $"Текущая скорость: {currentAgentSpeed:F1}");
                y += 20;
            }
        }

        GUI.Label(new Rect(20, y, 280, 20), $"FPS: {1f / Time.deltaTime:F1}");
    }

    private void OnValidate()
    {
        agentCount = Mathf.Max(0, agentCount);
        predatorCount = Mathf.Max(0, predatorCount);
        agentSpeed = Mathf.Max(0.1f, agentSpeed);
        predatorSpeed = Mathf.Max(0.1f, predatorSpeed);
        spawnRadius = Mathf.Max(1f, spawnRadius);
        spawnHeight = Mathf.Max(1f, spawnHeight);

        if (agentAcceleration < agentSpeed * 0.3f)
            agentAcceleration = agentSpeed * 0.6f;

        if (predatorAcceleration < predatorSpeed * 0.3f)
            predatorAcceleration = predatorSpeed * 0.6f;
    }

    private void OnDestroy()
    {
        spawnedEntities?.Clear();
    }
}