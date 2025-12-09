using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class SheepAgent : NetworkBehaviour
{
    [Header("Movement")]
    public float calmSpeed = 0.6f;        // speed when calm & with neighbours
    public float scaredSpeed = 3f;        // speed when running from player
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

    [Header("Lone wandering")]
    public float loneWanderSpeed = 0.4f;     // slow speed when alone
    public float loneWanderJitter = 0.3f;    // how much random steering when alone
    public float maxLoneWanderRadius = 5f;   // how far from spawn they’re allowed to drift

    [Header("Other")]
    public float wanderNoise = 0.5f;   // extra jitter when scared
    public float calmDamping = 5f;     // how quickly they slow down when we want them to stop

    private static readonly List<SheepAgent> AllSheep = new();

    private Vector3 _velocity;
    private Vector3 _spawnPoint;

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
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        _spawnPoint = transform.position;   // remember home position for lone wandering
        _velocity = Vector3.zero;
    }

    private void Update()
    {
        if (!IsServer) return;

        // 1) Flocking info
        ComputeFlocking(out Vector3 separation, out Vector3 alignment,
                        out Vector3 cohesion, out bool hasNeighbors);

        // 2) Threat (player) info
        bool hasThreat;
        Vector3 flee = ComputeFleeFromPlayers(out hasThreat);

        Vector3 steering = Vector3.zero;
        float maxSpeed = 0f;

        if (hasThreat)
        {
            // SCARED: full flocking + flee + wander
            steering += separation * separationWeight;
            steering += alignment * alignmentWeight;
            steering += cohesion * cohesionWeight;
            steering += flee * fleeWeight;
            steering += Random.insideUnitSphere * wanderNoise;

            maxSpeed = scaredSpeed;
        }
        else
        {
            // CALM behaviour

            // Always avoid overlapping
            steering += separation * separationWeight;

            if (hasNeighbors)
            {
                // Calm but in a group: drift toward & with the herd
                steering += alignment * alignmentWeight;
                steering += cohesion * cohesionWeight;
                maxSpeed = calmSpeed;
            }
            else
            {
                // Calm and ALONE: gentle wandering near spawn point
                steering += Random.insideUnitSphere * loneWanderJitter;

                // If too far from home, bias back toward spawn
                Vector3 toHome = _spawnPoint - transform.position;
                toHome.y = 0;
                if (toHome.magnitude > maxLoneWanderRadius)
                {
                    steering += toHome.normalized;   // a soft pull back
                }

                maxSpeed = loneWanderSpeed;
            }
        }

        steering.y = 0;

        // 3) Update velocity
        _velocity += steering * Time.deltaTime;

        if (maxSpeed <= 0f)
        {
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, calmDamping * Time.deltaTime);
        }
        else if (_velocity.magnitude > maxSpeed)
        {
            _velocity = _velocity.normalized * maxSpeed;
        }

        // Extra damping when we *want* them basically stopped (very small speeds)
        if (!hasThreat && maxSpeed < 0.01f)
        {
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, calmDamping * Time.deltaTime);
        }

        // 4) Move & rotate
        Vector3 newPos = transform.position + _velocity * Time.deltaTime;
        newPos.y = transform.position.y;
        transform.position = newPos;

        if (_velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    private void ComputeFlocking(out Vector3 separation,
                                 out Vector3 alignment,
                                 out Vector3 cohesion,
                                 out bool hasNeighbors)
    {
        separation = Vector3.zero;
        alignment  = Vector3.zero;
        cohesion   = Vector3.zero;
        hasNeighbors = false;

        int neighborCount = 0;

        foreach (var other in AllSheep)
        {
            if (other == this) continue;

            Vector3 toOther = other.transform.position - transform.position;
            float dist = toOther.magnitude;
            if (dist > neighborRadius) continue;

            neighborCount++;

            // Separation
            if (dist < separationRadius && dist > 0.0001f)
            {
                separation -= toOther / dist;
            }

            alignment += other.transform.forward;
            cohesion  += other.transform.position;
        }

        if (neighborCount > 0)
        {
            hasNeighbors = true;

            alignment /= neighborCount;
            alignment = alignment.normalized;

            cohesion  /= neighborCount;
            cohesion   = (cohesion - transform.position).normalized;
        }
    }

    private Vector3 ComputeFleeFromPlayers(out bool hasThreat)
    {
        hasThreat = false;

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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_spawnPoint == Vector3.zero ? transform.position : _spawnPoint,
                              maxLoneWanderRadius);
    }
}
