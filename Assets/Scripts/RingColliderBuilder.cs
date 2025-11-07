using UnityEngine;

public class RingColliderBuilder : MonoBehaviour
{
    public int segments = 16;
    public float ringRadius = 0.30f;
    public float tubeRadius = 0.009f;
    public bool useCapsules = true;
    public float capsuleLength = 0.10f;

    [ContextMenu("Rebuild ring colliders")]
    public void Build()
    {
        // șterge segmentele vechi
        for (int i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name.StartsWith("Seg"))
#if UNITY_EDITOR
                DestroyImmediate(transform.GetChild(i).gameObject);
#else
                Destroy(transform.GetChild(i).gameObject);
#endif

        // creează segmentele
        for (int i = 0; i < Mathf.Max(1, segments); i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            var seg = new GameObject("Seg" + i);
            seg.layer = gameObject.layer;
            seg.transform.SetParent(transform, false);
            seg.transform.localPosition = new Vector3(Mathf.Cos(a)*ringRadius, 0f, Mathf.Sin(a)*ringRadius);

            if (useCapsules)
            {
                seg.transform.LookAt(transform.position);
                seg.transform.Rotate(0f, 90f, 0f);
                var cap = seg.AddComponent<CapsuleCollider>();
                cap.direction = 2;
                cap.radius = tubeRadius;
                cap.height = capsuleLength + tubeRadius*2f;
            }
            else
            {
                var sph = seg.AddComponent<SphereCollider>();
                sph.radius = tubeRadius;
            }
        }
    }
}
