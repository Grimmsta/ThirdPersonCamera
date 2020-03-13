using UnityEngine;

public class CameraController : MonoBehaviour
{
    public const float Y_ANGLE_MIN = -10f;
    public const float Y_ANGLE_MAX = 90f;

    public const float OFFSET_MIN = 2f;
    public const float OFFSET_MAX = 20f;

    [SerializeField]
    Transform player = default;

    [SerializeField, Tooltip("Choose what layers should be blocking the view")]
    LayerMask obstructionMask = -1;

    [SerializeField, Range(OFFSET_MIN, OFFSET_MAX), Tooltip("The distance between the player and the camera")]
    float offsetDistance = 20f; //Distance between player and camera

    [SerializeField, Min(0f), Tooltip("An area where the player can move within without ")]
    float focusRadius = 1f; //Gives a radius where the player can move without the camera following

    [SerializeField, Range(0f, 1f), Tooltip("A percentage of the are you can move within before the camera starts moving. 0 = long delay, 1 = immediately")]
    float focusCentering = 0.75f;

    [SerializeField, Range(1f, 360f), Tooltip("Amount of degrees rotating per second")]
    float rotationSpeed = 90f;

    [SerializeField, Range(OFFSET_MIN, OFFSET_MAX), Tooltip("Capping the max and min vertical angle of the player controller")]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField, Min(0f), Tooltip("Amount of seconds before automatic rotation kicks in")]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f), Tooltip("Max angle to rotate linearly, after that we rotate according to the rotation speed")]
    float alignSmoothRange = 45f;

    [SerializeField, Tooltip("How fast we can zoom in towards the player")]
    float zoomSpeed = 410;

    Camera regularCamera;

    float lastManualRotationTime;

    const float e = 0.0001f; //Using e as in epsilon to mark a small positive number

    Vector3 focusPoint, previousFocusPoint; //What we want to focus on, in this case the players position

    Vector2 orbitAngles = new Vector2(45f, 0f); //We use Vector2D for there is no need for the Z axis

    void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = player.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    private void OnValidate()
    {
        if (maxVerticalAngle <minVerticalAngle) //Basically clamping the the max and min values of the vertical angle
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void LateUpdate()
    {
        UpdateFocusPoint();
        ZoomInAndOut();
        Quaternion lookRotation;
        
        if (ManualRotation() || AutomaticRotation()) //We only need to constraint the angles if they been changed, so we check for that
        {
            ConstraintAngles();
            lookRotation = Quaternion.Euler(orbitAngles); //WE only need to recalculate the angles if they been changed, otherwise (else stat.) we retrieve the existing one
        }
        else
        {
            lookRotation = transform.localRotation;
        }

        //Set the position and rotation for the camera
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * offsetDistance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = player.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance - regularCamera.nearClipPlane, obstructionMask))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition-rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = player.position;

        if (focusRadius > 0f) //If the radius is bigger than 0 we want the effect of having the player move a bit without camera following, else we just want to update the camera follow directly
        {
            float distance = Vector3.Distance(targetPoint, focusPoint); //Gets the distance between "the old player pos" and the newly updated player pos

            if (distance > focusRadius) //We check if that distance is greater than the radius we allowed the player to move within
            {
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, focusRadius / distance); //Lerps between the old and new player pos 
            }
            if (distance >0.01f && focusCentering >0f)
            {
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime)); //Lerping between the two points, smoothing the lerping out with the Pow method
            }
        }
        else 
        {
            focusPoint = targetPoint; 
        }
    }

    bool ManualRotation()
    {
        Vector2 input = new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));

        if (input.x < -e||input.x > e||input.y < -e|| input.y > e) //here we check for movement from the mouse
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input; //Time.unscaledDeltaTime is independent from the in-game time, so if the timescale is tempered the orbitAngles are unaffected 
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)        //Check if we should kick in automatic alignment
        {
            return false;
        }

        Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x, //Setting movement by checking the movement on a 2D plane
                                       focusPoint.z - previousFocusPoint.z); 
        
        float movementDeltaSqr = movement.sqrMagnitude; //Getting the change in movement by squaring the movement

        if (movementDeltaSqr < e) //Check if there has been a change in movement
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr)); //Getting the direction we are heading by normalizing our current direction 
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle)); 
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }

        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    void ConstraintAngles()
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y <0f) //Making sure the vertical angles stays within the 0-360 range and not exceeding that
        {
            orbitAngles.y += 360f;
        }
        else if (orbitAngles.y >=360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg; //Calculates the angle in which the player is heading
        return direction.x < 0f ? 360- angle : angle; //Since the angle can be clock or counterclockwise, we need to check the x value of our current direction as well 
    }

    void ZoomInAndOut()
    {
        offsetDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomSpeed;
        offsetDistance = Mathf.Clamp(offsetDistance, OFFSET_MIN, OFFSET_MAX);
    }

    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }
}
