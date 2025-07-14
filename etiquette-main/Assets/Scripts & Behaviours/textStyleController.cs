using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class textStyleController : MonoBehaviour
{
    [HideInInspector]
    public Color color1;
    [HideInInspector]
    public Color color2;
    [HideInInspector]
    public Color color3;
    [HideInInspector]
    public Color color4;
    [HideInInspector]
    public Color color5;
    [HideInInspector]
    public Color gwrcolor;
    [HideInInspector]
    public Color greycolor;
    [HideInInspector]
    public List<Color> baseColors;

    // Start is called before the first frame update
    void Start()
    {
        //Define the colors (with opacity);
        color1 = new Color(128f, 128f, 176f);
        color2 = new Color(142f, 121f, 170f);
        color3 = new Color(155f, 55f, 80f);
        color4 = new Color(102f, 153f, 54f);
        color5 = new Color(43f, 84f, 108f);
        gwrcolor = new Color(1f, 46f, 4f);
        greycolor = new Color(51f, 51f, 51f);
        baseColors.Add(color1);
        baseColors.Add(color2);
        baseColors.Add(color3);
        baseColors.Add(color4);
        baseColors.Add(color5);
    }

}
