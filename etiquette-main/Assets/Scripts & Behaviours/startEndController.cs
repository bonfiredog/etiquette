using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class startEndController : MonoBehaviour
{
    public GameObject startend;
    private StationScheduler ss;
    private TrainControl tc;
    private float endTimer1 = 180;
    public bool isStarted = false;
    private GameObject ttstart;
    private GameObject ttquit;
    private CanvasGroup canvasGroup;
    private bool isFading = false; // guard flag

    void Start()
    {
        ss = GameObject.Find("stationScheduleController").GetComponent<StationScheduler>();
        tc = GameObject.Find("trainController").GetComponent<TrainControl>();
        ttstart = GameObject.Find("startbutton");
        ttquit = GameObject.Find("quitbutton");

        // Get or add a CanvasGroup on the startend object
        canvasGroup = startend.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = startend.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        startend.SetActive(false);
    }

    void Update()
    {
        if (isStarted == true && ss.nextStationName == "London Paddington" && tc.docked == true && ss.milesToNextStation <= 1)
        {
            if (!isFading) // only trigger once
            {
                startend.SetActive(true);
                StartCoroutine(FadeIn(startend, 2f));
                isFading = true;
                if (ss.ending == false) { ss.ending = true; }
            }

            if (endTimer1 > 0)
            {
                endTimer1 -= 1 * Time.deltaTime;
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }

    IEnumerator FadeIn(GameObject obj, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}