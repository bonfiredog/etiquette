using UnityEngine;

public class delayregister : MonoBehaviour
{
 void Awake() { createText.delayGen = this.gameObject; }
void OnDestroy() { createText.delayGen = null; }
}
