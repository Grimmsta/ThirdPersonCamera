﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public const float Y_ANGLE_MIN = 0f;
    public const float Y_ANGLE_MAX = 50f;

    public const float OFFSET_MIN = 2f;
    public const float OFFSET_MAX = 20f;
    
    public const float ZOOM_MIN = 0f;
    public const float ZOOM_MAX = 1f;

    public Transform player = default;
    public Transform camTransform;
    Camera regularcam;
    
    [SerializeField, Range(1f, 20f)]
    float offsetDistance = 20f; //Distance between player and camera

    [SerializeField, Min(0f)]
    float focusRadius = 1f; //Gives a radius where the player can move without the camera following

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.75f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    public float zoomSpeed = 410;

    float horizontal = 0;
    float vertical = 0;
    float zoom = 0;

    float lastManualRotationTime;

    const float e = 0.0001f; //Using e as in epsilon to mark a small positive number

    Vector3 focusPoint, previousFocusPoint; //What we want to focus on, in this case the players position

    Vector2 orbitAngles = new Vector2(45f, 0f); //We use Vector2D for there is no need for the Z axis

    void Awake()
    {
        regularcam = GetComponent<Camera>();
        focusPoint = player.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    void Start()
    {
        camTransform = transform;
        cam = Camera.main;
    }

    private void OnValidate()
    {
        if (maxVerticalAngle <minVerticalAngle) //Basically clamping the the max and min values of the vertical angle
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateFocusPoint();
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
        Vector3 lookDir = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDir * offsetDistance;

        if (Physics.Raycast(focusPoint, -lookDir, out RaycastHit hit, offsetDistance))
        {
            lookPosition = focusPoint - lookDir * hit.distance;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);

        //float playerRotation = Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed;
        //player.Rotate(0, playerRotation, 0);

        //horizontal += Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed;

        //zoom += Input.GetAxis("Mouse ScrollWheel")*Time.deltaTime* zoomSpeed;

        //offsetDistance -= zoom;

        //vertical += Input.GetAxis("Mouse Y") * Time.deltaTime*rotationSpeed;

        //zoom = Mathf.Clamp(zoom, ZOOM_MIN, ZOOM_MAX);
        //offsetDistance = Mathf.Clamp(offsetDistance, OFFSET_MIN, OFFSET_MAX);
        //vertical = Mathf.Clamp(vertical, Y_ANGLE_MIN, Y_ANGLE_MAX);

        //Vector3 direction = new Vector3(0, 0, offsetDistance);
        //Quaternion rotation = Quaternion.Euler(vertical, horizontal, 0);
        //camTransform.position = player.position - (rotation * direction);

        //transform.LookAt(player);
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
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, Mathf.Pow(1f - focusCentering, Time.deltaTime)); //Lerping between the two points, smoothing the lerping out with the Pow method
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
        if (Time.unscaledTime - lastManualRotationTime < alignDelay) //Check if we should kick in automatic alignment
        {
            return false;
        }

        Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x, focusPoint.z - previousFocusPoint.z); //Setting movement by checking the movement on a 2D plane
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
            rotationSpeed *= (180f - deltaAbs) / alignSmoothRange;
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
}