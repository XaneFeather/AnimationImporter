using System;
using UnityEngine;

namespace AnimationImporter {
	[Serializable]
	public enum SpriteNamingScheme : int {
		Classic,             // file 0
		FileAnimationZero,   // file_anim_0, ...
		FileAnimationOne,    // file_anim_1, ...
		AnimationZero,       // anim_0, ...
		AnimationOne,        // anim_1, ...
		FileAtAnimationZero, // file@anim00
		FileAtAnimationOne   // file@anim01
	}

	public static class SpriteNaming {
		private static int[] namingSchemesValues = null;
		public static int[] NamingSchemesValues {
			get {
				if (namingSchemesValues == null) {
					InitNamingLists();
				}

				return namingSchemesValues;
			}
		}

		private static string[] namingSchemesDisplayValues = null;
		public static string[] NamingSchemesDisplayValues {
			get {
				if (namingSchemesDisplayValues == null) {
					InitNamingLists();
				}

				return namingSchemesDisplayValues;
			}
		}

		private static void InitNamingLists() {
			var allNamingSchemes = Enum.GetValues(typeof(SpriteNamingScheme));

			namingSchemesValues = new int[allNamingSchemes.Length];
			namingSchemesDisplayValues = new string[allNamingSchemes.Length];

			for (int i = 0; i < allNamingSchemes.Length; i++) {
				SpriteNamingScheme namingScheme = (SpriteNamingScheme)allNamingSchemes.GetValue(i);
				namingSchemesValues[i] = (int)namingScheme;
				namingSchemesDisplayValues[i] = namingScheme.ToDisplayString();
			}
		}

		private static string ToDisplayString(this SpriteNamingScheme namingScheme) {
			switch (namingScheme) {
				case SpriteNamingScheme.Classic:
					return "file 00, file 01, ...";
				case SpriteNamingScheme.FileAnimationZero:
					return "file_anim_00, file_anim_01, ...";
				case SpriteNamingScheme.FileAnimationOne:
					return "file_anim_01, file_anim_02, ...";
				case SpriteNamingScheme.AnimationZero:
					return "anim_00, anim_01, ...";
				case SpriteNamingScheme.AnimationOne:
					return "anim_01, anim_02, ...";
				case SpriteNamingScheme.FileAtAnimationZero:
					return "file@anim_00, file@anim_01, ...";
				case SpriteNamingScheme.FileAtAnimationOne:
					return "file@anim_01, file@anim_02, ...";
			}

			return "";
		}
	}
}