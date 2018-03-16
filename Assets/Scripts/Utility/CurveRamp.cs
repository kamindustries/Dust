// ColorRamp is a gradient property with an associated Texture2D for passing to shaders
// Currently set to RGBAHalf

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust 
{
	[System.Serializable]
	public class CurveRamp : RampBase 
    {

		public AnimationCurve Curve;

        public override void Init() 
        {
            Format = TextureFormat.RFloat;
        }

		public override void Update() {
			if (!Texture) Setup();

			Color [] tempArray = new Color[Width];
			for (int i = 0; i < 1024; i++) {
				float time = (float)i/(float)Width;
				tempArray[i].r = Curve.Evaluate(time);
				tempArray[i].g = 0;
				tempArray[i].b = 0;
			}
			
			Texture.SetPixels(tempArray);
			Texture.Apply();
		}

	}
}