using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ConverterRT : MonoBehaviour
{
    [Serializable]
    public enum MovingMode { BlackHole, Wind }

    [Space(5)]
    public Camera gameCamera;
    public RenderTexture cameraOutputRT;
    public MovingMode movingMode = MovingMode.BlackHole;

    [Space(5)]
    [Header("Screen Particles")]
    public string particlesLayerName;
    public int rowParticlesCount = 60;
    public int columnParticlesCount = 60;
    public Mesh particleMesh;
    public float particleScale = 0.035f;

    private List<GameObject> particlesList = new List<GameObject>();
    private List<Vector3> targetsPosition = new List<Vector3>();
    private bool isMoving = false;

    void Update()
    {
        //init particles
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RenderTexture.active = cameraOutputRT;

            Texture2D texture2D = new Texture2D(cameraOutputRT.width, cameraOutputRT.height);
            texture2D.ReadPixels(new Rect(0, 0, cameraOutputRT.width, cameraOutputRT.height), 0, 0);
            texture2D.Apply();

            for (int x = 0; x <= texture2D.width; x += texture2D.width / rowParticlesCount)
            {
                for (int y = 0; y <= texture2D.height; y += texture2D.height / columnParticlesCount)
                {
                    Ray ray = gameCamera.ScreenPointToRay(new Vector3(x, y, 0));
                    RaycastHit hitInfo;
                    Physics.Raycast(ray, out hitInfo);
                    Color clr = texture2D.GetPixel(x, y);
                    Color lightClr = Color.white * clr;
                    particlesList.Add(CreateParticle("particle", lightClr, hitInfo.point));
                }
            }

            //on screen stay only particles
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.cullingMask = 1 << LayerMask.NameToLayer(particlesLayerName);
        }
        //start moving
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            switch (movingMode)
            {
                case MovingMode.BlackHole:
                    {
                        isMoving = true;
                        for (int i = 0; i < particlesList.Count; i++)
                        {
                            var rnd = new System.Random((int)Time.time);
                            targetsPosition.Add(new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), particlesList[i].transform.position.z));
                        }
                    }
                    break;
                case MovingMode.Wind:
                    {
                        var rnd = new System.Random((int)Time.time);
                        isMoving = true;
                        for (int i = 0; i < particlesList.Count; i++)
                        {
                            targetsPosition.Add(new Vector3((float)(rnd.NextDouble() * (100 - 10) + 10), (float)(rnd.NextDouble() * (100 - 10) + 10), particlesList[i].transform.position.z));
                        }
                    }
                    break;
            }
        }
        if (isMoving)
        {
            for (int i = 0; i < particlesList.Count; i++)
            {
                particlesList[i].transform.position = Vector3.MoveTowards(particlesList[i].transform.position, targetsPosition[i], 0.004f);
            }
        }
    }

    private GameObject CreateParticle(string name, Color color, Vector3 position)
    {
        GameObject particle = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        particle.GetComponent<MeshFilter>().mesh = particleMesh;
        particle.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
        particle.GetComponent<MeshRenderer>().sharedMaterial.color = color;
        particle.layer = LayerMask.NameToLayer(particlesLayerName);
        particle.transform.localScale = Vector3.one * particleScale;

        particle.transform.position = position;
        return particle;
    }
}