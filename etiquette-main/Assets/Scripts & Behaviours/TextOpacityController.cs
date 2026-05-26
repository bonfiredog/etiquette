using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TextOpacityController : MonoBehaviour
{
    [Header("Tags of Text Objects to Monitor")]
    public List<string> monitoredTags = new List<string>();

    [Header("Tag of Fade Zone Objects")]
    public string stationTag = "fade";

    [Header("Check Interval (Seconds)")]
    public float updateInterval = 0.05f;

    [Header("Fade Duration (Seconds)")]
    public float fadeDuration = 0.2f;

    private class TextData
    {
        public Material material;
        public float targetAlpha = 1f;
        public float currentAlpha = 1f;
    }

    private Dictionary<Renderer, TextData> textObjects = new Dictionary<Renderer, TextData>();
    private List<Renderer> fadeZones = new List<Renderer>();

    void Start()
    {
        RefreshFadeZones();
        StartCoroutine(UpdateLoop());
    }

    // Fade zones are static — only needs to run once at start.
    // Call this manually if fade zones ever change at runtime.
    public void RefreshFadeZones()
    {
        fadeZones.Clear();
        GameObject[] zones = GameObject.FindGameObjectsWithTag(stationTag);
        foreach (GameObject zone in zones)
        {
            Renderer rend = zone.GetComponent<Renderer>();
            if (rend != null)
                fadeZones.Add(rend);
        }
    }

    IEnumerator UpdateLoop()
    {
        while (true)
        {
            RefreshTextObjects();
            SetTargetOpacities();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Text objects change frequently (pooling), so refresh on interval.
    void RefreshTextObjects()
    {
        textObjects.Clear();
        foreach (string tag in monitoredTags)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject go in found)
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend == null || textObjects.ContainsKey(rend)) continue;

                TextMeshPro tmp = go.GetComponent<TextMeshPro>();
                if (tmp == null) continue;

                // Calling .material creates a per-instance material, allowing
                // each text object to fade independently.
                Material mat = rend.material;
                float opacity = mat.GetFloat("_Opacity");

                textObjects.Add(rend, new TextData
                {
                    material = mat,
                    targetAlpha = 1f,
                    currentAlpha = opacity
                });
            }
        }
    }

    void SetTargetOpacities()
    {
        foreach (var pair in textObjects)
        {
            bool overlaps = false;
            foreach (Renderer fadeZone in fadeZones)
            {
                if (ZBoundsOverlap(pair.Key, fadeZone))
                {
                    overlaps = true;
                    break;
                }
            }
            pair.Value.targetAlpha = overlaps ? 0f : 1f;
        }
    }

    // Runs every frame, but only does work when a fade is in progress.
    // Reads/writes cached material and alpha — no GetComponent or Find calls.
    void Update()
    {
        float fadeSpeed = Time.deltaTime / fadeDuration;
        foreach (TextData data in textObjects.Values)
        {
            if (Mathf.Approximately(data.currentAlpha, data.targetAlpha)) continue;
            data.currentAlpha = Mathf.MoveTowards(data.currentAlpha, data.targetAlpha, fadeSpeed);
            data.material.SetFloat("_Opacity", data.currentAlpha);
        }
    }

    bool ZBoundsOverlap(Renderer a, Renderer b)
    {
        float aMin = a.bounds.min.z;
        float aMax = a.bounds.max.z;
        float bMin = b.bounds.min.z;
        float bMax = b.bounds.max.z;
        return aMin <= bMax && aMax >= bMin;
    }
}   