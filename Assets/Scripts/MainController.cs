using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour
{
    public int movementSpeed = 10;
    public int rotationSpeed = 100;
 
    [SerializeField]
    Transform playerInputSpace = default;

    public bool useCameraDirection = false;  

    Vector2 playerInput;
    Vector3 movement;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        playerInput.y = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime;
        playerInput.x = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;

        if (playerInputSpace && useCameraDirection) //Only moving forward with the direction of where you're looking
        {
            rotationSpeed = movementSpeed;
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            movement = (forward * playerInput.y + right * playerInput.x);
        }
        else
        {
            //transform.Rotate(0, playerInput.x, 0);
            transform.Translate(playerInput.x, 0, playerInput.y);
        }
        transform.localPosition += movement;
    }

    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg; //Calculates the angle in which the player is heading
        return direction.x < 0f ? 360 - angle : angle; //Since the angle can be clock or counterclockwise, we need to check the x value of our current direction as well 
    }
}
