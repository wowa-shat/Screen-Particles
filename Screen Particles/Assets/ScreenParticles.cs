using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScreenParticles : MonoBehaviour
{
    #region Public Variables
    [Serializable]
    public enum MovingMode { BlackHole, UpRight, RandomDirection }

    [Space(5)]
    public Camera gameCamera;
    public Camera RTCamera;

    [Space(5)]
    [Header("Particles Settings")]
    public string particlesLayerName;
    public int columnsCount = 60;
    public int rowsCount = 60;
    public Mesh particleMesh;
    public float particleScale = 0.035f;
    [Space(5)]
    [Header("Particles Movement")]
    [Range(-1, 1)]
    public float particlesSpeed;
    public MovingMode movingMode = MovingMode.BlackHole;
    #endregion

    private RenderTexture cameraOutputRT;
    private List<ScreenParticle> particles = new List<ScreenParticle>();
    private bool isMoving = false;

    void Awake()
    {
        cameraOutputRT = new RenderTexture(Screen.width, Screen.height, 24);
        RTCamera.targetTexture = cameraOutputRT;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RenderTexture.active = cameraOutputRT;

            Texture2D texture2D = new Texture2D(cameraOutputRT.width, cameraOutputRT.height);
            texture2D.ReadPixels(new Rect(0, 0, cameraOutputRT.width, cameraOutputRT.height), 0, 0);
            texture2D.Apply();
            int i = 0;
            var rnd = new System.Random((int)Time.time);
            for (int x = 0; x < texture2D.width; x += texture2D.width / columnsCount)
            {
                for (int y = 0; y < texture2D.height; y += texture2D.height / rowsCount)
                {
                    Ray ray = gameCamera.ScreenPointToRay(new Vector3(x, y, 0));
                    RaycastHit hitInfo;
                    Physics.Raycast(ray, out hitInfo);
                    Color color = texture2D.GetPixel(x, y);

                    particles.Add(new ScreenParticle(CreateParticle("particle" + i, color, hitInfo), movingMode, rnd));
                    i++;
                }
            }

            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            //now camera see just particles layer
            gameCamera.cullingMask = 1 << LayerMask.NameToLayer(particlesLayerName);
            isMoving = true;
        }
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var pos = particles[i].particle.transform.position;
                if (particlesSpeed >= 0)
                {
                    particles[i].particle.transform.position = Vector3.MoveTowards(pos, particles[i].target, particlesSpeed * Time.deltaTime);
                }
                else
                {
                    particles[i].particle.transform.position = Vector3.MoveTowards(pos, particles[i].originPos, -particlesSpeed * Time.deltaTime);
                }
            }
        }
    }

    private GameObject CreateParticle(string name, Color color, RaycastHit hitInfo)
    {
        GameObject particle = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        particle.transform.SetParent(hitInfo.collider.gameObject.GetComponent<Transform>());
        particle.GetComponent<MeshFilter>().mesh = particleMesh;
        particle.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
        particle.GetComponent<MeshRenderer>().sharedMaterial.color = color;
        particle.layer = LayerMask.NameToLayer(particlesLayerName);
        particle.transform.localScale = Vector3.one * particleScale;
        particle.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -hitInfo.normal);
        particle.transform.position = hitInfo.point;
        return particle;
    }

    class ScreenParticle
    {
        public GameObject particle;
        public Vector3 originPos;
        public Vector3 target;

        public ScreenParticle(GameObject particle, MovingMode movingMode, System.Random rnd)
        {
            this.particle = particle;

            switch (movingMode)
            {
                case MovingMode.BlackHole:
                    {
                        var rand = new System.Random((int)UnityEngine.Random.Range(float.MinValue, float.MaxValue));
                        this.target = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), this.particle.transform.position.z);
                    }
                    break;
                case MovingMode.UpRight:
                    {
                        this.target = new Vector3(rnd.Next(10, 100), rnd.Next(10, 100), this.particle.transform.position.z);
                    }
                    break;
                case MovingMode.RandomDirection:
                    {
                        Vector3 t = UnityEngine.Random.onUnitSphere * 100f;
                        this.target = new Vector3(t.x, t.y, this.particle.transform.position.z);
                    }
                    break;
                default:
                    throw new Exception("Unknown MovingMode. Please add new case for this mode");
            }

            this.originPos = particle.transform.position;
        }
    }
}