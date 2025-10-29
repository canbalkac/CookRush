using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;

    //Sprint Parameters
    public float maxStamina = 20f;
    public float drainPerSec = 1.5f;
    public float regenPerSec = 1.0f;
    public float sprintMultiplier = 1.3f;
}