using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class FPS : MonoBehaviour
{

    public int fpsTarget = -1;
    public float updateInterval = 0.5f;
    private float lastInterval;
    private int frames = 0;
    private float fps;
    void Start()
    {
        Application.targetFrameRate = fpsTarget;
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;
    }
    // Update is called once per frame  
    void Update()
    {
        ++frames;
        float timeNow = Time.realtimeSinceStartup;
        if (timeNow >= lastInterval + updateInterval)
        {
            fps = frames / (timeNow - lastInterval);
            frames = 0;
            lastInterval = timeNow;
        }
    }
    void OnGUI()
    {
        GUI.Label(new Rect(40, 40, 100, 30), fps.ToString());
    }
}