using UnityEngine;

public class isRotating : MonoBehaviour
{
    public enum RotateDirection
    {
        X, Y, Z
    }

    public bool isRotate = true;
    public float rotateSpeed = 10f;
    public RotateDirection rotateDirection = RotateDirection.Y;

    void Update()
    {
        if (isRotate)
        {
            Vector3 rotationAxis = Vector3.zero;
            switch (rotateDirection)
            {
                case RotateDirection.X:
                    rotationAxis = Vector3.right;
                    break;
                case RotateDirection.Y:
                    rotationAxis = Vector3.up;
                    break;
                case RotateDirection.Z:
                    rotationAxis = Vector3.forward;
                    break;
            }

            // Rotate the object
            transform.Rotate(rotationAxis * rotateSpeed * Time.deltaTime);
        }
    }
}
