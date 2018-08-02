using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AnimationImporter {
	public class ImportedAnimationFrame {
		// ================================================================================
		//  Naming
		// --------------------------------------------------------------------------------

		private string name;
		public string Name {
			get { return name; }
			set { name = value; }
		}

		// ================================================================================
		//  Properties
		// --------------------------------------------------------------------------------

		public int X;
		public int Y;
		public int Width;
		public int Height;

		public int Duration; // in milliseconds as part of an animation

		// reference to the Sprite that was created with this frame information
		public Sprite Sprite = null;
	}
}