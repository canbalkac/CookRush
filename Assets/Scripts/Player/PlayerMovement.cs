//using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.UI;
//using TMPro;
//
//[RequireComponent(typeof(Rigidbody))]
//public class PlayerMovement : MonoBehaviour
//{
//    public Rigidbody rb;
//    public InputActionReference move;
//    public InputActionReference sprint;
//    public Player player;
//    public float turnSpeed = 720f;
//    public bool cameraRelative = false;
//
//    public Image StaminaGreenIMG;
//    public Image StaminaRedIMG;
//    public TMP_Text staminaText;
//    public float stamina;
//    public float maxStamina;
//
//
//    Camera cam;
//
//    void Awake()
//    {
//
//        if (!rb) rb = GetComponent<Rigidbody>();
//        if (!player) player = GetComponent<Player>();
//        cam = Camera.main;
//
//        // Kayıtlı keybind'leri yükle
//        var asset = move?.action?.actionMap?.asset;
//        var json = PlayerPrefs.GetString("rebinds", string.Empty);
//        if (asset != null && !string.IsNullOrEmpty(json))
//            asset.LoadBindingOverridesFromJson(json);
//
//        float stamina = player ? player.maxStamina : 10f;
//        float maxStamina = stamina;
//        staminaText.text = "Stamina : " + stamina;
//    }
//
//    void OnEnable() { move?.action?.Enable(); sprint?.action?.Enable(); }
//    void OnDisable() { move?.action?.Disable(); sprint?.action?.Disable(); }
//
//    private void FixedUpdate()
//    {
//        if (move == null) return;
//
//        // Player Script
//        float baseSpeed = player ? player.moveSpeed : 5f;
//        float sprintMultiplier = player ? player.sprintMultiplier : 1.3f;
//
//        // Sprinting or Walking
//        bool isSprinting = sprint?.action?.IsPressed() ?? false;
//        float moveSpeed = isSprinting ? baseSpeed * sprintMultiplier : baseSpeed;
//
//        Debug.Log("Sprinting ? " + isSprinting);
//        if (isSprinting)
//        {
//            staminaText.text = "Stamina : " + stamina;
//            stamina -= 0.1f;
//            StaminaGreenIMG.fillAmount = stamina;
//            StaminaRedIMG.fillAmount = stamina;
//        } 
//        else 
//        {
//            if(stamina != maxStamina)
//            {
//                stamina += 0.1f;
//                StaminaGreenIMG.fillAmount = stamina;
//                StaminaRedIMG.fillAmount = stamina;
//            }
//        }
//
//        Debug.Log("Movement Speed = " + moveSpeed);
//
//        Vector2 input = move.action.ReadValue<Vector2>();
//        Vector3 dir = new Vector3(input.x, 0f, input.y);
//
//        if(cameraRelative && cam)
//        {
//            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
//            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
//            dir = (r * input.x + f * input.y);
//        }
//
//        if (dir.sqrMagnitude > 1f) dir.Normalize();
//
//        Vector3 v = rb.linearVelocity;
//        v.x = dir.x * moveSpeed;
//        v.z = dir.z * moveSpeed;
//        rb.linearVelocity = v;
//        if(isSprinting) { }
//
//        Vector3 planar = new Vector3(v.x, 0f, v.z);
//        if (planar.sqrMagnitude > 0.0001f)
//        {
//            Quaternion target = Quaternion.LookRotation(planar.normalized, Vector3.up);
//            Quaternion next = Quaternion.RotateTowards(rb.rotation, target, turnSpeed * Time.fixedDeltaTime);
//            rb.MoveRotation(next);
//        }
//        else rb.angularVelocity = Vector3.zero;
//    }
//}
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public InputActionReference move;
    public InputActionReference sprint;
    public Player player;                  // moveSpeed, maxStamina, drainPerSec, regenPerSec, sprintMultiplier
    public Image StaminaGreenIMG;
    public Image StaminaRedIMG;
    public Canvas StaminaCanvas;

    [Header("Movement")]
    public float turnSpeed = 720f;
    public bool cameraRelative = false;

    [Header("State")]
    public float stamina = -1f;            // -1 => Awake'ta player.maxStamina'ya çekilir
    bool exhausted;                        // 0'a düştü mü? Tam dolana kadar sprint yasak

    Camera cam;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!player) player = GetComponent<Player>();
        cam = Camera.main;

        // Rebind'leri yükle
        var asset = move?.action?.actionMap?.asset;
        var json = PlayerPrefs.GetString("rebinds", string.Empty);
        if (asset != null && !string.IsNullOrEmpty(json))
            asset.LoadBindingOverridesFromJson(json);

        // Stamina başlangıcı
        float maxStamina = GetMaxStamina();
        stamina = (stamina < 0f) ? maxStamina : Mathf.Clamp(stamina, 0f, maxStamina);
        exhausted = stamina <= 0f;
        stamina = maxStamina;
        UpdateStaminaUI();
    }

    void OnEnable() { move?.action?.Enable(); sprint?.action?.Enable(); }
    void OnDisable() { move?.action?.Disable(); sprint?.action?.Disable(); }

    void FixedUpdate()
    {
        StaminaCanvas.transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        if (move == null || move.action == null) return;

        // Parametreleri Player'dan çek
        float baseSpeed = GetMoveSpeed();
        float sprintMult = GetSprintMultiplier();
        float maxStamina = GetMaxStamina();
        float drainPerSec = GetDrainPerSec();
        float regenPerSec = GetRegenPerSec();

        // Input → yön
        Vector2 input = move.action.ReadValue<Vector2>();
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        if (cameraRelative && cam)
        {
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            dir = r * input.x + f * input.y;
        }
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        bool sprintPressed = sprint?.action?.IsPressed() ?? false;
        bool isSprinting = sprintPressed && !exhausted && stamina > 0f;

        float moveSpeed = isSprinting ? baseSpeed * sprintMult : baseSpeed;

        // Stamina güncelle
        float dt = Time.fixedDeltaTime;
        if (isSprinting && dir != Vector3.zero)
        {
            stamina -= drainPerSec * dt;
            if (stamina <= 0f) { stamina = 0f; exhausted = true; }
        }
        else
        {
            stamina += regenPerSec * dt;
            if (exhausted && stamina >= maxStamina) exhausted = false; // tam dolunca kilidi aç
        }
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // update stamina ui
        if (isSprinting) StaminaCanvas.enabled = true;
        else if (!isSprinting && stamina == maxStamina) StaminaCanvas.enabled = false;
        float fill = maxStamina > 0f ? Mathf.Clamp01(stamina / maxStamina) : 0f;
        if(dir != Vector3.zero && isSprinting)
        {
            StaminaGreenIMG.fillAmount = fill + 0.07f;
            StaminaRedIMG.fillAmount = fill;
        } 
        else
        {
            StaminaGreenIMG.fillAmount = StaminaRedIMG.fillAmount;
            StaminaRedIMG.fillAmount = fill;
        }

            // Hareket
            Vector3 v = rb.linearVelocity;
        v.x = dir.x * moveSpeed;
        v.z = dir.z * moveSpeed;
        rb.linearVelocity = v;

        // Yönler
        Vector3 planar = new Vector3(v.x, 0f, v.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(planar.normalized, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, target, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }
        else rb.angularVelocity = Vector3.zero;
    }

    // ---- Helpers: Player parametreleri ----
    float GetMoveSpeed() => player ? player.moveSpeed : 5f;
    float GetMaxStamina() => player ? player.maxStamina : 10f;
    float GetDrainPerSec() => player ? player.drainPerSec : 1.5f;
    float GetRegenPerSec() => player ? player.regenPerSec : 1.0f;
    float GetSprintMultiplier() => player ? player.sprintMultiplier : 1.3f;

    void UpdateStaminaUI(float max = -1f)
    {
        if (max < 0f) max = GetMaxStamina();
        float fill = max > 0f ? Mathf.Clamp01(stamina / max) : 0f;
        if (StaminaGreenIMG) StaminaGreenIMG.fillAmount = fill + 0.07f;
        if (StaminaRedIMG) StaminaRedIMG.fillAmount = fill;
    }
}
