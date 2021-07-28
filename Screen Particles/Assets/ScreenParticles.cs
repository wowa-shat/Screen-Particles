using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScreenParticles : MonoBehaviour
{
    [Serializable]
    public enum MovingMode { BlackHole, RandomDirection }

    #region Public Variables
    [Space(5)]
    [Header("Cameras")]
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
    public GameObject blackHolePointObj;
    #endregion

    private RenderTexture cameraOutputRT;
    private ScreenParticle[,] particles;
    private Vector3 blackHolePoint;

    void Awake()
    {
        //create render texture and make this target texture
        cameraOutputRT = new RenderTexture(Screen.width, Screen.height, 24);
        RTCamera.targetTexture = cameraOutputRT;

        blackHolePoint = blackHolePointObj.transform.localPosition;

        particles = new ScreenParticle[cameraOutputRT.width, cameraOutputRT.height];
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
            for (int x = 0; x < particles.GetLength(0); x += particles.GetLength(0) / columnsCount)
            {
                for (int y = 0; y < particles.GetLength(1); y += particles.GetLength(1) / rowsCount)
                {
                    Ray ray = gameCamera.ScreenPointToRay(new Vector3(x, y, 0));
                    Physics.Raycast(ray, out RaycastHit hitInfo);
                    Color color = texture2D.GetPixel(x, y);

                    particles[x, y] = new ScreenParticle(CreateParticle("particle" + i, color, hitInfo), true, movingMode, blackHolePoint);
                    i++;
                }
            }

            //camera see just particles layer
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.cullingMask = 1 << LayerMask.NameToLayer(particlesLayerName);
        }
    }

    private void FixedUpdate()
    {
        for (int x = 0; x < particles.GetLength(0); x += particles.GetLength(0) / columnsCount)
        {
            for (int y = 0; y < particles.GetLength(1); y += particles.GetLength(1) / rowsCount)
            {
                if (particles[x, y] != null)
                {
                    if (particles[x, y].isMoving)
                    {
                        var localPos = particles[x, y].Particle.transform.localPosition;

                        if (particlesSpeed >= 0)
                        {
                            particles[x, y].Particle.transform.localPosition = Vector3.MoveTowards(localPos, particles[x, y].Target, particlesSpeed * Time.deltaTime);
                        }
                        else
                        {
                            particles[x, y].Particle.transform.localPosition = Vector3.MoveTowards(localPos, particles[x, y].OriginPos, -particlesSpeed * Time.deltaTime);
                        }
                    }

                    if (particles[x, y].Particle.transform.localPosition == particles[x, y].Target)
                    {
                        if (particlesSpeed < 0)
                        {
                            particles[x, y].isMoving = true;
                            particles[x, y].Particle.SetActive(true);
                        }
                        else
                        {
                            particles[x, y].isMoving = false;
                            particles[x, y].Particle.SetActive(false);
                        }
                    }
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
        public GameObject Particle { get; private set; }
        public Vector3 OriginPos { get; private set; }
        public Vector3 Target { get; private set; }
        public bool isMoving;

        public ScreenParticle(GameObject particle, bool isMoving, MovingMode movingMode, Vector3 blackHolePoint)
        {
            Particle = particle;
            OriginPos = particle.transform.localPosition;
            this.isMoving = isMoving;

            switch (movingMode)
            {
                case MovingMode.BlackHole:
                    {
                        Target = blackHolePoint;
                    }
                    break;
                case MovingMode.RandomDirection:
                    {
                        Vector3 target = UnityEngine.Random.onUnitSphere * 10;
                        Target = new Vector3(target.x, target.y, Particle.transform.localPosition.z);
                    }
                    break;
                default:
                    throw new Exception("Unknown MovingMode. Please add case for this mode");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(blackHolePointObj.transform.position, 0.05f);
    }
}