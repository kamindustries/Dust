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


// --------------------------------------------------
// Functions 
// --------------------------------------------------

float rand(float2 co)
{
	return frac(sin(dot(co.xy,float2(12.9898,78.233))) * 43758.5453);
}

float3 randomSpherePoint(float3 rand, float scatterVolume = 1.0) {
    float3 thetaPhiR = clamp(rand, float3(0,0,0), float3(1,1,1));
    float3 newPoint = float3(0,0,0);

    thetaPhiR.x *= 2. * PI;
    thetaPhiR.y = ((thetaPhiR.y * 2.) - 1.) * 0.5 * PI;
    newPoint.x = cos(thetaPhiR.x) * cos(thetaPhiR.y);
    newPoint.y = sin(thetaPhiR.y);
    newPoint.z = sin(thetaPhiR.x) * cos(thetaPhiR.y);

    return newPoint * lerp(1., sqrt(sqrt(thetaPhiR.z)), scatterVolume);
}

float fit(float val, float inMin, float inMax, float outMin, float outMax) {
    return ((outMax - outMin) * (val - inMin) / (inMax - inMin)) + outMin;
}

float2 fit(float2 val, float2 inMin, float2 inMax, float2 outMin, float2 outMax) {
    return ((outMax - outMin) * (val - inMin) / (inMax - inMin)) + outMin;
}
