using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    public void SetRed()
    {
        redLight.SetActive(true);
        yellowLight.SetActive(false);
        greenLight.SetActive(false);
    }

    public void SetYellow()
    {
        redLight.SetActive(false);
        yellowLight.SetActive(true);
        greenLight.SetActive(false);
    }

    public void SetGreen()
    {
        redLight.SetActive(false);
        yellowLight.SetActive(false);
        greenLight.SetActive(true);
    }
}
