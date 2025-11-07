using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Variabile pentru mișcare
    private CharacterController controller;
    private Transform cameraTransform;
    public float viteza = 10f;
    public float sensitivitateMouse = 200f;
    private float xRotation = 0f;

    // Variabile pentru "Grab" (mâini)
    private Transform obiectPrins = null;
    private Transform cameraNoastra;
    public Transform suportMana;

    void Start()
    {
        // Setup Mișcare
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
        cameraNoastra = cameraTransform; // Salvăm o referință pentru "grab"

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- PARTEA 1: Mișcarea (WASD + Mouse) ---
        float moveZ = Input.GetAxis("Vertical");
        float moveX = Input.GetAxis("Horizontal");
        Vector3 miscare = transform.forward * moveZ + transform.right * moveX;
        controller.Move(miscare * viteza * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * sensitivitateMouse * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivitateMouse * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // --- PARTEA 2: Interacțiunea (Click / Grab) ---
        // Asta e "mâna" ta. Asta e tot ce ai nevoie pentru Mihai.

        // Când dăm click stânga
        if (Input.GetMouseButtonDown(0))
        {
            // Dacă nu ținem nimic în mână...
            if (obiectPrins == null)
            {
                // ... încercăm să prindem ceva
                Ray raza = new Ray(cameraNoastra.position, cameraNoastra.forward);
                RaycastHit hit;
                if (Physics.Raycast(raza, out hit, 3f)) // 3f = 3 metri distanță
                {
                    // Dacă am lovit un obiect care are tag-ul "Grabbable"
                    if (hit.transform.CompareTag("Grabbable"))
                    {
                        // L-am prins!
                        obiectPrins = hit.transform;
                        obiectPrins.SetParent(suportMana); // Îl facem copilul camerei
                        obiectPrins.transform.localPosition = Vector3.zero;
                        obiectPrins.GetComponent<Rigidbody>().isKinematic = true; // Oprim fizica pe el
                        Debug.Log("Am PRINS: " + obiectPrins.name);
                    }
                }
            }
            // Dacă deja ținem ceva în mână...
            else
            {
                // ... îi dăm drumul
                Debug.Log("Am DAT DRUMUL la: " + obiectPrins.name);
                obiectPrins.SetParent(null); // Nu mai e copilul nimănui
                obiectPrins.GetComponent<Rigidbody>().isKinematic = false; // Repornim fizica
                obiectPrins = null; // Nu mai ținem nimic în mână
            }
        }
    }
}