using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScreenParticles : MonoBehaviour
{
    [Serializable]
    public enum MovingMode { BlackHole, OnUnitSphereX100 }

    [Space(5)]
    public Camera gameCamera;
    public RenderTexture cameraOutputRT;
    public MovingMode movingMode = MovingMode.BlackHole;
    [Range(-1,1)]
    public float particlesSpeed;

    [Space(5)]
    [Header("Screen Particles")]
    public string particlesLayerName;
    public int rowParticlesCount = 60;
    public int columnParticlesCount = 60;
    public Mesh particleMesh;
    public float particleScale = 0.035f;

    private List<ScreenParticle> particles = new List<ScreenParticle>();
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

                    particles.Add(new ScreenParticle(CreateParticle("particle", lightClr, hitInfo.point), movingMode));
                }
            }

            //on screen stay only particles
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.cullingMask = 1 << LayerMask.NameToLayer(particlesLayerName);
            isMoving = true;
        }
        if (isMoving)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if(particlesSpeed >= 0)
                {
                    particles[i].particle.transform.position = Vector3.MoveTowards(particles[i].particle.transform.position, particles[i].target, particlesSpeed / 100f);
                }
                else
                {
                    particles[i].particle.transform.position = Vector3.MoveTowards(particles[i].particle.transform.position, particles[i].originPos, -particlesSpeed / 100f);
                }
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

    class ScreenParticle
    {
        public GameObject particle;
        public Vector3 target;
        public Vector3 originPos;

        public ScreenParticle(GameObject particle, MovingMode movingMode)
        {
            this.particle = particle;

            switch (movingMode)
            {
                case MovingMode.BlackHole:
                    {
                        var rnd = new System.Random((int)Time.time);
                        this.target = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), this.particle.transform.position.z);
                    }
                    break;
                case MovingMode.OnUnitSphereX100:
                    {
                        Vector3 t = UnityEngine.Random.onUnitSphere * 100f;
                        this.target = new Vector3(t.x, t.y, this.particle.transform.position.z);
                    }
                    break;
                default:
                    throw new Exception("Unknown MovingMode. Please add new mode to enum");
            }

            this.originPos = particle.transform.position;
        }
    }
}