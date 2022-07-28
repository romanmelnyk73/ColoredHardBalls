using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePhysics : MonoBehaviour
{
    public struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Color color;

        public Particle(float posRange, float maxVel)
        {
            position.x = Random.value * posRange - posRange/2;
            position.y = Random.value * posRange;
            position.z = Random.value * posRange - posRange / 2;
            velocity.x = Random.value * maxVel - maxVel/2;
            velocity.y = Random.value * maxVel - maxVel/2;
            velocity.z = Random.value * maxVel - maxVel/2;
            color.r = Random.value;
            color.g = Random.value;
            color.b = Random.value;
            color.a = 1;
            //  color = Vector4.one;
        }
    }

    public ComputeShader shader;
    public Mesh particleMesh;
    public Material particleMaterial;
  
    
    public int particlesCount = 512;
    public float particleDiameter = 0.2f;
    public float boxSize = 2.5f;
    float radius;

    ComputeBuffer particlesBuffer;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    Particle[] particlesArray;
  
    int groupSizeX;
    int numOfParticles;
    Bounds bounds;

    int kernelHandle;
   
    MaterialPropertyBlock props;

    void Start()
    {
        radius = 0.5f * particleDiameter;
        kernelHandle = shader.FindKernel("CSMain");
    
        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particlesCount / (float)x);
        numOfParticles = groupSizeX * (int)x;
        
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitParticles();
        InitShader();
    }

    private void InitParticles()
    {
        particlesArray = new Particle[numOfParticles];

        for (int i = 0; i < numOfParticles; i++)
        {
            particlesArray[i] = new Particle(3, 2.05f);
        }
    }

    void InitShader()
    {
        particlesBuffer = new ComputeBuffer(numOfParticles, 10 * sizeof(float));
        particlesBuffer.SetData(particlesArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (particleMesh != null)
        {
            args[0] = (uint)particleMesh.GetIndexCount(0);
            args[1] = (uint)numOfParticles;
            args[2] = (uint)particleMesh.GetIndexStart(0);
            args[3] = (uint)particleMesh.GetBaseVertex(0);
        }
        argsBuffer.SetData(args);

        shader.SetInt("particlesCount", numOfParticles);
        shader.SetFloat("particleDiameter", particleDiameter);

		// bind kernel  CSMain 
		shader.SetBuffer(kernelHandle, "particlesBuffer", particlesBuffer);
        shader.SetVector("limitsXZ", new Vector4(-boxSize+radius, boxSize-radius, -boxSize+radius, boxSize-radius));
        shader.SetFloat("floorY", -boxSize+radius);
        shader.SetFloat("radius", radius);

        particleMaterial.SetFloat("_Radius", radius*2);
        particleMaterial.SetBuffer("particlesBuffer", particlesBuffer);
    }

    void Update()
    {
        int iterations = 5;
        shader.SetFloat("deltaTime", Time.deltaTime/iterations);

        for (int i = 0; i < iterations; i++)
        {
                shader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        }
        
        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, bounds, argsBuffer, 0, props);
    }

    void OnDestroy()
    {
        if (particlesBuffer != null)
        {
            particlesBuffer.Dispose();
        }

        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }
}

