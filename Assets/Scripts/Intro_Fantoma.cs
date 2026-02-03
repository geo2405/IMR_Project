using UnityEngine;
using TMPro;
using System.Collections;

public class IntroFantoma : MonoBehaviour
{
    [Header("Referinte")]
    public GameObject fantoma; // Trage Fantoma aici
    public float timpAsteptare = 10f;

    void Start()
    {
        // La inceput, ne asiguram ca fantoma NU se misca
        if (fantoma != null)
        {
            // Dezactivam scriptul de miscare al fantomei (ex: NavMeshAgent sau scriptul tau)
            // Presupunem ca scriptul tau de miscare se numeste FantomaAI
            var scriptMiscare = fantoma.GetComponent<MonoBehaviour>();
            // Inlocuieste MonoBehaviour cu numele real al scriptului tau de miscare daca il stii
            scriptMiscare.enabled = false;
        }

        // Pornim numaratoarea inversa
        StartCoroutine(PornesteJocul());
    }

    IEnumerator PornesteJocul()
    {
        // Asteptam 10 secunde
        yield return new WaitForSeconds(timpAsteptare);

        // 1. Ascundem mesajul
        gameObject.SetActive(false);

        // 2. Activam fantoma
        if (fantoma != null)
        {
            var scriptMiscare = fantoma.GetComponent<MonoBehaviour>();
            scriptMiscare.enabled = true;
            Debug.Log("👻 FANTOMA S-A TREZIT! FUGI!");
        }
    }
}