using UnityEngine;
using Cinemachine;

public class AimPositionDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] public Transform debugTransform; // Assign a sphere transform in inspector
    [SerializeField] private LayerMask aimColliderMask = Physics.DefaultRaycastLayers;
    [SerializeField] private CinemachineVirtualCamera virtualCamera; // Assign your Cinemachine camera
    private Camera mainCamera;

    private void Awake()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        if (virtualCamera != null)
        {
            mainCamera = virtualCamera.VirtualCameraGameObject.GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (debugTransform == null || mainCamera == null) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimColliderMask))
        {
            debugTransform.position = hit.point;
            debugTransform.rotation = Quaternion.LookRotation(hit.normal);
        }
        else
        {
            debugTransform.position = ray.GetPoint(1000f);
        }
    }
}
