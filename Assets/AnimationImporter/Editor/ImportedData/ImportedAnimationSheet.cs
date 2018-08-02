using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Linq;

namespace AnimationImporter {
	public class ImportedAnimationSheet {
		public string Name { get; set; }
		public string AssetDirectory { get; set; }

		public int Width { get; set; }
		public int Height { get; set; }
		public Vector2Int SourceSize { get; set; }

		public bool UsePivot { get; set; }
		public Vector2 Pivot { get; set; }

		public int MaxTextureSize {
			get {
				return Mathf.Max(Width, Height);
			}
		}

		public List<ImportedAnimationFrame> Frames = new List<ImportedAnimationFrame>();
		public List<ImportedAnimation> Animations = new List<ImportedAnimation>();
		public List<ImportedAnimationSlice> Slices = new List<ImportedAnimationSlice>();

		public bool HasAnimations {
			get {
				return Animations != null && Animations.Count > 0;
			}
		}

		private Dictionary<string, ImportedAnimation> animationDatabase = null;

		private PreviousImportSettings previousImportSettings = null;
		public PreviousImportSettings PreviousImportSettings {
			get {
				return previousImportSettings;
			}
			set {
				previousImportSettings = value;
			}
		}
		public bool HasPreviousTextureImportSettings {
			get {
				return previousImportSettings != null && previousImportSettings.HasPreviousTextureImportSettings;
			}
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		// Get animation by name; used when updating an existing AnimatorController 
		public AnimationClip GetClip(string clipName) {
			if (animationDatabase == null) {
				BuildIndex();
			}

			if (animationDatabase.ContainsKey(clipName)) {
				return animationDatabase[clipName].AnimationClip;
			}

			return null;
		}

		/* 
			Get animation by name; used when creating an AnimatorOverrideController
			we look for similar names so the OverrideController is still functional in cases where more specific or alternative animations are not present
			idle <- idle
			idleAlt <- idle
		*/
		public AnimationClip GetClipOrSimilar(string clipName) {
			AnimationClip clip = GetClip(clipName);

			if (clip != null) {
				return clip;
			}

			List<ImportedAnimation> similarAnimations = new List<ImportedAnimation>();
			foreach (var item in Animations) {
				if (item.IsCategory) { continue; }
				if (clipName.Contains(item.Name)) {
					similarAnimations.Add(item);
				}
			}

			if (similarAnimations.Count > 0) {
				ImportedAnimation similar = similarAnimations.OrderBy(x => x.Name.Length).Reverse().First();
				return similar.AnimationClip;
			}

			return null;
		}

		public void CreateAnimation(ImportedAnimation anim, string basePath, string masterName, AnimationTargetObjectType targetType) {
			const string nameDelimiter = "@";

			AnimationClip clip;
			string fileName = basePath + "/" + masterName + nameDelimiter + anim.Name + ".anim";
			bool isLooping = anim.IsLooping;

			// Check if animation file already exists
			clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
			if (clip != null) {
				// Get previous animation settings
				targetType = PreviousImportSettings.GetAnimationTargetFromExistingClip(clip);
			}
			else {
				clip = new AnimationClip();
				AssetDatabase.CreateAsset(clip, fileName);
			}

			// Change loop settings
			if (isLooping) {
				clip.wrapMode = WrapMode.Loop;
				clip.SetLoop(true);
			}
			else {
				clip.wrapMode = WrapMode.Clamp;
				clip.SetLoop(false);
			}

			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[anim.Count + 1]; // one more than sprites because we repeat the last sprite

			for (int i = 0; i < anim.Count; i++) {
				ObjectReferenceKeyframe keyFrame = new ObjectReferenceKeyframe { time = anim.GetKeyFrameTime(i) };

				Sprite sprite = anim.Frames[i].Sprite;
				keyFrame.value = sprite;
				keyFrames[i] = keyFrame;
			}

			// Repeating the last frame at a point "just before the end" so the animation gets its correct length

			ObjectReferenceKeyframe lastKeyFrame = new ObjectReferenceKeyframe { time = anim.GetLastKeyFrameTime(clip.frameRate) };

			Sprite lastSprite = anim.Frames[anim.Count - 1].Sprite;
			lastKeyFrame.value = lastSprite;
			keyFrames[anim.Count] = lastKeyFrame;

			// Save curve into clip, either for SpriteRenderer, Image, or both
			if (targetType == AnimationTargetObjectType.SpriteRenderer) {
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.SpriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.ImageCurveBinding, null);
			}
			else if (targetType == AnimationTargetObjectType.Image) {
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.SpriteRendererCurveBinding, null);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.ImageCurveBinding, keyFrames);
			}
			else if (targetType == AnimationTargetObjectType.SpriteRendererAndImage) {
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.SpriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.ImageCurveBinding, keyFrames);
			}

			EditorUtility.SetDirty(clip);
			anim.AnimationClip = clip;
		}

		public void ApplyGlobalFramesToAnimationFrames() {
			for (int i = 0; i < Animations.Count; i++) {
				ImportedAnimation anim = Animations[i];

				anim.SetFrames(Frames.GetRange(anim.FirstSpriteIndex, anim.Count).ToArray());
			}
		}

		// ================================================================================
		//  Determine looping state of animations
		// --------------------------------------------------------------------------------

		public void SetNonLoopingAnimations(List<string> nonLoopingAnimationNames) {
			Regex nonLoopingAnimationsRegex = GetRegexFromNonLoopingAnimationNames(nonLoopingAnimationNames);

			foreach (var item in Animations) {
				if (item.IsCategory) { continue; }
				item.IsLooping = ShouldLoop(nonLoopingAnimationsRegex, item.Name);
			}
		}

		private bool ShouldLoop(Regex nonLoopingAnimationsRegex, string name) {
			if (!string.IsNullOrEmpty(nonLoopingAnimationsRegex.ToString())) {
				if (nonLoopingAnimationsRegex.IsMatch(name)) {
					return false;
				}
			}

			return true;
		}

		private Regex GetRegexFromNonLoopingAnimationNames(List<string> value) {
			string regexString = string.Empty;
			if (value.Count > 0) {
				// Add word boundaries to treat non-regular expressions as exact names
				regexString = string.Concat("\\b", value[0], "\\b");
			}

			for (int i = 1; i < value.Count; i++) {
				string anim = value[i];
				// Add or to speed up the test rather than building N regular expressions
				regexString = string.Concat(regexString, "|", "\\b", anim, "\\b");
			}

			return new System.Text.RegularExpressions.Regex(regexString);
		}

		// ================================================================================
		//  Sprite Data
		// --------------------------------------------------------------------------------

		public SpriteMetaData[] GetSpriteSheet(SpriteAlignment spriteAlignment, Vector2 pivotPoint) {
			SpriteMetaData[] metaData = new SpriteMetaData[Frames.Count];

			for (int i = 0; i < Frames.Count; i++) {
				ImportedAnimationFrame spriteInfo = Frames[i];
				SpriteMetaData spriteMetaData = new SpriteMetaData();

				// Sprite alignment
				spriteMetaData.alignment = (int)spriteAlignment;
				if (spriteAlignment == SpriteAlignment.Custom) {
					spriteMetaData.pivot = pivotPoint;
				}

				spriteMetaData.name = spriteInfo.Name;
				spriteMetaData.rect = new Rect(spriteInfo.X, spriteInfo.Y, spriteInfo.Width, spriteInfo.Height);

				metaData[i] = spriteMetaData;
			}

			return metaData;
		}

		public void ApplySpriteNamingScheme(SpriteNamingScheme namingScheme) {
			const string nameDelimiter = "_";
			const string fileNameDelimiter = "@";

			if (namingScheme == SpriteNamingScheme.Classic) {
				for (int i = 0; i < Frames.Count; i++) {
					Frames[i].Name = Name + " " + i.ToString("D2");
				}
			}
			else {
				foreach (var anim in Animations) {
					if (anim.IsCategory) { continue; }
					for (int i = 0; i < anim.Frames.Length; i++) {
						var animFrame = anim.Frames[i];

						switch (namingScheme) {
							case SpriteNamingScheme.FileAnimationZero:
								animFrame.Name = Name + nameDelimiter + anim.Name + nameDelimiter + i.ToString("D2");
								break;
							case SpriteNamingScheme.FileAnimationOne:
								animFrame.Name = Name + nameDelimiter + anim.Name + nameDelimiter + (i + 1).ToString("D2");
								break;
							case SpriteNamingScheme.AnimationZero:
								animFrame.Name = anim.Name + nameDelimiter + i.ToString("D2");
								break;
							case SpriteNamingScheme.AnimationOne:
								animFrame.Name = anim.Name + nameDelimiter + (i + 1).ToString("D2");
								break;
							case SpriteNamingScheme.FileAtAnimationZero:
								animFrame.Name = Name + fileNameDelimiter + anim.Name + nameDelimiter + i.ToString("D2");
								break;
							case SpriteNamingScheme.FileAtAnimationOne:
								animFrame.Name = Name + fileNameDelimiter + anim.Name + nameDelimiter + (i + 1).ToString("D2");
								break;
						}
					}
				}
			}

			// Remove unused frames from the list so they don't get created for the sprite sheet
			for (int i = Frames.Count - 1; i >= 0; i--) {
				if (string.IsNullOrEmpty(Frames[i].Name)) {
					Frames.RemoveAt(i);
				}
			}
		}

		public void ApplyCreatedSprites(Sprite[] sprites) {
			if (sprites == null) {
				return;
			}

			// Add final Sprites to frames by comparing names
			// as we can't be sure about the right order of the sprites
			for (int i = 0; i < sprites.Length; i++) {
				Sprite sprite = sprites[i];

				for (int k = 0; k < Frames.Count; k++) {
					if (Frames[k].Name == sprite.name) {
						Frames[k].Sprite = sprite;
						break;
					}
				}
			}
		}

		// ================================================================================
		//  Private methods
		// --------------------------------------------------------------------------------

		private void BuildIndex() {
			animationDatabase = new Dictionary<string, ImportedAnimation>();

			for (int i = 0; i < Animations.Count; i++) {
				ImportedAnimation anim = Animations[i];
				if (anim.IsCategory) { continue; }
				animationDatabase[anim.Name] = anim;
			}
		}
	}
}