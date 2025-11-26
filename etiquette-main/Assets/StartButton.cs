using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour
{
private GameObject sec;
private startEndController startControl;
private GameObject tt;
    public float moveSpeed = 5f;
    public float moveDistance = 10f; // How far down to move

    // Start is called before the first frame update
    void Start()
    {
        tt = GameObject.Find("tt_ui");
        sec = GameObject.Find("startEndController");
        startControl = sec.GetComponent<startEndController>();
    }

    // Update is called once per frame
    void Update()
    {
          if (Input.GetMouseButtonUp(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                     Debug.Log("Start button clicked!");
                   
                    StartCoroutine(MoveDownAndDestroy(tt));
                }
            }
        }
    }

    void StartGame() {
        startControl.isStarted = true;
       
    }


    
    IEnumerator MoveDownAndDestroy(GameObject obj)
    {
        Vector3 startPos = obj.transform.position;
        Vector3 targetPos = startPos + Vector3.down * moveDistance;
        
        float elapsed = 0f;
        float duration = moveDistance / moveSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            obj.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        
        // Ensure final position
        obj.transform.position = targetPos;
        
        // Destroy after movement complete
        Destroy(obj);
         StartGame();
    }
}
