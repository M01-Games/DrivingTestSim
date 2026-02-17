using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    private CarController carController;

    [Header("UI")]
    public TextMeshProUGUI speedText;

    [Header("Smoothing")]
    public float smoothSpeed = 5f;   // Higher = snappier, lower = smoother
    private float displayedSpeed = 0f;

    void Start()
    {
        carController = GetComponent<CarController>();
    }

    void Update()
    {
        if (carController == null || speedText == null)
            return;

        // Get speed in m/s from your CarController
        float speedMS = carController.speed;

        // Convert m/s → mph
        float speedMPH = speedMS * 2.23694f;

        // Smooth the displayed speed
        displayedSpeed = Mathf.Lerp(displayedSpeed, speedMPH, Time.deltaTime * smoothSpeed);

        // Round for clean UI
        int roundedSpeed = Mathf.RoundToInt(displayedSpeed);

        int finalSpeed = roundedSpeed / 4;

        // Update UI
        speedText.text = finalSpeed + " mph";
    }
}
