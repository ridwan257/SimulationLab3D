using UnityEngine;
using Boids;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Mesh mesh;

    [Header("Boid Handller")]
    [SerializeField] private float boidSizeFactor;
    [SerializeField] private bool obstracleHited;
    [SerializeField] private bool obstracleHasLeft;
    [SerializeField] private bool obstracleHasRight;
    [SerializeField] private float obstraclePercetionRadius;
    [SerializeField] private LayerMask obstracleLayer;

    [Header("Boid Movement Settings")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxSteerForce;
    [SerializeField] private float fovAngle;
    [SerializeField] private float perceptionRadius;
    [SerializeField] private float boundaryRepulsionFactor;
    [SerializeField] private float levelFlightFactor;
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
    private Boid[] boids;
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
        maxSteerForce = 3.0f;
        fovAngle = 45.0f;
        boundaryRepulsionFactor = 2.0f;
        levelFlightFactor = 0.5f;
        perceptionRadius = 20.0f;
        obstraclePercetionRadius = 4.0f;
        chohesionFactor = 0.25f;
        alignmentFactor = 0.75f;
        separationFactor = 1.25f;
        minHeight = 5;
        maxHeight = 20;

        obstracleLayer = 1 << LayerMask.NameToLayer("Obstracle");

        // Initilize Boids
        boidCount = 100;
        boids = new Boid[boidCount];
        for(int i = 0; i < boids.Length; i++)
        {
            boids[i] = new Boid
            {
                id = i,
                position = new Vector3(Random.Range(-50, 50),
                                       Random.Range(5, 15),
                                       Random.Range(-50, 50)),
                //position = new Vector3(29.2f,
                //                       8,
                //                       20),
                forward = Vector3.forward,
                velocity = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5)),
                //velocity = new Vector3(0, 0f, -4f),
                baseColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))
            };
            boids[i].color = boids[i].baseColor;
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
        for (int i = 0; i < boidCount; i++)
        {
            Boid b = boids[i];
            Vector3 forwardDir = b.GetDirection();

            // 1. Calculation of other Forces
            Vector3 repulsionForce = b.CalcBoundaryRepulsion(minBoundary, maxBoundary, minHeight, 
                                                           maxHeight, maxSpeed, maxSteerForce);
            Vector3 levelFlightForce = b.CalcLevelFlightForce(maxSpeed, maxSteerForce);

            // 2. Desired direction calcualtion for flocking behavoir
            Vector3 cohesionDir = b.CalcCohesionDirection(boids, perceptionRadius, fovAngle);
            Vector3 alignmentDir = b.CalcAlignmentDirection(boids, perceptionRadius, fovAngle);
            Vector3 separationDir = b.CalcSeparationDirection(boids, perceptionRadius, fovAngle);

            // 3. Convert Directions to Steer Forces
            Vector3 steerCohesion = b.CalcSteerForce(cohesionDir, maxSpeed, maxSteerForce);
            Vector3 steerAlignment = b.CalcSteerForce(alignmentDir, maxSpeed, maxSteerForce);
            Vector3 steerSeparation = b.CalcSteerForce(separationDir, maxSpeed, maxSteerForce);


            // 4. Detection of Obstracle
            Vector3 obstractleForce = Vector3.zero;
            Quaternion rotationMachine = Quaternion.LookRotation(forwardDir);
            Vector3 rightWingDir = rotationMachine * Vector3.right;
            Vector3 leftWingDir = rotationMachine * Vector3.left;
            
            float wingSpan = boidSizeFactor * 0.5f;
            Vector3 rightWingTip = b.position + (rightWingDir * wingSpan);
            Vector3 leftWingTip = b.position + (leftWingDir * wingSpan);

            bool tipHited = Physics.Raycast(b.position, forwardDir, obstraclePercetionRadius, obstracleLayer);
            bool leftHited = Physics.Raycast(leftWingTip, forwardDir, obstraclePercetionRadius, obstracleLayer);
            bool rightHited = Physics.Raycast(rightWingTip, forwardDir, obstraclePercetionRadius, obstracleLayer);

            obstracleHited = tipHited || leftHited || rightHited;


            if (obstracleHited)
            {

                obstracleHasRight = Physics.Raycast(b.position, rightWingDir, obstraclePercetionRadius, obstracleLayer);
                obstracleHasLeft = Physics.Raycast(b.position, leftWingDir, obstraclePercetionRadius, obstracleLayer);
                if (!obstracleHasRight)
                {
                    obstractleForce = b.CalcSteerForce(rightWingDir, maxSpeed, maxSteerForce);
                    //Debug.Log("RIght side is free");
                }
                else if (!obstracleHasLeft)
                {
                    obstractleForce = b.CalcSteerForce(leftWingDir, maxSpeed, maxSteerForce);
                    //Debug.Log("Left side is free");
                }
                else
                {
                    // both sides are blocked!
                    obstractleForce = b.CalcSteerForce(Vector3.up, maxSpeed, maxSteerForce);
                }
            } else
            {
                obstracleHasRight = false;
                obstracleHasLeft = false;
            }
            
            // 5. Combine Forces
            Vector3 totalForce = (boundaryRepulsionFactor * repulsionForce) +
                                 (levelFlightFactor * levelFlightForce) +
                                 (10.0f * obstractleForce) + 
                                 (chohesionFactor * steerCohesion) +
                                 (alignmentFactor * steerAlignment) +
                                 (separationFactor * steerSeparation);

            
            // 6. Applying Force
            b.ApplyForce(totalForce);
            b.Update(maxSpeed, Time.deltaTime);

            // 7. Boid Color Handlings
            bool foundNeighbor = false;
            for (int j = 0; j < boids.Length; j++)
            {
                if (i == j) continue;
                if (b.CanSeeNeighbor(boids[j], perceptionRadius, fovAngle))
                {
                    b.color = boids[j].color;
                    foundNeighbor = true;
                    break;
                }
            }
            if (!foundNeighbor) b.color = b.baseColor;

            boids[i] = b;
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

        //Boid b = boids[0];
        //Vector3 pos = b.position;
        //Vector3 forward = b.GetDirection();

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

        for (int i = 0; i < boids.Length; i++)
        {
            Boid b = boids[i];
            Vector3 pos = b.position;
            Vector3 forward = b.GetDirection();

            Quaternion rotationMachine = Quaternion.LookRotation(b.forward);
            Vector3 rightWingDir = rotationMachine * Vector3.right;
            Vector3 leftWingDir = rotationMachine * Vector3.left;
            Vector3 noseDir = rotationMachine * Vector3.forward;
            Vector3 backDir = rotationMachine * Vector3.back;

            float wingSpan = boidSizeFactor * 0.5f;
            Vector3 rightWingTip = b.position + (rightWingDir * wingSpan);
            Vector3 leftWingTip = b.position + (leftWingDir * wingSpan);
            Vector3 noseTip = b.position + (noseDir * wingSpan);
            Vector3 backTip = b.position + (backDir * wingSpan);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, forward * obstraclePercetionRadius);
            Gizmos.DrawRay(rightWingTip, forward * obstraclePercetionRadius);
            Gizmos.DrawRay(leftWingTip, forward * obstraclePercetionRadius);


            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, rightWingDir * obstraclePercetionRadius);
            Gizmos.DrawRay(pos, leftWingDir * obstraclePercetionRadius);
            //Gizmos.DrawRay(noseTip, noseDir * obstraclePercetionRadius);
            //Gizmos.DrawRay(backTip, backDir * obstraclePercetionRadius);

        }
    }

}
