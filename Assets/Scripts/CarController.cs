using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum GearState
{
    Neutral,
    Running,
    CheckingChange,
    Changing,
    Park
};

public class CarController : MonoBehaviour
{
    private Rigidbody playerRB;

    public WheelColliders colliders;
    //public WheelGameObjects wheelgameobjects;

    public float gasInput;
    public float brakeInput;
    public float steeringInput;
    public bool enabledPark;
    //public Mybutton gasPedal;
    //public Mybutton brakePedal;
    public float motorPower;
    public float brakePower;
    public float slipAngle;
    public float speed;
    private float speedClamped;
    public float maxSpeed;
    public AnimationCurve steeringCurve;

    public int isEngineRunning;

    public float RPM;
    public float redLine;
    public float idleRPM;
    public TMP_Text rpmText;
    public TMP_Text gearText;
    public Transform rpmNeedle;
    public float minNeedleRotation;
    public float maxNeedleRotation;
    public int currentGear;

    public float[] gearRatios;
    public float differentialRatio;
    private float currentTorque;
    private float clutch;
    private float wheelRPM;
    public AnimationCurve hpToRPMCurve;
    public GearState gearState;
    public float increaseGearRPM;
    public float decreaseGearRPM;
    public float changeGearTime = 0.5f;

    [Header("Steering Wheel")]
    private float lastSteeringAngle;
    public Transform steeringWheel;
    public float maxWheelRotation = 120f;
    public float steeringWheelSmooth = 3f;
    private Quaternion steeringWheelStartRot;   // stores original rotation
    private float steeringWheelCurrent = 0f;

    [Header("Indicators")]
    public UnityEngine.UI.RawImage leftIndicatorUI;
    public UnityEngine.UI.RawImage rightIndicatorUI;
    private bool leftIndicator;
    private bool rightIndicator;
    private float indicatorTimer;
    public float indicatorFlashSpeed = 0.5f; // seconds per flash

    // Start is called before the first frame update
    void Start()
    {
        playerRB = gameObject.GetComponent<Rigidbody>();
        if (steeringWheel != null) 
            steeringWheelStartRot = steeringWheel.localRotation; // save original rotation
    }

    // Update is called once per frame
    void Update()
    {
        rpmNeedle.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(minNeedleRotation, maxNeedleRotation, RPM / (redLine * 1.1f)));
        rpmText.text = RPM.ToString("0,000")+"rpm";

        gearText.text = 
            (gearState == GearState.Park) ? "P" :
            (gearState == GearState.Neutral) ? "N" : 
            (currentGear + 1).ToString();

        speed = colliders.RRWheel.rpm * colliders.RRWheel.radius * 2f * Mathf.PI / 10f;
        speedClamped = Mathf.Lerp(speedClamped, speed, Time.deltaTime);

        CheckInput();
        ApplyMotor();
        ApplySteering();
        ApplyBrake();
        UpdateSteeringWheel();
        UpdateIndicators();

        //ApplyWheelPositions();
    }
    
    void CheckInput()
    {
        gasInput = Input.GetAxis("Vertical");

        steeringInput = Input.GetAxis("Horizontal");

        slipAngle = Vector3.Angle(transform.forward, playerRB.linearVelocity-transform.forward);

        // Auto-cancel LEFT indicator when steering right
        if (leftIndicator && lastSteeringAngle > 15f)
        {
            leftIndicator = false;
            leftIndicatorUI.enabled = false;
        }

        // Auto-cancel RIGHT indicator when steering left
        if (rightIndicator && lastSteeringAngle < -15f)
        {
            rightIndicator = false;
            rightIndicatorUI.enabled = false;
        }

        // Toggle left indicator (Q or LB)
        if (Input.GetButtonDown("Fire2"))
        {
            leftIndicator = !leftIndicator;
            rightIndicator = false; // can't have both unless hazards
        }
        // Toggle right indicator (E or RB)
        if (Input.GetButtonDown("Fire3"))
        {
            rightIndicator = !rightIndicator;
            leftIndicator = false;
        }
        
        // Toggle Park with left click or A on controller 
        if (Input.GetButtonDown("Fire1")) 
        { 
            if (gearState == GearState.Park)
                SetPark(false);
            else 
                SetPark(true);
        }

        if (gearState == GearState.Park)
        {
            gasInput = 0;
            steeringInput = 0;
            brakeInput = 1f;
            return; // stop the rest of CheckInput()
        }

        if (Mathf.Abs(gasInput) > 0 && isEngineRunning == 0)
        {
            StartCoroutine(GetComponent<EngineAudio>().StartEngine());
            gearState = GearState.Running;

        }

        float movingDirection = Vector3.Dot(transform.forward, playerRB.linearVelocity);
        if (gearState != GearState.Changing)
        {
            if (gearState == GearState.Neutral)
            {
                clutch = 0; 
                if (Mathf.Abs( gasInput )> 0) gearState = GearState.Running;
            }
            else
            {
            clutch = Input.GetKey(KeyCode.LeftShift) ? 0 : Mathf.Lerp(clutch, 1, Time.deltaTime);
            }
        }
        else
        {
            clutch = 0;
        }
        if (movingDirection < -0.5f && gasInput > 0)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else if (movingDirection > 0.5f && gasInput < 0)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else
        {
            brakeInput = 0;
        }
    }

    public void SetPark(bool enabledPark)
    {
        if (enabledPark)
        {
            gearState = GearState.Park;
            brakeInput = 1f;

            // Apply full brake torque immediately
            colliders.FRWheel.brakeTorque = brakePower;
            colliders.FLWheel.brakeTorque = brakePower;
            colliders.RRWheel.brakeTorque = brakePower;
            colliders.RLWheel.brakeTorque = brakePower;

            // Optional: zero out velocity
            playerRB.linearVelocity = Vector3.zero;
            playerRB.angularVelocity = Vector3.zero;
        }
        else
        {
            gearState = GearState.Neutral;
            brakeInput = 0f;
        }
    }

    void ApplyBrake()
    {
        colliders.FRWheel.brakeTorque = brakeInput * brakePower * 0.7f ;
        colliders.FLWheel.brakeTorque = brakeInput * brakePower * 0.7f;
        colliders.RRWheel.brakeTorque = brakeInput * brakePower * 0.3f;
        colliders.RLWheel.brakeTorque = brakeInput * brakePower * 0.3f;
    }

    void ApplyMotor() 
    {
        currentTorque = CalculateTorque();
        colliders.RRWheel.motorTorque = currentTorque * gasInput;
        colliders.RLWheel.motorTorque = currentTorque * gasInput;

        if (gearState == GearState.Park)
        {
            colliders.RRWheel.motorTorque = 0;
            colliders.RLWheel.motorTorque = 0;
            colliders.FRWheel.steerAngle = 0;
            colliders.FLWheel.steerAngle = 0;
            return;
        }
    }

    float CalculateTorque()
    {
        float torque = 0;
        if (gearState == GearState.Park)
        {
            RPM = idleRPM;
            return 0f;
        }
        if (RPM < idleRPM + 200 && gasInput == 0 && currentGear == 0)
        {
            gearState = GearState.Neutral;
        }
        if (gearState == GearState.Running && clutch > 0)
        {
            if (RPM > increaseGearRPM)
            {
                StartCoroutine(ChangeGear(1));
            }
            else if (RPM < decreaseGearRPM)
            {
                StartCoroutine(ChangeGear(-1));
            }
        }
        if (isEngineRunning > 0)
        {
            if (clutch < 0.1f)
            {
                RPM = Mathf.Lerp(RPM, Mathf.Max(idleRPM, redLine * gasInput) + Random.Range(-50, 50), Time.deltaTime);
            }
            else
            {
                wheelRPM = Mathf.Abs((colliders.RRWheel.rpm + colliders.RLWheel.rpm) / 2f) * gearRatios[currentGear] * differentialRatio;
                RPM = Mathf.Lerp(RPM, Mathf.Max(idleRPM - 100, wheelRPM), Time.deltaTime * 3f);
                float safeRPM = Mathf.Max(RPM, 1500f);
                torque = hpToRPMCurve.Evaluate(RPM / redLine) * motorPower / safeRPM * gearRatios[currentGear] * differentialRatio * 5252f * clutch;            }
        }
        return torque;
    }

    void ApplySteering()
    {
        if (gearState == GearState.Park) 
        { 
            colliders.FRWheel.steerAngle = 0; 
            colliders.FLWheel.steerAngle = 0; 
            return; 
        }

        float steeringAngle = steeringInput*steeringCurve.Evaluate(speed);
        lastSteeringAngle = steeringAngle;
        if (slipAngle < 120f)
        {
            steeringAngle += Vector3.SignedAngle(transform.forward, playerRB.linearVelocity + transform.forward, Vector3.up);
        }
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);

        colliders.FRWheel.steerAngle = steeringAngle;
        colliders.FLWheel.steerAngle = steeringAngle;
    }

    void UpdateSteeringWheel()
    {
        if (steeringWheel == null)
            return;
    
        // Correct direction
        float targetRotation = steeringInput * maxWheelRotation;
    
        // Smooth movement
        steeringWheelCurrent = Mathf.Lerp(
            steeringWheelCurrent,
            targetRotation,
            Time.deltaTime * steeringWheelSmooth
        );
    
        // Apply relative to original rotation
        steeringWheel.localRotation =
            steeringWheelStartRot * Quaternion.Euler(0f, 0f, steeringWheelCurrent);
    }

    void UpdateIndicators()
    {
        indicatorTimer += Time.deltaTime;

        bool flashOn = Mathf.FloorToInt(indicatorTimer / indicatorFlashSpeed) % 2 == 0;

        if (leftIndicator)
            leftIndicatorUI.enabled = flashOn;
        else
            leftIndicatorUI.enabled = false;

        if (rightIndicator)
            rightIndicatorUI.enabled = flashOn;
        else
            rightIndicatorUI.enabled = false;
    }

    public float GetSpeedRatio()
    {
        var gas = Mathf.Clamp(Mathf.Abs(gasInput), 0.5f, 1f);
        return RPM * gas / redLine;
    }

    /*
    void ApplyWheelPositions()
    {
        UpdateWheel(colliders.FRWheel, wheelgameobjects.FRWheel);
        UpdateWheel(colliders.FLWheel, wheelgameobjects.FLWheel);
        UpdateWheel(colliders.RRWheel, wheelgameobjects.RRWheel);
        UpdateWheel(colliders.RLWheel, wheelgameobjects.RLWheel);
    }
    void UpdateWheel(WheelCollider coll, MeshRenderer wheelMesh)
    {
        Quaternion quat;
        Vector3 position;
        coll.GetWorldPose(out position, out quat);
        wheelMesh.transform.position = position;
        wheelMesh.transform.rotation = quat;
    }
    */

    IEnumerator ChangeGear(int gearChange)
    {
        gearState = GearState.CheckingChange;
        if (currentGear + gearChange >= 0)
        {
            if (gearChange > 0)
            {
                //increase the gear
                yield return new WaitForSeconds(0.7f);
                if (RPM < increaseGearRPM || currentGear >= gearRatios.Length - 1)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }
            if (gearChange < 0)
            {
                //decrease the gear
                yield return new WaitForSeconds(0.1f);
                if (RPM > decreaseGearRPM || currentGear <= 0)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }
            gearState = GearState.Changing;
            yield return new WaitForSeconds(changeGearTime);
            currentGear += gearChange;
        }
        if(gearState!=GearState.Neutral)
        gearState = GearState.Running;
    }
}

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FRWheel;
    public WheelCollider FLWheel;
    public WheelCollider RRWheel;
    public WheelCollider RLWheel;
}

/*
[System.Serializable]
public class WheelGameObjects
{
    public MeshRenderer FRWheel;
    public MeshRenderer FLWheel;
    public MeshRenderer RRWheel;
    public MeshRenderer RLWheel;
}
*/

