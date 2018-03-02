#define PI 3.14159265359

// --------------------------------------------------
// Structures
// --------------------------------------------------

// Particle system buffer
struct ParticleStruct
{
    float3 pos;
    float3 vel;
    float4 cd;
    float age;
    float lifespan;
    float mass;
    float momentum;
};

struct MeshStruct
{
    float3 pos;
    float3 normal;
    float3 cd;
};


// --------------------------------------------------
// Functions 
// --------------------------------------------------

float rand(float2 co)
{
	return frac(sin(dot(co.xy,float2(12.9898,78.233))) * 43758.5453123);
}

float3 randomSpherePoint(float3 rand, float scatterVolume = 1.0) 
{
    float3 thetaPhiR = clamp(rand, float3(0,0,0), float3(1,1,1));
    float3 newPoint = float3(0,0,0);

    thetaPhiR.x *= 2. * PI;
    thetaPhiR.y = ((thetaPhiR.y * 2.) - 1.) * 0.5 * PI;
    newPoint.x = cos(thetaPhiR.x) * cos(thetaPhiR.y);
    newPoint.y = sin(thetaPhiR.y);
    newPoint.z = sin(thetaPhiR.x) * cos(thetaPhiR.y);

    // Blend between returning points on surface of sphere vs volume
    return newPoint * lerp(1., sqrt(sqrt(thetaPhiR.z)), scatterVolume);
}

float fit(float val, float inMin, float inMax, float outMin, float outMax) 
{
    return ((outMax - outMin) * (val - inMin) / (inMax - inMin)) + outMin;
}

float2 fit(float2 val, float2 inMin, float2 inMax, float2 outMin, float2 outMax) 
{
    return ((outMax - outMin) * (val - inMin) / (inMax - inMin)) + outMin;
}

float3 bayesian(float3 a, float3 b, float3 c, float2 random) {
    float r = random.x;
    float s = random.y;
    if (r + s >= 1.0) {
        r = 1.-r;
        s = 1.-s;
    }
    return a + ((b-a)*r) + ((c-a)*s);
}

// Return a position within a triangle
float3 bayesianCoordinate(RWStructuredBuffer<MeshStruct> vertices, int3 triangles, float2 random) 
{
    float3 a = vertices[triangles.x].pos;
    float3 b = vertices[triangles.y].pos;
    float3 c = vertices[triangles.z].pos;
    return bayesian(a, b, c, random.xy);
}