using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TextOpacityController : MonoBehaviour
{
    [Header("Tags of Text Objects to Monitor")]
    public List<string> monitoredTags = new List<string>();

    [Header("Tag of Station Objects")]
    public string stationTag = "station";

    [Header("Update Interval (Seconds)")]
    public float updateInterval = 1f;

    [Header("Fade Duration (Milliseconds)")]
    public float fadeDurationMs = 250f;

    private class TextData
    {
        public TextMeshPro tmp;
        public Renderer renderer;
        public float targetAlpha = 1f;
    }

    private Dictionary<Renderer, TextData> textObjects = new Dictionary<Renderer, TextData>();
    private List<Renderer> stationObjects = new List<Renderer>();

    void Start()
    {
        StartCoroutine(UpdateOpacityLoop());
    }

    IEnumerator UpdateOpacityLoop()
    {
        while (true)
        {
            RefreshObjects();
            SetTargetOpacities();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void RefreshObjects()
    {
        textObjects.Clear();
        stationObjects.Clear();

        foreach (string tag in monitoredTags)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject go in found)
            {
                TextMeshPro tmp = go.GetComponent<TextMeshPro>();
                Renderer rend = go.GetComponent<Renderer>();

                if (tmp != null && rend != null && !textObjects.ContainsKey(rend))
                {
                    TextData data = new TextData
                    {
                        tmp = tmp,
                        renderer = rend,
                        targetAlpha = tmp.color.a
                    };
                    textObjects.Add(rend, data);
                }
            }
        }

        GameObject[] stations = GameObject.FindGameObjectsWithTag(stationTag);
        foreach (GameObject station in stations)
        {
            Renderer rend = station.GetComponent<Renderer>();
            if (rend != null)
            {
                stationObjects.Add(rend);
            }
        }
    }

    void SetTargetOpacities()
    {
        foreach (var pair in textObjects)
        {
            Renderer textRenderer = pair.Key;
            TextData data = pair.Value;

            bool overlaps = false;

            foreach (Renderer stationRenderer in stationObjects)
            {
                if (ZBoundsOverlap(textRenderer, stationRenderer))
                {
                    overlaps = true;
                    break;
                }
            }

            data.targetAlpha = overlaps ? 0f : 1f;
        }
    }

    void Update()
    {
        float fadeSpeed = Time.deltaTime / (fadeDurationMs / 1000f);

        foreach (var pair in textObjects.Values)
        {
            TextMeshPro tmp = pair.tmp;
            float currentAlpha = tmp.color.a;
            float targetAlpha = pair.targetAlpha;

            if (Mathf.Approximately(currentAlpha, targetAlpha))
                continue;

            float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed);

            Color color = tmp.color;
            color.a = newAlpha;
            tmp.color = color;
        }
    }

    bool ZBoundsOverlap(Renderer a, Renderer b)
    {
        float aMin = a.bounds.min.z;
        float aMax = a.bounds.max.z;

        float bMin = b.bounds.min.z;
        float bMax = b.bounds.max.z;

        return (aMin <= bMax && aMax >= bMin);
    }
}
