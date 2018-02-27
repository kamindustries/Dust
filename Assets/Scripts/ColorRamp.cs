// ColorRamp is a gradient property with an associated Texture2D for passing to shaders
// Currently set to RGBAHalf

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust 
{
	[System.Serializable]
	public class ColorRamp {

		public Gradient Gradient;

		private Texture2D texture;
		public Texture2D Texture 
		{
			get
			{
				return texture;
			}
		}
		private const int width = 1024;
		

		public void Setup() {
			texture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false);
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			
			Update();
		}

		public void Update() {
			if (!texture) Setup();

			Color [] tempArray = new Color[width];
			for (int i = 0; i < 1024; i++) {
				float time = (float)i/(float)width;
				tempArray[i] = Gradient.Evaluate(time);
			}
			texture.SetPixels(tempArray);
			texture.Apply();
		}

	}
}