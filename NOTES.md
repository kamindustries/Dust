~~~
DustParticleSystemKernel(
	uint3 groupID : SV_GroupID, 
      // 3D ID of thread group; range depends on Dispatch call
	  // uint2(0..64,0..64)
   uint3 groupThreadID : SV_GroupThreadID, 
      // 3D ID of thread in a thread group; range depends on numthreads
	  // uint2(0..16,0..16)
   uint groupIndex : SV_GroupIndex, 
      // flattened/linearized SV_GroupThreadID. 
      // groupIndex specifies the index within the group
	  // 0..256
   uint3 id : SV_DispatchThreadID) 
      // = SV_GroupThreadID + (SV_GroupID * numthreads) 
	  // = uint2(0..15,0..15) + (uint2(064,064) * uint2(0..16,0..16))
	  // = uint2(0..15,0..15) + (uint2(1024,1024))
)
~~~