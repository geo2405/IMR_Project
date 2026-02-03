using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // DOAR pentru Keyboard
#endif

public class WelcomeSequenceVR : MonoBehaviour
{
    [Header("Pages")]
    [TextArea(2, 5)]
    public string[] pages;

    [Header("UI References")]
    public TMP_Text messageText;
    public TMP_Text hintText;
    public CanvasGroup canvasGroup;

    [Header("VR Layout")]
    public float distanceFromCamera = 1.4f;
    public Vector3 canvasScale = new Vector3(0.001f, 0.001f, 0.001f);

    [Header("Animation")]
    public float fadeDuration = 0.25f;

    int pageIndex = 0;
    bool isActive = false;
    bool triggerWasPressed = false;

    float startTime;
public float inputDelay = 0.5f;
float lastTriggerTime = 0f;
public float triggerCooldown = 0.6f; // secunde


    // =========================
    // UNITY
    // =========================
    void Start()
    {
        startTime = Time.time;

        AttachToCamera();
        pageIndex = 0;

        if (pages == null || pages.Length == 0)
        {
            pages = new[]
            {
                "FIIVerse îți oferă un tur rapid.",
                "Vorbește cu profesorii și colectează semnături.",
                "Succes! Explorează mediul."
            };
        }

        ShowPage(pageIndex);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        StartCoroutine(Fade(0f, 1f));
        isActive = true;
    }

    void Update()
    {
        if (!isActive)
            return;

        if (NextPressed())
        {
            NextPage();
        }
    }

    // =========================
    // CAMERA ATTACH
    // =========================
    void AttachToCamera()
    {
        Camera cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("WelcomeSequenceVR: Main Camera not found.");
            return;
        }

        transform.SetParent(cam.transform, false);
        transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
        transform.localRotation = Quaternion.identity;
        transform.localScale = canvasScale;
    }

    // =========================
    // PAGE LOGIC
    // =========================
    void ShowPage(int index)
    {
        messageText.text = pages[index];
        hintText.text = $"Trigger / Space → Next ({index + 1}/{pages.Length})";
    }

    void NextPage()
    {
        pageIndex++;

        if (pageIndex >= pages.Length)
        {
            StartCoroutine(Close());
            return;
        }

        ShowPage(pageIndex);
    }

    // =========================
    // INPUT (ROBUST PENTRU QUEST)
    // =========================
    bool NextPressed()
{
    // Delay inițial (evită skip instant la start)
    if (Time.time - startTime < inputDelay)
        return false;

#if ENABLE_INPUT_SYSTEM
    // PC / Simulator
    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        return true;
#endif

    List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    InputDevices.GetDevicesWithCharacteristics(
        InputDeviceCharacteristics.Controller,
        devices
    );

    foreach (var device in devices)
    {
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue))
        {
            // apăsare intenționată
            if (triggerValue > 0.75f &&
                !triggerWasPressed &&
                Time.time - lastTriggerTime > triggerCooldown)
            {
                triggerWasPressed = true;
                lastTriggerTime = Time.time;
                return true;
            }

            // reset doar când e complet eliberat
            if (triggerValue < 0.15f)
            {
                triggerWasPressed = false;
            }
        }
    }

    return false;
}
    // =========================
    // ANIMATION
    // =========================
    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    IEnumerator Close()
    {
        isActive = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return Fade(1f, 0f);
        Destroy(gameObject);
    }
}
