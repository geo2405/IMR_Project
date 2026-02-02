using UnityEngine;
using System.Collections;

public class GhostAI : MonoBehaviour
{
    public float viteza = 2.0f;
    public float distantaDeAtac = 1.5f;
    public float intarziereStart = 3.0f;

    [Header("UI - Trage Panelul Aici")]
    public GameObject panelMoarte;
    public AudioSource sunetSperietura;

    private Transform tinta;
    private bool jocTerminat = false;
    private bool poateSaMiste = false;

    void Start()
    {
        // Tintim Camera (Ochii)
        if (Camera.main != null) tinta = Camera.main.transform;

        // Ne asiguram ca e stins la inceput
        if (panelMoarte != null) panelMoarte.SetActive(false);

        StartCoroutine(AsteaptaStart());
    }

    IEnumerator AsteaptaStart()
    {
        yield return new WaitForSeconds(intarziereStart);
        poateSaMiste = true;
    }

    void Update()
    {
        // Daca jocul e gata, asteptam SPACE ca sa iesim
        if (jocTerminat)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("👋 Iesim din joc!");
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
            return;
        }

        if (tinta == null || !poateSaMiste) return;

        // Logica de urmarire
        transform.LookAt(tinta.position);
        transform.position = Vector3.MoveTowards(transform.position, tinta.position, viteza * Time.deltaTime);

        if (Vector3.Distance(transform.position, tinta.position) < distantaDeAtac)
        {
            TeAmPrins();
        }
    }

    void TeAmPrins()
    {
        jocTerminat = true;
        Debug.Log("😱 MORT! Apasa SPACE ca sa iesi.");

        // DOAR APRINDEM PANELUL. Nu il mai mutam, nu il mai rotim.
        // El trebuie sa fie deja pus bine in scena.
        if (panelMoarte != null) panelMoarte.SetActive(true);

        if (sunetSperietura != null) sunetSperietura.Play();

        // Oprim timpul ca sa nu treaca fantoma prin tine
        Time.timeScale = 0;
    }
}