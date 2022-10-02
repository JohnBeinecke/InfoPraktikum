using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteColorSwitch : MonoBehaviour
{
    [SerializeField] private Color[] colors;
    [SerializeField] private float timeBetweenColors;

    private int currColor;
    private float currTimer;
    private Image img;
    
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
        img.color = colors[0];
        currColor = 0;
        currTimer = timeBetweenColors;
    }

    // Update is called once per frame
    void Update()
    {
        if (currTimer > 0f)
        {
            currTimer -= Time.deltaTime;

            if (currTimer <= 0f)
            {
                currTimer = timeBetweenColors;
                currColor++;


            }
            
            img.color = new Color(
                Mathf.Lerp(colors[(currColor+1) % colors.Length].r,colors[currColor % colors.Length].r, currTimer / timeBetweenColors),
                Mathf.Lerp(colors[(currColor+1) % colors.Length].g,colors[currColor % colors.Length].g, currTimer / timeBetweenColors),
                Mathf.Lerp(colors[(currColor+1) % colors.Length].b, colors[currColor % colors.Length].b, currTimer / timeBetweenColors));
        }
    }
}
