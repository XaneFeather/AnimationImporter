using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AnimationImporter {
	public class AnimationImporterSharedConfig : ScriptableObject {
		private const string PrefsPrefix = "ANIMATION_IMPORTER_";

		[SerializeField]
#pragma warning disable IDE0044 // Add readonly modifier
		private List<string> animationNamesThatDoNotLoop = new List<string>() { "death" };
#pragma warning restore IDE0044 // Add readonly modifier
		public List<string> AnimationNamesThatDoNotLoop { get { return animationNamesThatDoNotLoop; } }

		[SerializeField]
		private bool automaticImporting = false;
		public bool AutomaticImporting {
			get {
				return automaticImporting;
			}
			set {
				automaticImporting = value;
			}
		}

		[SerializeField]
		private float spritePixelsPerUnit = 100f;
		public float SpritePixelsPerUnit {
			get {
				return spritePixelsPerUnit;
			}
			set {
				spritePixelsPerUnit = value;
			}
		}

		[SerializeField]
		private AnimationTargetObjectType targetObjectType = AnimationTargetObjectType.SpriteRenderer;
		public AnimationTargetObjectType TargetObjectType {
			get {
				return targetObjectType;
			}
			set {
				targetObjectType = value;
			}
		}

		[SerializeField]
		private SpriteAlignment spriteAlignment = SpriteAlignment.BottomCenter;
		public SpriteAlignment SpriteAlignment {
			get {
				return spriteAlignment;
			}
			set {
				spriteAlignment = value;
			}
		}

		[SerializeField]
		private float spriteAlignmentCustomX = 0;
		public float SpriteAlignmentCustomX {
			get {
				return spriteAlignmentCustomX;
			}
			set {
				spriteAlignmentCustomX = value;
			}
		}

		[SerializeField]
		private float spriteAlignmentCustomY = 0;
		public float SpriteAlignmentCustomY {
			get {
				return spriteAlignmentCustomY;
			}
			set {
				spriteAlignmentCustomY = value;
			}
		}

		[SerializeField]
		private AssetTargetLocation spritesTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SubDirectory, "Sprites");
		public AssetTargetLocation SpritesTargetLocation {
			get { return spritesTargetLocation; }
			set { spritesTargetLocation = value; }
		}

		[SerializeField]
		private AssetTargetLocation animationsTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SubDirectory, "Animations");
		public AssetTargetLocation AnimationsTargetLocation {
			get { return animationsTargetLocation; }
			set { animationsTargetLocation = value; }
		}

		[SerializeField]
		private AssetTargetLocation animationControllersTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SameDirectory, "Animations");
		public AssetTargetLocation AnimationControllersTargetLocation {
			get { return animationControllersTargetLocation; }
			set { animationControllersTargetLocation = value; }
		}

		[SerializeField]
		private SpriteNamingScheme spriteNamingScheme = SpriteNamingScheme.Classic;
		public SpriteNamingScheme SpriteNamingScheme {
			get { return spriteNamingScheme; }
			set { spriteNamingScheme = value; }
		}

		public void RemoveAnimationThatDoesNotLoop(int index) {
			AnimationNamesThatDoNotLoop.RemoveAt(index);
		}

		public bool AddAnimationThatDoesNotLoop(string animationName) {
			if (string.IsNullOrEmpty(animationName) || AnimationNamesThatDoNotLoop.Contains(animationName)) {
				return false;
			}

			AnimationNamesThatDoNotLoop.Add(animationName);

			return true;
		}

		/// <summary>
		/// Specify if the Unity user has preferences for an older version of AnimationImporter
		/// </summary>
		/// <returns><c>true</c>, if the user has old preferences, <c>false</c> otherwise.</returns>
		public bool UserHasOldPreferences() {
			var pixelsPerUnityKey = PrefsPrefix + "spritePixelsPerUnit";
			return PlayerPrefs.HasKey(pixelsPerUnityKey) || EditorPrefs.HasKey(pixelsPerUnityKey);
		}

		private bool HasKeyInPreferences(string key) {
			return PlayerPrefs.HasKey(key) || EditorPrefs.HasKey(key);
		}

		private int GetIntFromPreferences(string intKey) {
			if (PlayerPrefs.HasKey(intKey)) {
				return PlayerPrefs.GetInt(intKey);
			}
			else if (EditorPrefs.HasKey(intKey)) {
				return EditorPrefs.GetInt(intKey);
			}
			else {
				return int.MinValue;
			}
		}

		private float GetFloatFromPreferences(string floatKey) {
			if (PlayerPrefs.HasKey(floatKey)) {
				return PlayerPrefs.GetFloat(floatKey);
			}
			else if (EditorPrefs.HasKey(floatKey)) {
				return EditorPrefs.GetFloat(floatKey);
			}
			else {
				return float.NaN;
			}
		}

		private bool GetBoolFromPreferences(string boolKey) {
			if (PlayerPrefs.HasKey(boolKey)) {
				return System.Convert.ToBoolean(PlayerPrefs.GetInt(boolKey));
			}
			else if (EditorPrefs.HasKey(boolKey)) {
				return EditorPrefs.GetBool(boolKey);
			}
			else {
				return false;
			}
		}

		private string GetStringFromPreferences(string stringKey) {
			if (PlayerPrefs.HasKey(stringKey)) {
				return PlayerPrefs.GetString(stringKey);
			}
			else if (EditorPrefs.HasKey(stringKey)) {
				return EditorPrefs.GetString(stringKey);
			}
			else {
				return string.Empty;
			}
		}
	}
}
