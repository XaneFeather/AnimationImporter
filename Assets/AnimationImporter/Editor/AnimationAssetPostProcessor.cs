using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;

namespace AnimationImporter {
	public class AnimationAssetPostprocessor : AssetPostprocessor {
		private static List<string> assetsMarkedForImport = new List<string>();
		private static EditorApplication.CallbackFunction importDelegate;

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath) {
			AnimationImporter importer = AnimationImporter.Instance;

			if (importer == null) {
				return;
			}

			// Do not create shared config during AssetPostprocess, or else it will recreate an empty config
			importer.LoadUserConfig();

			// If no config exists, they can't have set up automatic importing so just return out.
			if (importer.SharedData == null) {
				return;
			}

			if (importer.SharedData.AutomaticImporting) {
				List<string> markedAssets = new List<string>();

				foreach (string asset in importedAssets) {
					if (AnimationImporter.IsValidAsset(asset)) {
						MarkAssetForImport(asset, markedAssets);
					}
				}

				if (markedAssets.Count > 0) {
					assetsMarkedForImport.Clear();
					assetsMarkedForImport.AddRange(markedAssets);

					if (importDelegate == null) {
						importDelegate = new EditorApplication.CallbackFunction(ImportAssets);
					}

					// Subscribe to callback
					EditorApplication.update = Delegate.Combine(EditorApplication.update, importDelegate) as EditorApplication.CallbackFunction;
				}
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private static void MarkAssetForImport(string asset, List<string> markedAssets) {
			AnimationImporter importer = AnimationImporter.Instance;

			if (!importer.CanImportAnimations) {
				return;
			}

			if ((AnimationImporter.HasCustomReImport != null && AnimationImporter.HasCustomReImport(asset))
				|| importer.HasExistingAnimatorController(asset)
				|| importer.HasExistingAnimatorOverrideController(asset)) {
				markedAssets.Add(asset);
			}
		}

		private static void ImportAssets() {
			// Unsubscribe from callback
			EditorApplication.update = Delegate.Remove(EditorApplication.update, importDelegate as EditorApplication.CallbackFunction) as EditorApplication.CallbackFunction;

			AssetDatabase.Refresh();
			AnimationImporter importer = AnimationImporter.Instance;

			importer.AutomaticReImport(assetsMarkedForImport.ToArray());

			assetsMarkedForImport.Clear();
		}
	}
}