using UnityEngine;
using Boids;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Mesh mesh;

    [Header("Boid Handller")]
    [SerializeField] private Boid[] boids;
    [SerializeField] private float boidSizeFactor;

    [Header("Boid Movement Settings")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxSteerForce;
    [SerializeField] private float fovAngle;
    [SerializeField] private float perceptionRadius;
    [SerializeField] private float chohesionFactor;
    [SerializeField] private float alignmentFactor;
    [SerializeField] private float separationFactor;

    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private bool neighbourDetected;
    [SerializeField] private int firstHitIndex;

    private Vector3 minBoundary;
    private Vector3 maxBoundary;
    private int boidCount;
    private static readonly Vector3[] baseVertices = {
        new(0, 0, 1), new(0.5f,  0.0f, -1f), new(-0.5f, 0, -1),
        new(0, 0, 0), new(0.0f, -0.5f, -1f), new( 0.0f, 0, -1)
    };
    private static readonly int[] baseTriangles =
    {
        0, 1, 2, 3, 4, 5
    };


    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "PaperPlane";
        mesh.MarkDynamic();
        
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Check Boundary
        GetGroundBoundary(out minBoundary, out maxBoundary);
        
    }

    private void Start()
    {
        // Initilize all other param
        boidSizeFactor = 2.0f;
        maxSpeed = 8.0f;
        maxSteerForce = 2.0f;
        fovAngle = 40.0f;
        perceptionRadius = 20.0f;
        chohesionFactor = 0.6f;
        alignmentFactor = 1f;
        separationFactor = 0.8f;
        minHeight = 15;
        maxHeight = 50;


        // Initilize Boids
        boidCount = 100;
        boids = new Boid[boidCount];
        for(int i = 0; i < boids.Length; i++)
        {
            boids[i] = new Boid
            {
                id = i,
                position = new Vector3(Random.Range(-50, 50),
                                       Random.Range(30, 40),
                                       Random.Range(-50, 50)),
                forward = Vector3.forward,
                velocity = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5)),
                color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))
            };
        }

        GenerateMesh();
    }

    private void Update()
    {
        UpdateBoidsAndBoundary();
        // Drawing all boid
        GenerateMesh();
    }

    private void UpdateBoidsAndBoundary()
    {
        for (int i = 0; i < boids.Length; i++)
        {
            // boundary force
            Vector3 repulsionForce = boids[i].CalcBoundaryRepulsion(minBoundary, maxBoundary,
                                                           minHeight, maxHeight, 
                                                           maxSpeed, maxSteerForce);

            Vector3 cohesionDir = boids[i].CalcCohesionDirection(boids, perceptionRadius, fovAngle);
            Vector3 alignmentDir = boids[i].CalcAlignmentDirection(boids, perceptionRadius, fovAngle);
            Vector3 separationDir = boids[i].CalcSeparationDirection(boids, perceptionRadius, fovAngle);

            Vector3 steerCohesion  = boids[i].CalcSteerForce(cohesionDir, maxSpeed, maxSteerForce);
            Vector3 steerAlignment = boids[i].CalcSteerForce(alignmentDir, maxSpeed, maxSteerForce);
            Vector3 steerSeparation = boids[i].CalcSteerForce(separationDir, maxSpeed, maxSteerForce);

            Vector3 flockingForce = 2.5f * repulsionForce +
                                    chohesionFactor * steerCohesion + 
                                    alignmentFactor * steerAlignment + 
                                    separationFactor * steerSeparation;
            boids[i].ApplyForce(flockingForce);

            for(int j=0; j < boids.Length; j++)
            {
                if (i == j) continue;

                if(boids[i].CanSeeNeighbor(boids[j], perceptionRadius, fovAngle))
                {
                    boids[i].color = boids[j].color;
                    break;
                }
            }

            // apply and update force
            boids[i].Update(maxSpeed, Time.deltaTime);
        }

        // int j = 1;
        // for (; j < boidCount; j++)
        // {
        //     if(boids[0].CanSeeNeighbor(boids[j], perceptionRadius, fovAngle))
        //     {
        //         neighbourDetected = true;
        //         firstHitIndex = j;
        //         break;
        //     }
        // }
        // if (j == boidCount)
        // {
        //     neighbourDetected = false;
        //     firstHitIndex = 0;
        // }
    }

    private void GetGroundBoundary(out Vector3 min, out Vector3 max)
    {
        // Check Boundary
        GameObject g = GameObject.Find("PlayGround");
        Transform t = g.transform.Find("Ground");
        Renderer r = t.GetComponent<Renderer>();
        min = r.bounds.min;
        max = r.bounds.max;
    }

    private void GenerateMesh()
    {
        Vector3[] vertices = new Vector3[boidCount * baseVertices.Length];
        int[] triangles = new int[boidCount * baseTriangles.Length];
        Color[] colors = new Color[boidCount * baseVertices.Length];

        Vector3 tranformedVector;
        Quaternion rotationMachine;

        for (int i = 0; i < boidCount; i++)
        {
            rotationMachine = Quaternion.LookRotation(boids[i].GetDirection());

            Color boidColor = boids[i].color;

            for (int j = 0; j < baseVertices.Length; j++)
            {
                int vertexIndex = i * baseVertices.Length + j;

                tranformedVector = rotationMachine * (boidSizeFactor * baseVertices[j]);

                vertices[vertexIndex] = boids[i].position + tranformedVector;
                colors[vertexIndex] = boidColor;
            }

            for (int t = 0; t < baseTriangles.Length; t++)
            {
                triangles[i * baseTriangles.Length + t] = (i * baseVertices.Length) + baseTriangles[t];
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    private void OnDrawGizmos()
    {
        if (boids == null) return;

        //foreach (Boid boid in boids)
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawLine(
        //            boid.position,
        //            boid.position + 1.5f * boidSizeFactor * boid.GetDirection()
        //        );
        //}

        // Boid b = boids[0];
        // Vector3 pos = b.position;
        // Vector3 forward = b.GetDirection();

        // Gizmos.color = neighbourDetected ? new Color(1, 0, 0, 0.25f) : new Color(0, 1, 0, 0.25f);
        // int rays = 16;
        // for (int i = 0; i < rays; i++)
        // {
        //     Quaternion circleRotation = Quaternion.AngleAxis(i * (360f / rays), forward);
        //     Vector3 upOrSide = (Mathf.Abs(forward.y) > 0.9f) ? Vector3.right : Vector3.up;
        //     Vector3 spreadDirection = Vector3.Cross(forward, upOrSide).normalized;

        //     Vector3 rayDir = Quaternion.AngleAxis(fovAngle, Vector3.Cross(forward, spreadDirection)) * forward;
        //     rayDir = circleRotation * rayDir;
        //     Gizmos.DrawRay(pos, rayDir * perceptionRadius);
        // }
        // Gizmos.color = Color.blue;
        // Gizmos.DrawRay(pos, forward * perceptionRadius);
    }

}
