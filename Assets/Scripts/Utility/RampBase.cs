// ColorRamp is a gradient property with an associated Texture2D for passing to shaders
// Currently set to RGBAHalf

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust 
{
	public class RampBase 
    {

        public bool Enable = false;
		public Texture2D Texture  { get { return texture; } }
		public TextureFormat Format { get { return format; } set { format = value; } }
		public int Width { get { return width; } }


		private Texture2D texture; 
		private TextureFormat format = TextureFormat.RGBAFloat;
		private const int width = 1024;

		public void Setup() 
        {
            Init();
			InitTexture();
			Update();
		}

		public void InitTexture() 
        {
			texture = new Texture2D(width, 1, format, false);
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
		}

        public virtual void Init() { }

        public virtual void Update() { }

	}
}