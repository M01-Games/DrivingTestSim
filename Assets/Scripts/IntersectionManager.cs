using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LaneType
{
    Straight,
    LeftTurn,
    RightTurn,
    StraightAndLeft,
    StraightAndRight
}

[System.Serializable]
public class LaneConfig
{
    public string laneName;
    public LaneType laneType;

    [Tooltip("Assign the TrafficLightController for this lane")]
    public TrafficLightController trafficLight;
}

[System.Serializable]
public class RoadConfig
{
    public string roadName;

    [Tooltip("All lanes belonging to this road")]
    public List<LaneConfig> lanes = new List<LaneConfig>();
}

[System.Serializable]
public class LightPhase
{
    public string phaseName;

    [Tooltip("All lanes that should be GREEN during this phase")]
    public List<LaneConfig> activeLanes = new List<LaneConfig>();

    public void SetGreen()
    {
        foreach (var lane in activeLanes)
        {
            if (lane != null && lane.trafficLight != null)
                lane.trafficLight.SetGreen();
        }
    }

    public void SetYellow()
    {
        foreach (var lane in activeLanes)
        {
            if (lane != null && lane.trafficLight != null)
                lane.trafficLight.SetYellow();
        }
    }
}

public class IntersectionManager : MonoBehaviour
{
    [Header("Road Setup")]
    [Tooltip("Add each road entering the intersection")]
    public List<RoadConfig> roads = new List<RoadConfig>();

    [Header("Traffic Light Phases")]
    [Tooltip("Each phase defines which lanes turn green together")]
    public List<LightPhase> phases = new List<LightPhase>();

    [Header("Timing (seconds)")]
    public float mingreenTime = 6.5f;
    public float maxgreenTime = 8f;
    public float greenTime = 0f;
    public float yellowTime = 2f;
    public float allRedTime = 1f;

    private int currentPhaseIndex = 0;
    private Coroutine cycleRoutine;

    void Start()
    {
        SetAllRed();

        if (phases.Count > 0)
            cycleRoutine = StartCoroutine(CycleLights());
        else
            Debug.LogWarning($"IntersectionManager on {name} has no phases configured.");
    }

    void OnDisable()
    {
        if (cycleRoutine != null)
            StopCoroutine(cycleRoutine);
    }

    IEnumerator CycleLights()
    {
        while (true)
        {
            LightPhase phase = phases[currentPhaseIndex];

            // Safety all-red period
            SetAllRed();
            yield return new WaitForSeconds(allRedTime);

            // GREEN
            phase.SetGreen();
            greenTime = Random.Range(mingreenTime, maxgreenTime);
            yield return new WaitForSeconds(greenTime);

            // YELLOW
            phase.SetYellow();
            yield return new WaitForSeconds(yellowTime);

            // Next phase
            currentPhaseIndex = (currentPhaseIndex + 1) % phases.Count;
        }
    }

    void SetAllRed()
    {
        foreach (var road in roads)
        {
            foreach (var lane in road.lanes)
            {
                if (lane.trafficLight != null)
                    lane.trafficLight.SetRed();
            }
        }
    }
}