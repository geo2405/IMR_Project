using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ThrowableBall : MonoBehaviour
{
    public Vector3 LastReleasePosition { get; private set; }

    public Transform respawnPoint;
    public float fallBelowYToRespawn = -5f;
    XRGrabInteractable grab;
    Rigidbody rb;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        LastReleasePosition = transform.position;
    }

    void Update()
    {
        if (respawnPoint && transform.position.y < fallBelowYToRespawn)
            ResetToRespawn();
    }

    public void ResetToRespawn()
    {
        // fortez drop pt toti interactorii
        if (grab.isSelected && grab.interactionManager != null)
        {
            var copy = new List<IXRSelectInteractor>(grab.interactorsSelecting);
            foreach (var interactor in copy)
                grab.interactionManager.SelectExit(interactor, grab);
        }

        //dezactivez grab ca sa nu interfereze cu hover-ul
        grab.enabled = false;

        // mut apoi reactivez
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (respawnPoint)
            transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        Physics.SyncTransforms();
        rb.isKinematic = false;
        rb.WakeUp();

        // interactorii refac hover-ul
        StartCoroutine(ReenableGrabNextFrame());
    }

    IEnumerator ReenableGrabNextFrame()
    {
        yield return null; //astept 1 frame
        grab.enabled = true;
    }
}
