using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleRibbonTrailSmoother : MonoBehaviour
{
    public enum UpdateType
    {
        Update,
        LateUpdate,
        FixedUpdate,
        Manual
    }


    private const int AllocatedArraySize = 100;

    [Range(0, 10)]
    public int iterations = 2;
    public bool fixedTimeStep = false;
    public UpdateType updateType = UpdateType.Update;

    private ParticleSystem ps;
    private int rawParticleCount;
    private ParticleSystem.Particle[] rawParticles;
    private List<ParticleSystem.Particle> processedParticles = new List<ParticleSystem.Particle>();


    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        
        if (ps.trails.mode != ParticleSystemTrailMode.Ribbon)
        {
            Debug.LogWarning("This component works only with Ribbon trail mode.");
        }
    }


    private void Update()
    {
        if (updateType == UpdateType.Update)
        {
            Simulate(Time.deltaTime);
        }
    }


    private void FixedUpdate()
    {
        if (updateType == UpdateType.FixedUpdate)
        {
            Simulate(Time.fixedDeltaTime);
        }
    }


    private void LateUpdate()
    {
        if (updateType == UpdateType.LateUpdate)
        {
            Simulate(Time.deltaTime);
        }
    }


    public void Simulate(float deltaTime)
    {
        // Works only with Ribbon trail mode
        if (ps.trails.mode != ParticleSystemTrailMode.Ribbon)
        {
            return;
        }
        
        // Restore previously stored raw particles before simulation
        if (rawParticles != null)
        {
            ps.SetParticles(rawParticles, rawParticleCount);
        }


        // Simulate the particle system
        ps.Simulate(deltaTime, true, false, fixedTimeStep);


        // Store the raw particles before applying smoothing

        if (rawParticles == null)
        {
            rawParticles = new ParticleSystem.Particle[AllocatedArraySize];
        }

        int rawParticlesArraySize = rawParticles.Length;

        while (rawParticlesArraySize < ps.particleCount)
        {
            rawParticlesArraySize += AllocatedArraySize;
        }

        if (rawParticlesArraySize > rawParticles.Length)
        {
            Array.Resize(ref rawParticles, rawParticlesArraySize);
        }

        rawParticleCount = ps.GetParticles(rawParticles);


        ApplySmoothing();
    }


    public void Clear()
    {
        rawParticleCount = 0;
        rawParticles = null;
        processedParticles.Clear();
    }


    private void ApplySmoothing()
    {
        if (ps.particleCount < 3)
        {
            return;
        }


        processedParticles.Clear();

        for (int i = 0; i < rawParticleCount; i++)
        {
            processedParticles.Add(rawParticles[i]);
        }

        processedParticles.Sort(SortByLifetime);


        for (int k = 0; k < iterations; k++)
        {
            List<ParticleSystem.Particle> smoothParticles = new List<ParticleSystem.Particle>();

            int count = processedParticles.Count;

            for (int i = 0; i < count - 1; i++)
            {
                ParticleSystem.Particle p0 = processedParticles[i];
                ParticleSystem.Particle p1 = processedParticles[(i + 1) % count];

                Vector3 pos0 = p0.position;
                Vector3 pos1 = p1.position;

                // Chaikin's corner-cutting algorithm: generate two new points (Q and R) between pos0 and pos1
                Vector3 Q = Vector3.Lerp(pos0, pos1, 0.25f); // 25% point
                Vector3 R = Vector3.Lerp(pos0, pos1, 0.75f); // 75% point

                ParticleSystem.Particle pQ = new ParticleSystem.Particle();
                pQ.position = Q;
                pQ.startLifetime = Mathf.Lerp(p0.startLifetime, p1.startLifetime, 0.25f);
                pQ.remainingLifetime = Mathf.Lerp(p0.remainingLifetime, p1.remainingLifetime, 0.25f);
                pQ.startSize = Mathf.Lerp(p0.startSize, p1.startSize, 0.25f);
                pQ.startSize3D = Vector3.Lerp(p0.startSize3D, p1.startSize3D, 0.25f);
                pQ.startColor = Color.Lerp(p0.startColor, p1.startColor, 0.25f);
                pQ.velocity = Vector3.Lerp(p0.velocity, p1.velocity, 0.25f);

                ParticleSystem.Particle pR = new ParticleSystem.Particle();
                pR.position = R;
                pR.startLifetime = Mathf.Lerp(p0.startLifetime, p1.startLifetime, 0.75f);
                pR.remainingLifetime = Mathf.Lerp(p0.remainingLifetime, p1.remainingLifetime, 0.75f);
                pR.startSize = Mathf.Lerp(p0.startSize, p1.startSize, 0.75f);
                pR.startSize3D = Vector3.Lerp(p0.startSize3D, p1.startSize3D, 0.75f);
                pR.startColor = Color.Lerp(p0.startColor, p1.startColor, 0.75f);
                pR.velocity = Vector3.Lerp(p0.velocity, p1.velocity, 0.75f);

                smoothParticles.Add(pQ);
                smoothParticles.Add(pR);
            }

            // Preserve the start and end particles
            smoothParticles.Insert(0, processedParticles[0]);
            smoothParticles.Add(processedParticles[processedParticles.Count - 1]);

            processedParticles = smoothParticles;
        }

        ps.SetParticles(processedParticles.ToArray());
    }


    private int SortByLifetime(ParticleSystem.Particle p1, ParticleSystem.Particle p2)
    {
        return (p1.startLifetime - p1.remainingLifetime).CompareTo(p2.startLifetime - p2.remainingLifetime);
    }
}
