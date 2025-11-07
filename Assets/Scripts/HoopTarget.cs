using System.Collections.Generic;
using UnityEngine;

public class HoopTarget : MonoBehaviour
{
    public float pointsPerMeter = 5f;
    public int minPoints = 1;


    private void OnTriggerEnter(Collider other)
    {
        //verific daca intra in trigger
        var ball = other.attachedRigidbody
            ? other.attachedRigidbody.GetComponent<ThrowableBall>()
            : other.GetComponentInParent<ThrowableBall>();

        if (!ball) return;

        float distance = Vector3.Distance(ball.LastReleasePosition, transform.position);

        // puncte = max(minPoints, distanta * coeficient), rotunjit la intreg
        int points = Mathf.Max(minPoints, Mathf.RoundToInt(distance * pointsPerMeter));

        // incrementeaz scorul
        ScoreManager.Instance?.AddScore(points, other.bounds.center);
    }

    
}
