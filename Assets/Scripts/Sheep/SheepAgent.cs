using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class SheepAgent : NetworkBehaviour
{
    [Header("Movement")]
    public float calmSpeed = 0f;     // speed when calm (0 = stand still)
    public float scaredSpeed = 3f;   // speed when running
    public float turnSpeed = 5f;

    [Header("Flocking")]
    public float neighborRadius = 4f;
    public float separationRadius = 1.5f;

    public float separationWeight = 1.5f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.0f;

    [Header("Fleeing")]
    public float fearRadius = 6f;
    public float fleeWeight = 3f;

    [Header("Other")]
    public float wanderNoise = 0.5f;   // random jitter when scared
    public float calmDamping = 5f;     // how fast they slow to a stop when calm

    // Static list so sheep can find each other quickly
    private static readonly List<SheepAgent> AllSheep = new();

    private Vector3 _velocity;

    private void OnEnable()
    {
        AllSheep.Add(this);
    }

    private void OnDisable()
    {
        AllSheep.Remove(this);
    }

    public override void OnNetworkSpawn()
    {
        // Only the server simulates flocking. Clients just receive transforms.
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // Start basically idle
        _velocity = Vector3.zero;
    }

    private void Update()
    {
        if (!IsServer) return;

        // --- 1) Find neighbors for flocking ---
        ComputeFlocking(out Vector3 separation, out Vector3 alignment, out Vector3 cohesion);

        // --- 2) Check for nearby players (by tag "Player") ---
        bool hasThreat;
        Vector3 flee = ComputeFleeFromPlayers(out hasThreat);

        Vector3 steering = Vector3.zero;

        if (hasThreat)
        {
            // When scared: full flock + flee + wander
            steering += separation * separationWeight;
            steering += alignment * alignmentWeight;
            steering += cohesion * cohesionWeight;
            steering += flee * fleeWeight;
            steering += Random.insideUnitSphere * wanderNoise;
        }
        else
        {
            // When calm: only separation so they don't overlap
            steering += separation * separationWeight;
        }

        steering.y = 0;

        // --- 3) Update velocity ---
        _velocity += steering * Time.deltaTime;

        float maxSpeed = hasThreat ? scaredSpeed : calmSpeed;
        if (maxSpeed <= 0f)
        {
            // If calmSpeed is 0, just damp to a stop
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, calmDamping * Time.deltaTime);
        }
        else
        {
            // Clamp to appropriate max speed
            if (_velocity.magnitude > maxSpeed)
                _velocity = _velocity.normalized * maxSpeed;
        }

        // Extra damping when calm so they actually come to rest
        if (!hasThreat)
        {
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, calmDamping * Time.deltaTime);
        }

        // --- 4) Move & rotate ---
        Vector3 newPos = transform.position + _velocity * Time.deltaTime;
        newPos.y = transform.position.y;      // keep on ground plane

        transform.position = newPos;

        if (_velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    private void ComputeFlocking(out Vector3 separation, out Vector3 alignment, out Vector3 cohesion)
    {
        separation = Vector3.zero;
        alignment = Vector3.zero;
        cohesion  = Vector3.zero;

        int neighborCount = 0;

        foreach (var other in AllSheep)
        {
            if (other == this) continue;

            Vector3 toOther = other.transform.position - transform.position;
            float dist = toOther.magnitude;
            if (dist > neighborRadius) continue;

            neighborCount++;

            // Separation: push away if too close
            if (dist < separationRadius && dist > 0.0001f)
            {
                separation -= toOther / dist; // normalized away
            }

            // Alignment: match forward directions
            alignment += other.transform.forward;

            // Cohesion: toward average position
            cohesion += other.transform.position;
        }

        if (neighborCount > 0)
        {
            alignment /= neighborCount;
            alignment = alignment.normalized;

            cohesion  /= neighborCount;
            cohesion   = (cohesion - transform.position).normalized;
        }
    }

    private Vector3 ComputeFleeFromPlayers(out bool hasThreat)
    {
        hasThreat = false;

        // Check for *any* collider inside fear radius, then filter by tag
        Collider[] hits = Physics.OverlapSphere(transform.position, fearRadius);
        Collider closest = null;
        float minDistSq = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Player")) continue;

            float dSq = (h.transform.position - transform.position).sqrMagnitude;
            if (dSq < minDistSq)
            {
                minDistSq = dSq;
                closest = h;
            }
        }

        if (closest == null)
            return Vector3.zero;

        hasThreat = true;
        Vector3 away = transform.position - closest.transform.position;
        away.y = 0;
        return away.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fearRadius);
    }
}
