using UnityEngine;
using System.Collections;
using UnityStandardAssets_ImageEffects;

public class CameraController : MonoBehaviour
{

    public PlayerController playerController;
    private Vector3 velocity = Vector3.zero;
    private float firstXDistance;
    private float firstZDistance;
    private float currentXDistance;
    private float currentZDistance;

    private Camera mainCamera;
    private float safeZonePercentage = 0.8f; // Tỉ lệ safezone, giá trị từ 0 đến 1
    public float scaleValue;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();

        // Tính toán kích thước safezone dựa trên kích thước màn hình và tỉ lệ safezone
        float safeWidth = Screen.width * safeZonePercentage;
        float safeHeight = Screen.height * safeZonePercentage;
        if (2 * safeHeight > 3 * safeWidth)
        {
            scaleValue = 40f;
        }
        else
        {
            scaleValue = 60f;
        }
        // Tính toán kích thước camera dựa trên kích thước safezone
        float cameraSize = Mathf.Max(safeWidth, safeHeight) / scaleValue;

        mainCamera.orthographicSize = cameraSize;
    }


    void Start()
    {
        firstXDistance = transform.position.x - playerController.transform.position.x;
        firstZDistance = transform.position.z - playerController.transform.position.z;
    }

    void Update()
    {
        if (playerController.isRunning && !playerController.gameManager.gameOver)
        {
            Vector3 pos = transform.position;
            pos.x = playerController.transform.position.x + firstXDistance;
            pos.z = playerController.transform.position.z + firstZDistance;
            transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, 0.1f);
        }
    }

    public void ResetPosition()
    {
        Vector3 pos = transform.position;
        pos.x = playerController.transform.position.x + firstXDistance;
        pos.z = playerController.transform.position.z + firstZDistance;

        transform.position = pos;
    }
}
