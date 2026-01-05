using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using SwarmSimulation.Runtime.Components.Tags;

namespace SwarmSimulation.Runtime
{
    public class DemoController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float cameraSpeed = 10f;
        [SerializeField] private float cameraRotationSpeed = 2f;
        [SerializeField] private float cameraZoomSpeed = 5f;

        private EntityManager entityManager;
        private Vector3 cameraRotation;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (mainCamera == null)
                mainCamera = Camera.main;

            cameraRotation = mainCamera.transform.eulerAngles;
        }

        private void Update()
        {
            HandleCameraMovement();
            HandleAgentCommands();
            UpdateCameraPosition();
        }

        private void HandleCameraMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float zoom = Input.GetAxis("Mouse ScrollWheel");

            Vector3 moveDirection = new Vector3(horizontal, 0, vertical);
            mainCamera.transform.position += mainCamera.transform.TransformDirection(moveDirection) *
                cameraSpeed * Time.deltaTime;

            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * cameraRotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * cameraRotationSpeed;

                cameraRotation.y += mouseX;
                cameraRotation.x -= mouseY;
                cameraRotation.x = Mathf.Clamp(cameraRotation.x, -80f, 80f);
            }

            mainCamera.transform.position += mainCamera.transform.forward * zoom * cameraZoomSpeed;
        }

        private void HandleAgentCommands()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleAgentActivity();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetSimulation();
            }
        }

        private void ToggleAgentActivity()
        {
            var query = entityManager.CreateEntityQuery(typeof(AgentTag), typeof(IsActiveAgent));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                var isActive = entityManager.IsComponentEnabled<IsActiveAgent>(entity);
                entityManager.SetComponentEnabled<IsActiveAgent>(entity, !isActive);
            }

            entities.Dispose();
        }

        private void ResetSimulation()
        {
            var query = entityManager.CreateEntityQuery(typeof(AgentTag));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<LocalTransform>(entity))
                {
                    var randomPos = Random.insideUnitSphere * 50f;
                    randomPos.y = Mathf.Abs(randomPos.y);

                    var transform = entityManager.GetComponentData<LocalTransform>(entity);
                    transform.Position = randomPos;
                    entityManager.SetComponentData(entity, transform);
                }
            }

            entities.Dispose();
        }

        private void UpdateCameraPosition()
        {
            mainCamera.transform.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 300, 150), "Swarm Simulation Controls");

            GUI.Label(new Rect(20, 40, 280, 20), "WASD: Move Camera");
            GUI.Label(new Rect(20, 60, 280, 20), "Right Mouse: Rotate Camera");
            GUI.Label(new Rect(20, 80, 280, 20), "Scroll Wheel: Zoom");
            GUI.Label(new Rect(20, 100, 280, 20), "SPACE: Toggle Agents");
            GUI.Label(new Rect(20, 120, 280, 20), "R: Reset Positions");

            var agentQuery = entityManager.CreateEntityQuery(typeof(AgentTag), typeof(IsActiveAgent));
            var predatorQuery = entityManager.CreateEntityQuery(typeof(PredatorTag), typeof(IsActiveAgent));

            GUI.Label(new Rect(20, 140, 280, 20),
                $"Agents: {agentQuery.CalculateEntityCount()} | Predators: {predatorQuery.CalculateEntityCount()}");
        }
    }
}