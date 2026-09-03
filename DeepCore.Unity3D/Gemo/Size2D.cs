using System;

namespace DeepCore.GUI.Gemo
{
	public class Size2D
	{
		public float width;
		
		public float height;

		public Size2D() {}

		public Size2D(float w, float h) {
			width = w;
			height = h;
		}

		public void swap() {
			float temp = width;
			width = height;
			height = temp;
		}

		public bool Equals(Size2D r) {
			if (r != null) {
				return ((width == r.width) &&
				        (height == r.height));
			}
			return false;
		}
	}
}

