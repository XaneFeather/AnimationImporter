using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimationImporter {
	public class AnimationImporterWindow : EditorWindow {
		// ================================================================================
		//  private
		// --------------------------------------------------------------------------------

		private AnimationImporter Importer {
			get {
				return AnimationImporter.Instance;
			}
		}

		private GUIStyle dropBoxStyle;
		private GUIStyle infoTextStyle;

		private string nonLoopingAnimationEnterValue = "";

		private Vector2 scrollPos = Vector2.zero;

		// ================================================================================
		//  menu entry
		// --------------------------------------------------------------------------------

		[MenuItem("Window/Animation Importer")]
		public static void ImportAnimationsMenu() {
			GetWindow(typeof(AnimationImporterWindow), false, "Anim Importer");
		}

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		public void OnEnable() {
			Importer.LoadOrCreateUserConfig();
		}

		public void OnGUI() {
			CheckGuiStyles();

			if (Importer.CanImportAnimations) {
				scrollPos = GUILayout.BeginScrollView(scrollPos);

				EditorGUILayout.Space();

				ShowAnimationsGui();

				GUILayout.Space(25f);

				ShowAnimatorControllerGui();

				GUILayout.Space(25f);

				ShowAnimatorOverrideControllerGui();

				GUILayout.Space(25f);

				ShowUserConfig();

				GUILayout.EndScrollView();
			}
			else {
				EditorGUILayout.Space();

				ShowHeadline("Select Aseprite Application");

				EditorGUILayout.Space();

				ShowAsepriteApplicationSelection();

				EditorGUILayout.Space();

				GUILayout.Label("Aseprite has to be installed on this machine because the Importer calls Aseprite through the command line for creating images and getting animation data.", infoTextStyle);
			}
		}

		// ================================================================================
		//  GUI methods
		// --------------------------------------------------------------------------------

		private void CheckGuiStyles() {
			if (dropBoxStyle == null) {
				GetBoxStyle();
			}
			if (infoTextStyle == null) {
				GetTextInfoStyle();
			}
		}

		private void GetBoxStyle() {
			dropBoxStyle = new GUIStyle(EditorStyles.helpBox) {
				alignment = TextAnchor.MiddleCenter
			};
		}

		private void GetTextInfoStyle() {
			infoTextStyle = new GUIStyle(EditorStyles.label) {
				wordWrap = true
			};
		}

		private void ShowUserConfig() {
			if (Importer == null || Importer.SharedData == null) {
				return;
			}

			ShowHeadline("Config");

			/*
				Aseprite Application
			*/

			ShowAsepriteApplicationSelection();

			GUILayout.Space(5f);

			/*
				Sprite values
			*/

			Importer.SharedData.TargetObjectType = (AnimationTargetObjectType)EditorGUILayout.EnumPopup("Target Object", Importer.SharedData.TargetObjectType);

			Importer.SharedData.SpriteAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Alignment", Importer.SharedData.SpriteAlignment);

			if (Importer.SharedData.SpriteAlignment == SpriteAlignment.Custom) {
				Importer.SharedData.SpriteAlignmentCustomX = EditorGUILayout.Slider("x", Importer.SharedData.SpriteAlignmentCustomX, 0, 1f);
				Importer.SharedData.SpriteAlignmentCustomY = EditorGUILayout.Slider("y", Importer.SharedData.SpriteAlignmentCustomY, 0, 1f);
			}

			Importer.SharedData.SpritePixelsPerUnit = EditorGUILayout.FloatField("Sprite Pixels per Unit", Importer.SharedData.SpritePixelsPerUnit);

			GUILayout.Space(5f);

			ShowTargetLocationOptions("Sprites", Importer.SharedData.SpritesTargetLocation);
			ShowTargetLocationOptions("Animations", Importer.SharedData.AnimationsTargetLocation);
			ShowTargetLocationOptions("AnimationController", Importer.SharedData.AnimationControllersTargetLocation);

			GUILayout.Space(5f);

			Importer.SharedData.SpriteNamingScheme = (SpriteNamingScheme)EditorGUILayout.IntPopup("Sprite Naming Scheme",
				(int)Importer.SharedData.SpriteNamingScheme,
				SpriteNaming.NamingSchemesDisplayValues, SpriteNaming.NamingSchemesValues);

			GUILayout.Space(25f);

			ShowHeadline("Automatic Import");
			EditorGUILayout.BeginHorizontal();
			Importer.SharedData.AutomaticImporting = EditorGUILayout.Toggle("Automatic Import", Importer.SharedData.AutomaticImporting);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("Looks for existing Animation Controller with same name.");

			/*
				Animations that do not loop
			*/

			GUILayout.Space(25f);
			ShowHeadline("Non-looping Animations");

			for (int i = 0; i < Importer.SharedData.AnimationNamesThatDoNotLoop.Count; i++) {
				GUILayout.BeginHorizontal();
				GUILayout.Label(Importer.SharedData.AnimationNamesThatDoNotLoop[i]);
				bool doDelete = GUILayout.Button("Delete");
				GUILayout.EndHorizontal();
				if (doDelete) {
					Importer.SharedData.RemoveAnimationThatDoesNotLoop(i);
					break;
				}
			}

			EditorGUILayout.Space();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Add ");
			nonLoopingAnimationEnterValue = EditorGUILayout.TextField(nonLoopingAnimationEnterValue);
			if (GUILayout.Button("Enter")) {
				if (Importer.SharedData.AddAnimationThatDoesNotLoop(nonLoopingAnimationEnterValue)) {
					nonLoopingAnimationEnterValue = "";
				}
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.LabelField("Enter Part of the Animation Name or a Regex Expression.");

			if (GUI.changed) {
				EditorUtility.SetDirty(Importer.SharedData);
			}
		}

		private void ShowTargetLocationOptions(string label, AssetTargetLocation targetLocation) {
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(label, GUILayout.Width(130f));

			targetLocation.LocationType = (AssetTargetLocationType)EditorGUILayout.EnumPopup(targetLocation.LocationType, GUILayout.Width(130f));

			bool prevEnabled = GUI.enabled;
			GUI.enabled = targetLocation.LocationType == AssetTargetLocationType.GlobalDirectory;

			string globalDirectory = targetLocation.GlobalDirectory;

			if (GUILayout.Button("Select", GUILayout.Width(50f))) {
				var startDirectory = globalDirectory;
				if (!Directory.Exists(startDirectory)) {
					startDirectory = Application.dataPath;
				}
				startDirectory = Application.dataPath;

				var path = EditorUtility.OpenFolderPanel("Select Target Location", globalDirectory, "");
				if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(AssetDatabaseUtility.GetAssetPath(path))) {
					targetLocation.GlobalDirectory = AssetDatabaseUtility.GetAssetPath(path);
				}
			}

			if (targetLocation.LocationType == AssetTargetLocationType.GlobalDirectory) {
				string displayDirectory = "/" + globalDirectory;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(displayDirectory, GUILayout.MaxWidth(300f));
			}

			GUI.enabled = prevEnabled;

			EditorGUILayout.EndHorizontal();
		}

		private void ShowAsepriteApplicationSelection() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Aseprite Application Path");

			string newPath = Importer.AsepritePath;

			if (GUILayout.Button("Select")) {
				var path = EditorUtility.OpenFilePanel(
					"Select Aseprite Application",
					"",
					"exe,app");
				if (!string.IsNullOrEmpty(path)) {
					newPath = path;

					if (Application.platform == RuntimePlatform.OSXEditor) {
						newPath += "/Contents/MacOS/aseprite";
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			Importer.AsepritePath = GUILayout.TextField(newPath, GUILayout.MaxWidth(300f));

			GUILayout.EndHorizontal();
		}

		private void ShowAnimationsGui() {
			ShowHeadline("Animations");

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(Importer.CanImportAnimations, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0) {
				Importer.ImportAssets(droppedAssets);
			}
		}

		private void ShowAnimatorControllerGui() {
			ShowHeadline("Animator Controller + Animations");

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(Importer.CanImportAnimations, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0) {
				Importer.ImportAssets(droppedAssets, ImportAnimatorController.AnimatorController);
			}
		}

		private void ShowAnimatorOverrideControllerGui() {
			ShowHeadline("Animator Override Controller + Animations");

			Importer.BaseController = EditorGUILayout.ObjectField("Based on Controller:", Importer.BaseController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(Importer.CanImportAnimationsForOverrideController, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0) {
				Importer.ImportAssets(droppedAssets, ImportAnimatorController.AnimatorOverrideController);
			}
		}

		private void ShowHeadline(string headline) {
			EditorGUILayout.LabelField(headline, EditorStyles.boldLabel, GUILayout.Height(20f));
		}

		// ================================================================================
		//  OnGUI helper
		// --------------------------------------------------------------------------------

		public delegate bool IsValidAssetDelegate(string path);

		private T[] ShowDropButton<T>(bool isEnabled, IsValidAssetDelegate isValidAsset) where T : UnityEngine.Object {
			T[] returnValue = null;

			Rect dropArea = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));

			GUI.enabled = isEnabled;
			GUI.Box(dropArea, "Drop Animation files here", dropBoxStyle);
			GUI.enabled = true;

			if (!isEnabled) {
				return null;
			}

			Event evt = Event.current;
			switch (evt.type) {
				case EventType.DragUpdated:
				case EventType.DragPerform:

					if (!dropArea.Contains(evt.mousePosition)
						|| !DraggedObjectsContainValidObject<T>(isValidAsset)) {
						return null;
					}

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform) {
						DragAndDrop.AcceptDrag();

						List<T> validObjects = new List<T>();

						foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences) {
							var assetPath = AssetDatabase.GetAssetPath(draggedObject);

							if (draggedObject is T && isValidAsset(assetPath)) {
								validObjects.Add(draggedObject as T);
							}
						}

						returnValue = validObjects.ToArray();
					}

					evt.Use();

					break;
			}

			return returnValue;
		}

		private bool DraggedObjectsContainValidObject<T>(IsValidAssetDelegate isValidAsset) where T : UnityEngine.Object {
			foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences) {
				var assetPath = AssetDatabase.GetAssetPath(draggedObject);

				if (draggedObject is T && isValidAsset(assetPath)) {
					return true;
				}
			}

			return false;
		}
	}
}
