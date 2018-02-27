// ColorRamp is a gradient property with an associated Texture2D for passing to shaders
// Currently set to RGBAHalf
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust 
{
	[System.Serializable]
	public class ColorRampRange : ColorRamp {
        public float Range = 1f;
    }
}