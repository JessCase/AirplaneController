using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using System.Net;
#endif

public class AirplaneController : MonoBehaviour
{
    private Rigidbody rb;
    private Renderer objectRenderer;
    private Vector3 objectSize;

    public float groundCheckDistance = .5f;
    private bool isGrounded;

    #region Movement Variables

    public float maxSpeed = 15f;
    public float maxReverseSpeed = 5f;
    public float currentSpeed = 0;
    public float speedRampUp = .5f;
    public float speedRampDown = 1f;
    public float idleSpeed = 3f;

    public bool invertedPitch = true;
    public float pitchRotationSpeed = 200f;
    public float yawRotationSpeed = 5f;
    public float rollRotationSpeed = 20f;

    public float directionModifier = -.09f;

    // Internal Variables
    private float acceleration;
    private float pitch = 0;
    private float yaw = 0;
    private float roll = 0;

    private float speedPercentage;

    // Hard coded in to bypass using the input manager
    // To change inputs, simple change the letter at the end
    private KeyCode yawLeft = KeyCode.Q;
    private KeyCode yawRight = KeyCode.E;

    #endregion

    #region Propeller Variables

    public int numOfProps = 1;
    public Transform propeller;
    public Transform propeller2;
    public float propSpeedCap = 3f;

    #endregion

    #region Camera Variables

    public Transform cameraArm;

    public bool camPullback = true;
    public float minCameraDistance = 1;
    public float maxCameraDistance = 1.5f;

    public bool lagRotation = true;
    public float rotationLagSpeed = 1f;
    public float maxLagDist = .1f;

    // Internal Variables
    private float cameraDistance;
    private float minMaxDifference;

    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectRenderer = GetComponent<MeshRenderer>();
        objectSize = objectRenderer.bounds.size;

        if(camPullback)
        {
            // Sets camera distance to the minimum distance when play begins
            cameraDistance = minCameraDistance;
            minMaxDifference = maxCameraDistance - minCameraDistance;
        }

        // Locks cursor to middle of screen and hides it
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        #region Propeller Rotation

        if (numOfProps > 0 && propeller != null)
        {
            Quaternion prop = propeller.localRotation;

            // Calculate movement speed for propeller
            float propSpeed = Mathf.Clamp(-currentSpeed / 2.5f, -propSpeedCap, propSpeedCap);

            prop.eulerAngles = new Vector3(0, 0, propSpeed);

            // Moves 2nd prop when active
            if (numOfProps == 2 && propeller2 != null)
            {
                propeller2.rotation *= prop;
            }

            propeller.rotation *= prop;
        }

        #endregion

        IsGrounded();

        #region Camera

        #region Pullback

        if(camPullback)
        {
            // Calculates the percentage of speed the plane is currentl at
            speedPercentage = (currentSpeed / maxSpeed) * 100;

            // How much the camera distance should in % based on speed
            float camDistancePercent = (speedPercentage * minMaxDifference) / 100;

            // Moves camera to correct distance based on percentage
            cameraDistance = minCameraDistance + camDistancePercent;
            cameraArm.localScale = new Vector3(1, 1, cameraDistance);
        }

        #endregion

        #region Lag Rotation

        if (lagRotation)
        {
            Quaternion addRot = Quaternion.identity;

            // Right roll
            if (roll < 0)
            {
                Vector3 laggedRotation = new Vector3(addRot.x, addRot.y, addRot.z + rotationLagSpeed * Time.deltaTime);

                addRot.eulerAngles = laggedRotation;
            }
            // Left roll
            else if(roll > 0)
            {
                Vector3 laggedRotation = new Vector3(addRot.x, addRot.y, addRot.z - rotationLagSpeed * Time.deltaTime);

                addRot.eulerAngles = laggedRotation;
            }
            // No roll
            else
            {
                if(cameraArm.localRotation.z > 0)
                {
                    Vector3 resetRotation = new Vector3(addRot.x, addRot.y, addRot.z - rotationLagSpeed * Time.deltaTime);

                    addRot.eulerAngles = resetRotation;
                }
                else if(cameraArm.localRotation.z < 0)
                {
                    Vector3 resetRotation = new Vector3(addRot.x, addRot.y, addRot.z + rotationLagSpeed * Time.deltaTime);

                    addRot.eulerAngles = resetRotation;
                }
            }
            // Clamp rotation
            cameraArm.localRotation = new Quaternion(cameraArm.localRotation.x, cameraArm.localRotation.y, Mathf.Clamp(cameraArm.localRotation.z, -maxLagDist, maxLagDist), cameraArm.localRotation.w);
            // Apply rotation
            cameraArm.rotation *= addRot;
        }

        #endregion

        #endregion
    }

    private void FixedUpdate()
    {
        Acceleration();

        Quaternion addRot = Quaternion.identity;

        // Gives player control of pitch and roll only when in the air
        if(!isGrounded)
        {
            Pitch();
            // Uses default unity input
            roll = -Input.GetAxis("Horizontal") * (rollRotationSpeed * Time.deltaTime);
        }

        // Gives player control of yaw (left and right movement) only when moving or in the air
        if(acceleration != 0 || !isGrounded)
        {
            // Handles left and right turning
            if(Input.GetKey(yawLeft))
            {
                yaw = -1 * (yawRotationSpeed * Time.deltaTime);
            }
            else if(Input.GetKey(yawRight))
            {
                yaw = 1 * (yawRotationSpeed * Time.deltaTime);
            }
            else
            {
                yaw = 0 * (yawRotationSpeed * Time.deltaTime);
            }
            // Can create an input "Yaw" that will handle movement instead of the above if
            // yaw = Input.GetAxis("Yaw") * (yawRotationSpeed * Time.deltaTime);
        }

        addRot.eulerAngles = new Vector3(pitch, yaw, roll);
        rb.rotation *= addRot;
    }


    #region Movement Methods

    private void Acceleration()
    {
        acceleration = Input.GetAxis("Vertical");

        currentSpeed += acceleration * (speedRampUp * Time.deltaTime);

        // When not accelerating
        if(acceleration == 0)
        {
            // Bring speed back to 0 when grounded
            if (isGrounded)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, speedRampDown * Time.deltaTime);
            }
            // Bring speed to idleSpeed when in the air
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, idleSpeed, speedRampDown * Time.deltaTime);
            }
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxReverseSpeed, maxSpeed);

        Vector3 flightDirectionModifier = new Vector3(0, directionModifier, 0);
        rb.velocity = transform.TransformDirection(Vector3.forward + flightDirectionModifier) * currentSpeed;
    }

    private void Pitch()
    {
        pitch = Input.GetAxis("Mouse Y") * (pitchRotationSpeed * Time.deltaTime);

        if(!invertedPitch)
        {
            pitch = -pitch;
        }
    }

    #endregion

    private void IsGrounded()
    {
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * groundCheckDistance, Color.red);

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit, groundCheckDistance))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // Draws the directionModifier in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.forward + new Vector3(0, directionModifier, 0)) * 10);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * (groundCheckDistance));
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(AirplaneController)), InitializeOnLoadAttribute]
public class AirplaneControllerEditor : Editor
{
    AirplaneController plane;
    SerializedObject serPlane;

    private void OnEnable()
    {
        plane = (AirplaneController)target;
        serPlane = new SerializedObject(plane);
    }

    public override void OnInspectorGUI()
    {
        serPlane.Update();

        EditorGUILayout.Space();
        GUILayout.Label("Airplane Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUILayout.Label("By Jess Case", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        GUILayout.Label("version 1.0", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        EditorGUILayout.Space();

        #region Movement

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        GUI.enabled = false;
        plane.currentSpeed = EditorGUILayout.FloatField(new GUIContent("Current Speed", "Displays the current flight speed. Read only."), plane.currentSpeed);
        GUI.enabled = true;

        plane.maxSpeed = EditorGUILayout.FloatField(new GUIContent("Max Speed", "Determines the fastest possible flight speed when moving forward."), plane.maxSpeed);
        plane.maxReverseSpeed = EditorGUILayout.Slider(new GUIContent("Max Reverse Speed", "Determines the fastest possible speed when moving backwards."), plane.maxReverseSpeed, 0f, plane.maxSpeed);
        plane.speedRampUp = EditorGUILayout.Slider(new GUIContent("Speed Ramp Up", "Determines how fast the airplane gains speed while accelerating"), plane.speedRampUp, 0f, 1f);
        plane.speedRampDown = EditorGUILayout.Slider(new GUIContent("Speed Ramp Down", "Determines how fast the airplane loses speed when acceleration stops."), plane.speedRampDown, 0f, 5f);
        plane.idleSpeed = EditorGUILayout.Slider(new GUIContent("Idle Speed", "Determines the minimum speed of the airplane when acceleration stops in midair."), plane.idleSpeed, 0f, 5f);

        EditorGUILayout.Space();
        GUILayout.Label("Rotations", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        plane.invertedPitch = EditorGUILayout.ToggleLeft(new GUIContent("Invert Pitch", "When true,moving down with the mouse causes the plane to go up and moving up causes the plane to go down. When false, the opposite is true."), plane.invertedPitch);
        plane.pitchRotationSpeed = EditorGUILayout.Slider(new GUIContent("Pitch Rotation Speed", "Determines the speed of the plane’s up and down movement."), plane.pitchRotationSpeed, 0f, 500f);
        plane.yawRotationSpeed = EditorGUILayout.Slider(new GUIContent("Yaw Rotation Speed", "Determines the speed of the plane’s left and right movement."), plane.yawRotationSpeed, 0f, 10f);
        plane.rollRotationSpeed = EditorGUILayout.Slider(new GUIContent("Roll Rotation Speed", "Determines the speed of the plane’s rotation along the Z axis."), plane.rollRotationSpeed, 0f, 50f);

        EditorGUILayout.Space();
        GUILayout.Label("Miscellaneous", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        plane.directionModifier = EditorGUILayout.Slider(new GUIContent("Direction Modifier", "Adjusts forward direction of movement. Allows for a more controlled takeoff time. Shown as a black line."), plane.directionModifier, -5, 5);
        plane.groundCheckDistance = EditorGUILayout.Slider(new GUIContent("Ground Check", "Determines the distance checked from the bottom of the plane to the ground."), plane.groundCheckDistance, 0, 1f);

        EditorGUILayout.Space();

        #endregion

        #region Propeller

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Propeller Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        plane.numOfProps = EditorGUILayout.IntSlider(new GUIContent("Propellers Amount", "Number of props on the plane."), plane.numOfProps, 0, 2);

        if (plane.numOfProps == 1)
        {
            plane.propeller = (Transform)EditorGUILayout.ObjectField(new GUIContent("Propeller", "A child object of the plane, the propeller’s rotation is controlled by code when “Propeller Amount” is greater than 0."), plane.propeller, typeof(Transform), true);
        }
        else if(plane.numOfProps > 1)
        {
            plane.propeller = (Transform)EditorGUILayout.ObjectField(new GUIContent("Propeller 1", "A child object of the plane, the propeller’s rotation is controlled by code when “Propeller Amount” is greater than 0."), plane.propeller, typeof(Transform), true);
            plane.propeller2 = (Transform)EditorGUILayout.ObjectField(new GUIContent("Propeller 2", "Same as 'Propeller 1'. Only active when 'Propeller Amount' equals 2."), plane.propeller2, typeof(Transform), true);
        }

        GUI.enabled = plane.numOfProps > 0;

        plane.propSpeedCap = EditorGUILayout.Slider(new GUIContent("Speed Cap", "Determines the max rotation speed of the propellers."), plane.propSpeedCap, 0f, 5f);

        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Camera

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        plane.cameraArm = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Arm", "Placed in the middle of the plane object. Directly controls the movement of the player camera."), plane.cameraArm, typeof(Transform), true);
        
        plane.camPullback = EditorGUILayout.ToggleLeft(new GUIContent("Camera Pullback", "When active, the camera fall behind the plane during acceleration and catch uyp when not accelerating."), plane.camPullback);

        GUI.enabled = plane.camPullback;

        plane.minCameraDistance = EditorGUILayout.Slider(new GUIContent("Min Camera Distance", "The closest the camera will be to the plane. Camera starts at this distance and sits here when the player is not moving."), plane.minCameraDistance, 1f, 5f);
        plane.maxCameraDistance = EditorGUILayout.Slider(new GUIContent("Max Camera Distance", "The farthest the camera will be from the plane. While accelerating, the camera moves toward this distance."), plane.maxCameraDistance, 1f, 5f);

        GUI.enabled = true;

        plane.lagRotation = EditorGUILayout.ToggleLeft(new GUIContent("Lag Rotation", "When active, the camera will rotate slower than the plane. This causes a “lag” effect."), plane.lagRotation);

        GUI.enabled = plane.lagRotation;

        plane.rotationLagSpeed = EditorGUILayout.Slider(new GUIContent("Lag Speed", "Determines the speed at which the camera lags and catches up when active."), plane.rotationLagSpeed, .5f, 5f);
        plane.maxLagDist = EditorGUILayout.Slider(new GUIContent("Max Lag Distance", "Determines the maximum rotation distance the camera will lag behind the plane when rotating."), plane.maxLagDist, 0, .5f);

        GUI.enabled = true;

        #endregion

        //Sets any changes from the prefab
        if (GUI.changed)
        {
            EditorUtility.SetDirty(plane);
            Undo.RecordObject(plane, "Plane Change");
            serPlane.ApplyModifiedProperties();
        }
    }
}

#endif