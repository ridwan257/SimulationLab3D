using System;
using Unity.VisualScripting;
using UnityEngine;


namespace Boids
{
	struct Boid
	{   
        public int id;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 forward;

		public Vector3 GetDirection()
		{
            if (velocity.sqrMagnitude > 1e-6f)
            {
                forward = velocity.normalized;
            }
            return forward;
        }

        public void Update(float maxVelocity, float deltaTime)
        {
            velocity += acceleration * deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxVelocity);
            position += velocity * deltaTime;
            acceleration = Vector3.zero;

        }

        public void ApplyForce(Vector3 force)
        {
            acceleration += force;
        }

        public readonly Vector3 CalcSteerForce(Vector3 targetDirection, float maxSpeed, 
                                               float maxSteerForce, float weight = 1.0f)
        {
            if (targetDirection.sqrMagnitude < 1e-6f) return Vector3.zero;

            Vector3 desired = targetDirection.normalized * maxSpeed;
            Vector3 steer = desired - velocity;
            steer *= weight;

            return Vector3.ClampMagnitude(steer, maxSteerForce);
        }

        public readonly bool CanSeeNeighbor(Boid other, float pRadius, float fovAngle)
        {
            Vector3 diff = other.position - position;
            float distance = diff.magnitude;

            if (distance > pRadius || distance < 1e-4f) return false;

            Vector3 dirToNeighbor = diff.normalized;

            float dotResult = Vector3.Dot(forward, dirToNeighbor);

            float cosAngle = Mathf.Cos(fovAngle * Mathf.Deg2Rad);

            return dotResult > cosAngle;
        }

        public readonly Vector3 CalcBoundaryRepulsion(Vector3 minBoundary, Vector3 maxBoundary, float minHeight,
                                                      float maxHeight, float maxSpeed, float maxSteerForce)
        {
            Vector3 force = Vector3.zero;
            // Check for boundary
            if (position.y < minHeight)
                force += CalcSteerForce(Vector3.up, maxSpeed, maxSteerForce, 2.0f);
            else if (position.y > maxHeight)
                force += CalcSteerForce(Vector3.down, maxSpeed, maxSteerForce, 1.5f);

            if (position.x < minBoundary.x)
                force += CalcSteerForce(Vector3.right, maxSpeed, maxSteerForce, 1.5f);
            else if (position.x > maxBoundary.x)
                force += CalcSteerForce(Vector3.left, maxSpeed, maxSteerForce, 1.5f);

            if (position.z < minBoundary.z)
                force += CalcSteerForce(Vector3.forward, maxSpeed, maxSteerForce, 1.5f);
            else if (position.z > maxBoundary.z)
                force += CalcSteerForce(Vector3.back, maxSpeed, maxSteerForce, 1.5f);

            return force;
        }

        public readonly Vector3 CalcSeparationDirection(Boid[] boids, float pRadius, float fovAngle)
        {
            Vector3 targetDirection = Vector3.zero;
            int count = 0;
            float cosAngle = Mathf.Cos(fovAngle * Mathf.Deg2Rad);

            for (int i = 0; i < boids.Length; i++)
            {
                if (boids[i].id == id) continue;

                Vector3 diff = boids[i].position - position;
                float distance = diff.magnitude;
                if (distance > pRadius || distance < 1e-4f) continue;

                Vector3 dirToNeighbor = diff / distance;
                float dotResult = Vector3.Dot(forward, dirToNeighbor);
                
                if(dotResult < cosAngle) continue;

                // Now boid is inside perception range
                count++;
                targetDirection -= dirToNeighbor / distance;


            }
            if(count > 0) targetDirection /= count;
            
            return targetDirection;
        }
    };

    

}