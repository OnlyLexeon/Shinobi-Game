using UnityEngine;

public class TransformOnStart : MonoBehaviour
{
    public enum RotateDirection
    {
        X, Y, Z
    }

    // Rotation settings
    public float rotation = 90f;
    public RotateDirection rotateDirection = RotateDirection.Y;

    // Position settings
    public Vector3 positionOffset = Vector3.zero; // Offset to add to the current position
    public bool applyPositionOffset = false;     // Enable/disable position transformation

    void Start()
    {
        // Apply rotation
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
        transform.Rotate(rotationAxis * rotation);

        // Apply position offset if enabled
        if (applyPositionOffset)
        {
            transform.position += positionOffset;
        }
    }
}
