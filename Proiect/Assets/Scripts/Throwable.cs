using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Throwable : MonoBehaviour
{
    [HideInInspector] public Vector3 lastReleasePosition;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }
    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject != null && args.interactorObject.transform != null)
        {
            lastReleasePosition = args.interactorObject.transform.position;
        }
        else
        {
            lastReleasePosition = transform.position;
        }
    }

}
