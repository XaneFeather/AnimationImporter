using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor;
using System.IO;

namespace AnimationImporter.Aseprite {
	[InitializeOnLoad]
	public class AsepriteImporter : IAnimationImporterPlugin {
		// ================================================================================
		//  Const
		// --------------------------------------------------------------------------------

		const string AsepriteStandardPathWindows = @"C:\Program Files (x86)\Aseprite\Aseprite.exe";
		const string AsepriteStandardPathMacosx = @"/Applications/Aseprite.app/Contents/MacOS/aseprite";

		public static string StandardApplicationPath {
			get {
				if (Application.platform == RuntimePlatform.WindowsEditor) {
					return AsepriteStandardPathWindows;
				}
				else {
					return AsepriteStandardPathMacosx;
				}
			}
		}

		// ================================================================================
		//  Static constructor, registering plugin
		// --------------------------------------------------------------------------------

		static AsepriteImporter() {
			AsepriteImporter importer = new AsepriteImporter();
			AnimationImporter.RegisterImporter(importer, "ase", "aseprite");
		}

		// ================================================================================
		//  Public methods
		// --------------------------------------------------------------------------------

		public ImportedAnimationSheet Import(AnimationImportJob job, AnimationImporterSharedConfig config) {
			if (CreateSpriteAtlasAndMetaFile(job)) {
				AssetDatabase.Refresh();

				ImportedAnimationSheet animationSheet = CreateAnimationSheetFromMetaData(job, config);

				return animationSheet;
			}

			return null;
		}

		public bool IsValid() {
			return AnimationImporter.Instance != null && AnimationImporter.Instance.SharedData != null
				&& File.Exists(AnimationImporter.Instance.AsepritePath);
		}

		// ================================================================================
		//  Private methods
		// --------------------------------------------------------------------------------

		// parses a JSON file and creates the raw data for ImportedAnimationSheet from it
		private static ImportedAnimationSheet CreateAnimationSheetFromMetaData(AnimationImportJob job, AnimationImporterSharedConfig config) {
			string textAssetFilename = job.DirectoryPathForSprites + "/" + job.Name + ".json";
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textAssetFilename);

			if (textAsset != null) {
				JsonObject jsonObject = JsonObject.Parse(textAsset.ToString());
				ImportedAnimationSheet animationSheet = GetAnimationInfo(jsonObject);

				if (animationSheet == null) {
					return null;
				}

				if (!animationSheet.HasAnimations) {
					Debug.LogWarning("No Animations found in Aseprite file. Use Aseprite Tags to assign names to Animations.");
				}

				animationSheet.PreviousImportSettings = job.PreviousImportSettings;

				animationSheet.SetNonLoopingAnimations(config.AnimationNamesThatDoNotLoop);

				// Delete JSON file afterwards
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(textAsset));

				return animationSheet;
			}
			else {
				Debug.LogWarning("Problem with JSON file: " + textAssetFilename);
			}

			return null;
		}

		/// <summary>
		/// Calls the Aseprite application which then should output a png with all sprites and a corresponding JSON
		/// </summary>
		/// <returns></returns>
		private static bool CreateSpriteAtlasAndMetaFile(AnimationImportJob job) {
			char delimiter = '\"';
			string parameters = "--data " + delimiter + job.Name + ".json" + delimiter + " --sheet " + delimiter + job.Name + ".png" + delimiter + " --sheet-pack --list-tags --list-slices --format json-array " + delimiter + job.FileName + delimiter;

			if (!string.IsNullOrEmpty(job.AdditionalCommandLineArguments)) {
				parameters = job.AdditionalCommandLineArguments + " " + parameters;
			}

			bool success = CallAsepriteCli(AnimationImporter.Instance.AsepritePath, job.AssetDirectory, parameters) == 0;

			// Move png and json file to subfolder
			if (success && job.DirectoryPathForSprites != job.AssetDirectory) {
				// Create subdirectory
				if (!Directory.Exists(job.DirectoryPathForSprites)) {
					Directory.CreateDirectory(job.DirectoryPathForSprites);
				}

				// Check and copy json file
				string jsonSource = job.AssetDirectory + "/" + job.Name + ".json";
				string jsonTarget = job.DirectoryPathForSprites + "/" + job.Name + ".json";
				if (File.Exists(jsonSource)) {
					if (File.Exists(jsonTarget)) {
						File.Delete(jsonTarget);
					}
					File.Move(jsonSource, jsonTarget);
				}
				else {
					Debug.LogWarning("Calling Aseprite resulted in no json data file. Wrong Aseprite version?");
					return false;
				}

				// Check and copy png file
				string pngSource = job.AssetDirectory + "/" + job.Name + ".png";
				string pngTarget = job.DirectoryPathForSprites + "/" + job.Name + ".png";
				if (File.Exists(pngSource)) {
					if (File.Exists(pngTarget)) {
						File.Delete(pngTarget);
					}
					File.Move(pngSource, pngTarget);
				}
				else {
					Debug.LogWarning("Calling Aseprite resulted in no png Image file. Wrong Aseprite version?");
					return false;
				}
			}

			return success;
		}

		private static ImportedAnimationSheet GetAnimationInfo(JsonObject root) {
			if (root == null) {
				Debug.LogWarning("Error importing JSON animation info: JSONObject is NULL");
				return null;
			}

			ImportedAnimationSheet animationSheet = new ImportedAnimationSheet();
			animationSheet.UsePivot = false;

			// Import Frame Size
			if (GetFrameSizeFromJson(animationSheet, root) == false) {
				return null;
			}

			// Import all informations from JSON
			if (!root.ContainsKey("meta")) {
				Debug.LogWarning("Error importing JSON animation info: no 'meta' object");
				return null;
			}
			var meta = root["meta"].Obj;
			// Import meta data
			GetMetaInfosFromJson(animationSheet, meta);

			// Import Animation meta data
			if (GetAnimationsFromJson(animationSheet, meta) == false) {
				return null;
			}

			// Import Frames
			if (GetFramesFromJson(animationSheet, root) == false) {
				return null;
			}

			animationSheet.ApplyGlobalFramesToAnimationFrames();

			return animationSheet;
		}

		private static int CallAsepriteCli(string asepritePath, string path, string buildOptions) {
			string workingDirectory = Application.dataPath.Replace("Assets", "") + path;

			System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo {
				Arguments = "-b " + buildOptions,
				FileName = asepritePath,
				WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				UseShellExecute = false,
				WorkingDirectory = workingDirectory
			};

			// Run the external process & wait for it to finish
			using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start(start)) {
				proc.WaitForExit();

				// Retrieve the app's exit code
				return proc.ExitCode;
			}
		}

		private static bool GetFrameSizeFromJson(ImportedAnimationSheet animationSheet, JsonObject root) {
			var list = root["frames"].Array;
			if (list == null) {
				Debug.LogWarning("No 'frames' array found in JSON created by Aseprite.");
				IssueVersionWarning();
				return false;
			}

			foreach (var item in list) {
				var sourceSizeValues = item.Obj["sourceSize"].Obj;
				animationSheet.SourceSize = new Vector2Int((int)sourceSizeValues["w"].Number, (int)sourceSizeValues["h"].Number);
				return true;
			}

			return false;
		}

		private static void GetMetaInfosFromJson(ImportedAnimationSheet animationSheet, JsonObject meta) {
			var size = meta["size"].Obj;
			animationSheet.Width = (int)size["w"].Number;
			animationSheet.Height = (int)size["h"].Number;

			var slices = meta["slices"].Array;
			foreach (var slice in slices) {
				JsonObject sliceTag = slice.Obj;
				ImportedAnimationSlice importedSlice = new ImportedAnimationSlice(animationSheet.SourceSize) {
					Name = sliceTag["name"].Str
				};

				var keys = sliceTag["keys"].Array;
				foreach (var key in keys) {
					JsonObject keyTag = key.Obj;

					var boundsTag = keyTag["bounds"].Obj;
					var pivotTag = keyTag["pivot"].Obj;
					importedSlice.Bounds = new BoundsInt((int)boundsTag["x"].Number, (int)boundsTag["y"].Number, 0, (int)boundsTag["w"].Number, (int)boundsTag["h"].Number, 0);
					importedSlice.Pivot = new Vector2Int((int)pivotTag["x"].Number, (int)pivotTag["y"].Number);

					animationSheet.UsePivot = true;
					animationSheet.Pivot = importedSlice.NormalizedPivot;
					break;
				}

				animationSheet.Slices.Add(importedSlice);
			}
		}

		private static bool GetAnimationsFromJson(ImportedAnimationSheet animationSheet, JsonObject meta) {
			if (!meta.ContainsKey("frameTags")) {
				Debug.LogWarning("No 'frameTags' found in JSON created by Aseprite.");
				IssueVersionWarning();
				return false;
			}

			var frameTags = meta["frameTags"].Array;
			foreach (var item in frameTags) {
				JsonObject frameTag = item.Obj;
				ImportedAnimation anim = new ImportedAnimation {
					Name = frameTag["name"].Str,
					FirstSpriteIndex = (int)(frameTag["from"].Number),
					LastSpriteIndex = (int)(frameTag["to"].Number)
				};

				// Detect whether this frame is a subframe
				for (int i = animationSheet.Animations.Count - 1; i >= 0; i--) {
					var animation = animationSheet.Animations[i];

					if (anim.IsInAnimation(animation)) {
						animation.IsCategory = true;
					}
				}

				animationSheet.Animations.Add(anim);
			}

			// Rebuild Names
			foreach (var anim in animationSheet.Animations) {
				for (int i = animationSheet.Animations.Count - 1; i >= 0; i--) {
					var animation = animationSheet.Animations[i];
					if (anim.IsCategory || anim.Name.Equals(animation.Name, StringComparison.OrdinalIgnoreCase)) { continue; }

					if (anim.IsInAnimation(animation)) {
						anim.Name = animation.Name + "_" + anim.Name;
					}
				}
			}

			return true;
		}

		private static bool GetFramesFromJson(ImportedAnimationSheet animationSheet, JsonObject root) {
			var list = root["frames"].Array;

			if (list == null) {
				Debug.LogWarning("No 'frames' array found in JSON created by Aseprite.");
				IssueVersionWarning();
				return false;
			}

			foreach (var item in list) {
				ImportedAnimationFrame frame = new ImportedAnimationFrame();

				var frameValues = item.Obj["frame"].Obj;
				frame.Width = (int)frameValues["w"].Number;
				frame.Height = (int)frameValues["h"].Number;
				frame.X = (int)frameValues["x"].Number;
				frame.Y = animationSheet.Height - (int)frameValues["y"].Number - frame.Height; // Unity has a different coord system

				var sourceSizeValues = item.Obj["sourceSize"].Obj;
				animationSheet.SourceSize = new Vector2Int((int)sourceSizeValues["w"].Number, (int)sourceSizeValues["h"].Number);

				frame.Duration = (int)item.Obj["duration"].Number;

				animationSheet.Frames.Add(frame);
			}

			return true;
		}

		private static void IssueVersionWarning() {
			Debug.LogWarning("Please use official Aseprite 1.1.1 or newer.");
		}
	}
}