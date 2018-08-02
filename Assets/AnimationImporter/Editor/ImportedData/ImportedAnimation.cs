using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AnimationImporter {
	public class ImportedAnimation {
		public string Name;

		public ImportedAnimationFrame[] Frames = null;

		public bool IsCategory = false;
		public bool IsLooping = true;

		// Duration of each frame
		private List<float> timings = null;

		// Final animation clip; saved here for usage when building the AnimatorController
		public AnimationClip AnimationClip;

		// ================================================================================
		//  Temporary data, only used for first import
		// --------------------------------------------------------------------------------

		// Assuming all sprites are in some array/list and an animation is defined as a continous list of indices
		public int FirstSpriteIndex;
		public int LastSpriteIndex;

		// Used with the indices because we to not have the Frame array yet
		public int Count {
			get {
				return LastSpriteIndex - FirstSpriteIndex + 1;
			}
		}

		// ================================================================================
		//  Public methods
		// --------------------------------------------------------------------------------

		public void SetFrames(ImportedAnimationFrame[] frames) {
			this.Frames = frames;

			CalculateKeyFrameTimings();
		}

		public bool IsInAnimation(ImportedAnimation animation) {
			return this.FirstSpriteIndex >= animation.FirstSpriteIndex && this.LastSpriteIndex <= animation.LastSpriteIndex;
		}

		// ================================================================================
		//  Key Frames
		// --------------------------------------------------------------------------------

		public float GetKeyFrameTime(int i) {
			return timings[i];
		}

		public float GetLastKeyFrameTime(float frameRate) {
			float timePoint = GetKeyFrameTime(Count);
			timePoint -= (1f / frameRate);

			return timePoint;
		}

		private void CalculateKeyFrameTimings() {
			float timeCount;
			timings = new List<float>();

			// first sprite will be set at the beginning of the animation
			timeCount = 0;
			timings.Add(timeCount);

			for (int k = 0; k < Frames.Length; k++) {
				// add duration of frame in seconds
				timeCount += Frames[k].Duration / 1000f;
				timings.Add(timeCount);
			}
		}
	}
}