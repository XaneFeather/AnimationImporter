using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;
using System.Linq;
using AnimationImporter.Aseprite;

namespace AnimationImporter {
	public class AnimationImporter {
		// ================================================================================
		//	Singleton
		// --------------------------------------------------------------------------------

		private static AnimationImporter instance = null;
		public static AnimationImporter Instance {
			get {
				if (instance == null) {
					instance = new AnimationImporter();
				}

				return instance;
			}
		}

		// ================================================================================
		//  Delegates
		// --------------------------------------------------------------------------------

		public delegate ImportedAnimationSheet ImportDelegate(AnimationImportJob job, AnimationImporterSharedConfig config);

		public delegate bool CustomReImportDelegate(string fileName);
		public static CustomReImportDelegate HasCustomReImport = null;
		public static CustomReImportDelegate HandleCustomReImport = null;

		public delegate void ChangeImportJob(AnimationImportJob job);

		// ================================================================================
		//  Const
		// --------------------------------------------------------------------------------

		private const string PrefsPrefix = "ANIMATION_IMPORTER_";
		private const string SharedConfigPath = "Assets/Resources/AnimationImporter/AnimationImporterConfig.asset";

		// ================================================================================
		//  User values
		// --------------------------------------------------------------------------------

		string asepritePath = "";
		public string AsepritePath {
			get {
				return asepritePath;
			}
			set {
				if (asepritePath != value) {
					asepritePath = value;
					SaveUserConfig();
				}
			}
		}

		private RuntimeAnimatorController baseController = null;
		public RuntimeAnimatorController BaseController {
			get {
				return baseController;
			}
			set {
				if (baseController != value) {
					baseController = value;
					SaveUserConfig();
				}
			}
		}

		private AnimationImporterSharedConfig sharedData;
		public AnimationImporterSharedConfig SharedData {
			get {
				return sharedData;
			}
		}

		// ================================================================================
		//  Importer Plugins
		// --------------------------------------------------------------------------------

		private static Dictionary<string, IAnimationImporterPlugin> importerPlugins = new Dictionary<string, IAnimationImporterPlugin>();

		public static void RegisterImporter(IAnimationImporterPlugin importer, params string[] extensions) {
			foreach (var extension in extensions) {
				importerPlugins[extension] = importer;
			}
		}

		// ================================================================================
		//  Validation
		// --------------------------------------------------------------------------------

		// This was used in the past, might be again in the future, so leave it here
		public bool CanImportAnimations {
			get {
				return true;
			}
		}
		public bool CanImportAnimationsForOverrideController {
			get {
				return CanImportAnimations && baseController != null;
			}
		}

		// ================================================================================
		//  Save and load user values
		// --------------------------------------------------------------------------------

		public void LoadOrCreateUserConfig() {
			LoadPreferences();

			sharedData = ScriptableObjectUtility.LoadOrCreateSaveData<AnimationImporterSharedConfig>(SharedConfigPath);
		}

		public void LoadUserConfig() {
			LoadPreferences();

			sharedData = ScriptableObjectUtility.LoadSaveData<AnimationImporterSharedConfig>(SharedConfigPath);
		}

		private void LoadPreferences() {
			if (PlayerPrefs.HasKey(PrefsPrefix + "asepritePath")) {
				asepritePath = PlayerPrefs.GetString(PrefsPrefix + "asepritePath");
			}
			else {
				asepritePath = AsepriteImporter.StandardApplicationPath;

				if (!File.Exists(asepritePath)) {
					asepritePath = "";
				}
			}

			if (PlayerPrefs.HasKey(PrefsPrefix + "baseControllerPath")) {
				string baseControllerPath = PlayerPrefs.GetString(PrefsPrefix + "baseControllerPath");
				if (!string.IsNullOrEmpty(baseControllerPath)) {
					baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
				}
			}
		}

		private void SaveUserConfig() {
			PlayerPrefs.SetString(PrefsPrefix + "asepritePath", asepritePath);

			PlayerPrefs.SetString(PrefsPrefix + "baseControllerPath", baseController != null ? AssetDatabase.GetAssetPath(baseController) : string.Empty);
		}

		// ================================================================================
		//  Import methods
		// --------------------------------------------------------------------------------

		public void ImportAssets(DefaultAsset[] assets, ImportAnimatorController importAnimatorController = ImportAnimatorController.None) {
			List<AnimationImportJob> jobs = new List<AnimationImportJob>();

			foreach (var asset in assets) {
				string assetPath = AssetDatabase.GetAssetPath(asset);
				if (!IsValidAsset(assetPath)) {
					continue;
				}

				AnimationImportJob job = CreateAnimationImportJob(assetPath);
				job.ImportAnimatorController = importAnimatorController;
				jobs.Add(job);
			}

			Import(jobs.ToArray());
		}

		/// <summary>
		/// Can be used by custom import pipeline
		/// </summary>
		public ImportedAnimationSheet ImportSpritesAndAnimationSheet(string assetPath, ChangeImportJob changeImportJob = null, string additionalCommandLineArguments = null) {
			// Making sure config is valid
			if (SharedData == null) {
				LoadOrCreateUserConfig();
			}

			if (!IsValidAsset(assetPath)) {
				return null;
			}

			// Create a job
			var job = CreateAnimationImportJob(assetPath, additionalCommandLineArguments);
			job.CreateUnityAnimations = false;

			if (changeImportJob != null) {
				changeImportJob(job);
			}

			return ImportJob(job);
		}

		private void Import(AnimationImportJob[] jobs) {
			if (jobs == null || jobs.Length == 0) {
				return;
			}

			float progressPerJob = 1f / jobs.Length;

			try {
				for (int i = 0; i < jobs.Length; i++) {
					AnimationImportJob job = jobs[i];

					job.ProgressUpdated += (float progress) => {
						float completeProgress = i * progressPerJob + progress * progressPerJob;
						EditorUtility.DisplayProgressBar("Import", job.Name, completeProgress);
					};
					ImportJob(job);
				}
				AssetDatabase.Refresh();
			}
			catch (Exception error) {
				Debug.LogWarning(error.ToString());
				throw;
			}

			EditorUtility.ClearProgressBar();
		}

		private ImportedAnimationSheet ImportJob(AnimationImportJob job) {
			job.SetProgress(0);

			IAnimationImporterPlugin importer = importerPlugins[GetExtension(job.FileName)];
			ImportedAnimationSheet animationSheet = importer.Import(job, SharedData);

			job.SetProgress(0.3f);

			if (animationSheet != null) {
				animationSheet.AssetDirectory = job.AssetDirectory;
				animationSheet.Name = job.Name;

				animationSheet.ApplySpriteNamingScheme(SharedData.SpriteNamingScheme);

				CreateSprites(animationSheet, job);

				job.SetProgress(0.6f);

				if (job.CreateUnityAnimations) {
					CreateAnimations(animationSheet, job);

					job.SetProgress(0.8f);

					if (job.ImportAnimatorController == ImportAnimatorController.AnimatorController) {
						CreateAnimatorController(animationSheet);
					}
					else if (job.ImportAnimatorController == ImportAnimatorController.AnimatorOverrideController) {
						CreateAnimatorOverrideController(animationSheet, job.UseExistingAnimatorController);
					}
				}
			}

			return animationSheet;
		}

		// ================================================================================
		//  create animator controllers
		// --------------------------------------------------------------------------------

		private void CreateAnimatorController(ImportedAnimationSheet animations) {
			string directory = SharedData.AnimationControllersTargetLocation.GetAndEnsureTargetDirectory(animations.AssetDirectory);

			// check if controller already exists; use this to not loose any references to this in other assets
			string pathForAnimatorController = directory + "/" + animations.Name + ".controller";
			var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForAnimatorController);

			if (controller == null) {
				// create a new controller and place every animation as a state on the first layer
				controller = AnimatorController.CreateAnimatorControllerAtPath(pathForAnimatorController);
				controller.AddLayer("Default");

				foreach (var animation in animations.Animations) {
					if (animation.IsCategory) { continue; }
					AnimatorState state = controller.layers[0].stateMachine.AddState(animation.Name);
					state.motion = animation.AnimationClip;
				}
			}
			else {
				// look at all states on the first layer and replace clip if state has the same name
				var childStates = controller.layers[0].stateMachine.states;
				foreach (var childState in childStates) {
					AnimationClip clip = animations.GetClip(childState.state.name);
					if (clip != null) {
						childState.state.motion = clip;
					}
				}
			}

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
		}

		private void CreateAnimatorOverrideController(ImportedAnimationSheet animations, bool useExistingBaseController = false) {
			string directory = SharedData.AnimationControllersTargetLocation.GetAndEnsureTargetDirectory(animations.AssetDirectory);

			// check if override controller already exists; use this to not loose any references to this in other assets
			string pathForOverrideController = directory + "/" + animations.Name + ".overrideController";
			var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForOverrideController);

			RuntimeAnimatorController baseController = this.baseController;
			if (useExistingBaseController && overrideController.runtimeAnimatorController != null) {
				baseController = overrideController.runtimeAnimatorController;
			}

			if (baseController != null) {
				if (overrideController == null) {
					overrideController = new AnimatorOverrideController();
					AssetDatabase.CreateAsset(overrideController, pathForOverrideController);
				}

				overrideController.runtimeAnimatorController = baseController;

				// Set override clips
#if UNITY_5_6_OR_NEWER
				var clipPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
				overrideController.GetOverrides(clipPairs);

				foreach (var pair in clipPairs) {
					string animationName = pair.Key.name;
					AnimationClip clip = animations.GetClipOrSimilar(animationName);
					overrideController[animationName] = clip;
				}
#else
				var clipPairs = overrideController.clips;
				for (int i = 0; i < clipPairs.Length; i++)
				{
					string animationName = clipPairs[i].originalClip.name;
					AnimationClip clip = animations.GetClipOrSimilar(animationName);
					clipPairs[i].overrideClip = clip;
				}
				overrideController.clips = clipPairs;
#endif

				EditorUtility.SetDirty(overrideController);
			}
			else {
				Debug.LogWarning("No Animator Controller found as a base for the Override Controller");
			}
		}

		// ================================================================================
		//  Create sprites and animations
		// --------------------------------------------------------------------------------

		private void CreateAnimations(ImportedAnimationSheet animationSheet, AnimationImportJob job) {
			if (animationSheet == null) {
				return;
			}

			string imageAssetFilename = job.ImageAssetFilename;

			if (animationSheet.HasAnimations) {
				string targetPath = sharedData.AnimationsTargetLocation.GetAndEnsureTargetDirectory(animationSheet.AssetDirectory);
				CreateAnimationAssets(animationSheet, imageAssetFilename, targetPath);
			}
		}

		private void CreateAnimationAssets(ImportedAnimationSheet animationInfo, string imageAssetFilename, string pathForAnimations) {
			string masterName = Path.GetFileNameWithoutExtension(imageAssetFilename);

			foreach (var animation in animationInfo.Animations) {
				if (animation.IsCategory) { continue; }
				animationInfo.CreateAnimation(animation, pathForAnimations, masterName, SharedData.TargetObjectType);
			}
		}

		private void CreateSprites(ImportedAnimationSheet animationSheet, AnimationImportJob job) {
			if (animationSheet == null) {
				return;
			}

			string imageAssetFile = job.ImageAssetFilename;

			TextureImporter importer = AssetImporter.GetAtPath(imageAssetFile) as TextureImporter;

			// Apply texture import settings if there are no previous ones
			if (!animationSheet.HasPreviousTextureImportSettings) {
				importer.textureType = TextureImporterType.Sprite;
				importer.spritePixelsPerUnit = SharedData.SpritePixelsPerUnit;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Point;
#if UNITY_5_5_OR_NEWER
				importer.textureCompression = TextureImporterCompression.Uncompressed;
#else
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
#endif
			}

			// Create sub sprites for this file according to the AsepriteAnimationInfo
			if (animationSheet.UsePivot) {
				importer.spritesheet = animationSheet.GetSpriteSheet(SpriteAlignment.Custom, animationSheet.Pivot);
			}
			else {
				importer.spritesheet = animationSheet.GetSpriteSheet(SharedData.SpriteAlignment, new Vector2(SharedData.SpriteAlignmentCustomX, SharedData.SpriteAlignmentCustomY));
			}

			// Reapply old import settings (pivot settings for sprites)
			if (animationSheet.HasPreviousTextureImportSettings) {
				animationSheet.PreviousImportSettings.ApplyPreviousTextureImportSettings(importer);
			}

			// These values will be set in any case, not influenced by previous import settings
			importer.spriteImportMode = SpriteImportMode.Multiple;
			importer.maxTextureSize = animationSheet.MaxTextureSize;

			EditorUtility.SetDirty(importer);

			try {
				importer.SaveAndReimport();
			}
			catch (Exception e) {
				Debug.LogWarning("There was a problem with applying settings to the generated sprite file: " + e.ToString());
			}

			AssetDatabase.ImportAsset(imageAssetFile, ImportAssetOptions.ForceUpdate);

			Sprite[] createdSprites = GetAllSpritesFromAssetFile(imageAssetFile);
			animationSheet.ApplyCreatedSprites(createdSprites);
		}

		private static Sprite[] GetAllSpritesFromAssetFile(string imageFilename) {
			var assets = AssetDatabase.LoadAllAssetsAtPath(imageFilename);

			// Make sure we only grab valid sprites here
			List<Sprite> sprites = new List<Sprite>();
			foreach (var item in assets) {
				if (item is Sprite) {
					sprites.Add(item as Sprite);
				}
			}

			return sprites.ToArray();
		}

		// ================================================================================
		//  Querying existing assets
		// --------------------------------------------------------------------------------

		// Check if this is a valid file; we are only looking at the file extension here
		public static bool IsValidAsset(string path) {
			string extension = GetExtension(path);

			if (!string.IsNullOrEmpty(path)) {
				if (importerPlugins.ContainsKey(extension)) {
					IAnimationImporterPlugin importer = importerPlugins[extension];
					if (importer != null) {
						return importer.IsValid();
					}
				}
			}

			return false;
		}

		private static string GetExtension(string path) {
			if (string.IsNullOrEmpty(path)) {
				return null;
			}

			string extension = Path.GetExtension(path);
			if (extension.StartsWith(".")) {
				extension = extension.Remove(0, 1);
			}

			return extension;
		}

		public bool HasExistingRuntimeAnimatorController(string assetPath) {
			return HasExistingAnimatorController(assetPath) || HasExistingAnimatorOverrideController(assetPath);
		}

		public bool HasExistingAnimatorController(string assetPath) {
			return GetExistingAnimatorController(assetPath) != null;
		}

		public bool HasExistingAnimatorOverrideController(string assetPath) {
			return GetExistingAnimatorOverrideController(assetPath) != null;
		}

		public RuntimeAnimatorController GetExistingRuntimeAnimatorController(string assetPath) {
			AnimatorController animatorController = GetExistingAnimatorController(assetPath);
			if (animatorController != null) {
				return animatorController;
			}

			return GetExistingAnimatorOverrideController(assetPath);
		}

		public AnimatorController GetExistingAnimatorController(string assetPath) {
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);
			string targetDirectory = SharedData.AnimationControllersTargetLocation.GetTargetDirectory(basePath);

			string pathForController = targetDirectory + "/" + name + ".controller";
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForController);

			return controller;
		}

		public AnimatorOverrideController GetExistingAnimatorOverrideController(string assetPath) {
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);
			string targetDirectory = SharedData.AnimationControllersTargetLocation.GetTargetDirectory(basePath);

			string pathForController = targetDirectory + "/" + name + ".overrideController";
			AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForController);

			return controller;
		}

		// ================================================================================
		//  Automatic ReImport
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Will be called by the AssetPostProcessor
		/// </summary>
		public void AutomaticReImport(string[] assetPaths) {
			if (SharedData == null) {
				LoadOrCreateUserConfig();
			}

			List<AnimationImportJob> jobs = new List<AnimationImportJob>();

			foreach (var assetPath in assetPaths) {
				if (string.IsNullOrEmpty(assetPath)) {
					continue;
				}

				if (HandleCustomReImport != null && HandleCustomReImport(assetPath)) {
					continue;
				}

				AnimationImportJob job = CreateAnimationImportJob(assetPath);
				if (job != null) {
					if (HasExistingAnimatorController(assetPath)) {
						job.ImportAnimatorController = ImportAnimatorController.AnimatorController;
					}
					else if (HasExistingAnimatorOverrideController(assetPath)) {
						job.ImportAnimatorController = ImportAnimatorController.AnimatorOverrideController;
						job.UseExistingAnimatorController = true;
					}

					jobs.Add(job);
				}
			}

			Import(jobs.ToArray());
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private AnimationImportJob CreateAnimationImportJob(string assetPath, string additionalCommandLineArguments = "") {
			AnimationImportJob importJob = new AnimationImportJob(assetPath) {
				AdditionalCommandLineArguments = additionalCommandLineArguments
			};


			importJob.DirectoryPathForSprites = sharedData.SpritesTargetLocation.GetTargetDirectory(importJob.AssetDirectory);
			importJob.DirectoryPathForAnimations = sharedData.AnimationsTargetLocation.GetTargetDirectory(importJob.AssetDirectory);
			importJob.DirectoryPathForAnimationControllers = sharedData.AnimationControllersTargetLocation.GetTargetDirectory(importJob.AssetDirectory);

			// We analyze import settings on existing files
			importJob.PreviousImportSettings = CollectPreviousImportSettings(importJob);

			return importJob;
		}

		private PreviousImportSettings CollectPreviousImportSettings(AnimationImportJob importJob) {
			PreviousImportSettings previousImportSettings = new PreviousImportSettings();

			previousImportSettings.GetTextureImportSettings(importJob.ImageAssetFilename);

			return previousImportSettings;
		}

		private string GetBasePath(string path) {
			string extension = Path.GetExtension(path);
			if (!string.IsNullOrEmpty(extension) && extension[0] == '.') {
				extension = extension.Remove(0, 1);
			}

			string fileName = Path.GetFileNameWithoutExtension(path);
			string lastPart = "/" + fileName + "." + extension;

			return path.Replace(lastPart, "");
		}
	}
}
