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
        fovAngle = 30.0f;
        perceptionRadius = 10.0f;
        minHeight = 15;
        maxHeight = 50;


        // Initilize Boids
        boidCount = 20;
        boids = new Boid[boidCount];
        for(int i = 0; i < boids.Length; i++)
        {
            boids[i].id = i;
            boids[i].position = new Vector3(Random.Range(-50, 50), 
                                            Random.Range(minHeight, maxHeight), 
                                            Random.Range(-50, 50));
            boids[i].forward = Vector3.forward;
            boids[i].velocity = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
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
            Vector3 force = boids[i].CalcBoundaryRepulsion(minBoundary, maxBoundary,
                                                           minHeight, maxHeight, 
                                                           maxSpeed, maxSteerForce);
            boids[i].ApplyForce(force);

            // apply and update force
            boids[i].Update(maxSpeed, Time.deltaTime);
        }

        int j = 1;
        for (; j < boidCount; j++)
        {
            if(boids[0].CanSeeNeighbor(boids[j], perceptionRadius, fovAngle))
            {
                neighbourDetected = true;
                firstHitIndex = j;
                break;
            }
        }
        if (j == boidCount)
        {
            neighbourDetected = false;
            firstHitIndex = 0;
        }
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

            Color boidColor = Color.gray;
            if(firstHitIndex != 0) boidColor = (i == firstHitIndex) ? Color.yellow : Color.gray; 
            if(i == 0) boidColor = (neighbourDetected) ? Color.red : Color.green;

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

        Boid b = boids[0];
        Vector3 pos = b.position;
        Vector3 forward = b.GetDirection();

        Gizmos.color = neighbourDetected ? new Color(1, 0, 0, 0.25f) : new Color(0, 1, 0, 0.25f);
        int rays = 16;
        for (int i = 0; i < rays; i++)
        {
            Quaternion circleRotation = Quaternion.AngleAxis(i * (360f / rays), forward);
            Vector3 upOrSide = (Mathf.Abs(forward.y) > 0.9f) ? Vector3.right : Vector3.up;
            Vector3 spreadDirection = Vector3.Cross(forward, upOrSide).normalized;

            Vector3 rayDir = Quaternion.AngleAxis(fovAngle, Vector3.Cross(forward, spreadDirection)) * forward;
            rayDir = circleRotation * rayDir;
            Gizmos.DrawRay(pos, rayDir * perceptionRadius);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos, forward * perceptionRadius);
    }

}
