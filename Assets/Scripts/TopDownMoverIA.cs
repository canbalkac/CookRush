using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TopDownMoverIA : MonoBehaviour
{
    public Rigidbody rb;
    public InputActionReference move;
    public float moveSpeed = 5f;
    public float turnSpeed = 720f;
    public bool cameraRelative = false;

    Camera cam;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        // Kayýtlý keybind'leri yükle
        var asset = move?.action?.actionMap?.asset;
        var json = PlayerPrefs.GetString("rebinds", string.Empty);
        if (asset != null && !string.IsNullOrEmpty(json))
            asset.LoadBindingOverridesFromJson(json);
    }

    void OnEnable() { move?.action?.Enable(); }
    void OnDisable() { move?.action?.Disable(); }

    private void FixedUpdate()
    {
        if (move == null) return;

        Vector2 input = move.action.ReadValue<Vector2>();
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        if(cameraRelative && cam)
        {
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            dir = (r * input.x + f * input.y);
        }

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector3 v = rb.linearVelocity;
        v.x = dir.x * moveSpeed;
        v.z = dir.z * moveSpeed;
        rb.linearVelocity = v;

        Vector3 planar = new Vector3(v.x, 0f, v.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(planar.normalized, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, target, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }
        else rb.angularVelocity = Vector3.zero;
    }
}
