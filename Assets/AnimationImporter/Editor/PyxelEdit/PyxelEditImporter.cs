using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnimationImporter.Boomlagoon.JSON;

namespace AnimationImporter.PyxelEdit {
	[InitializeOnLoad]
	public class PyxelEditImporter : IAnimationImporterPlugin {
		private static PyxelEditData latestData = null;

		// ================================================================================
		//  static constructor, registering plugin
		// --------------------------------------------------------------------------------

		static PyxelEditImporter() {
			PyxelEditImporter importer = new PyxelEditImporter();
			AnimationImporter.RegisterImporter(importer, "pyxel");
		}

		public ImportedAnimationSheet Import(AnimationImportJob job, AnimationImporterSharedConfig config) {
			if (ImportImageAndMetaInfo(job)) {
				AssetDatabase.Refresh();
				return GetAnimationInfo(latestData);
			}

			return null;
		}

		public bool IsValid() {
			return IonicZipDllIsPresent();
		}

		private static bool ImportImageAndMetaInfo(AnimationImportJob job) {
			latestData = null;

			var zipFilePath = GetFileSystemPath(job.AssetDirectory + "/" + job.FileName);

			var files = GetContentsFromZipFile(zipFilePath);

			if (files.ContainsKey("docData.json")) {
				string jsonData = System.Text.Encoding.UTF8.GetString(files["docData.json"]);

				PyxelEditData pyxelEditData = ReadJson(jsonData);

				List<Layer> allLayers = new List<Layer>();

				foreach (var item in pyxelEditData.Canvas.Layers) {
					Layer layer = item.Value;
					string layerName = "layer" + item.Key.ToString() + ".png";
					layer.Texture = LoadTexture(files[layerName]);
					allLayers.Add(layer);
				}

				Texture2D image = CreateBlankTexture(new Color(0f, 0f, 0f, 0), pyxelEditData.Canvas.Width, pyxelEditData.Canvas.Height);
				for (int i = allLayers.Count - 1; i >= 0; i--) {
					Layer layer = allLayers[i];

					if (!layer.Hidden) {
						float maxAlpha = layer.Alpha / 255f;
						image = CombineTextures(image, layer.Texture, maxAlpha);
					}
				}

				if (!Directory.Exists(job.DirectoryPathForSprites)) {
					Directory.CreateDirectory(job.DirectoryPathForSprites);
				}

				SaveTextureToAssetPath(image, job.ImageAssetFilename);

				latestData = pyxelEditData;

				return true;
			}
			else {
				return false;
			}
		}

		private static ImportedAnimationSheet GetAnimationInfo(PyxelEditData data) {
			if (data == null) {
				return null;
			}

			int tileWidth = data.Tileset.TileWidth;
			int tileHeight = data.Tileset.TileHeight;

			int maxTileIndex = 0;

			ImportedAnimationSheet animationSheet = new ImportedAnimationSheet();
			animationSheet.Width = data.Canvas.Width;
			animationSheet.Height = data.Canvas.Height;

			// animations
			animationSheet.Animations = new List<ImportedAnimation>();
			for (int i = 0; i < data.Animations.Count; i++) {
				var animationData = data.Animations[i];

				ImportedAnimation importAnimation = new ImportedAnimation();

				importAnimation.Name = animationData.Name;

				importAnimation.FirstSpriteIndex = animationData.BaseTile;
				importAnimation.LastSpriteIndex = animationData.BaseTile + animationData.Length - 1;

				maxTileIndex = Mathf.Max(maxTileIndex, importAnimation.LastSpriteIndex);

				ImportedAnimationFrame[] frames = new ImportedAnimationFrame[animationData.Length];
				for (int frameIndex = 0; frameIndex < animationData.Length; frameIndex++) {
					ImportedAnimationFrame frame = new ImportedAnimationFrame();

					frame.Duration = animationData.FrameDuration;
					if (animationData.FrameDurationMultipliers[i] != 100) {
						frame.Duration *= (int)(animationData.FrameDurationMultipliers[i] / 100f);
					}

					int tileIndex = animationData.BaseTile + frameIndex;

					int columnCount = data.Canvas.Width / tileWidth;

					int column = tileIndex % columnCount;
					int row = tileIndex / columnCount;

					frame.Y = row * tileHeight;
					frame.X = column * tileWidth;
					frame.Width = tileWidth;
					frame.Height = tileHeight;

					frames[frameIndex] = frame;
				}

				importAnimation.SetFrames(frames);

				animationSheet.Animations.Add(importAnimation);
			}

			// gather all frames used by animations for the sprite sheet
			animationSheet.Frames = new List<ImportedAnimationFrame>();
			foreach (var anim in animationSheet.Animations) {
				if (anim.IsCategory) { continue; }
				foreach (var frame in anim.Frames) {
					animationSheet.Frames.Add(frame);
				}
			}

			return animationSheet;
		}

		private static PyxelEditData ReadJson(string jsonData) {
			PyxelEditData data = new PyxelEditData();

			JsonObject obj = JsonObject.Parse(jsonData);

			if (obj.ContainsKey("name")) {
				data.Name = obj["name"].Str;
			}
			if (obj.ContainsKey("tileset")) {
				data.Tileset.TileWidth = (int)obj["tileset"].Obj["tileWidth"].Number;
				data.Tileset.TileHeight = (int)obj["tileset"].Obj["tileHeight"].Number;
				data.Tileset.TilesWide = (int)obj["tileset"].Obj["tilesWide"].Number;
				data.Tileset.FixedWidth = obj["tileset"].Obj["fixedWidth"].Boolean;
				data.Tileset.NumTiles = (int)obj["tileset"].Obj["numTiles"].Number;
			}
			if (obj.ContainsKey("animations")) {
				foreach (var item in obj["animations"].Obj) {
					data.Animations[int.Parse(item.Key)] = new Animation(item.Value.Obj);
				}
			}
			if (obj.ContainsKey("canvas")) {
				data.Canvas.Width = (int)obj["canvas"].Obj["width"].Number;
				data.Canvas.Height = (int)obj["canvas"].Obj["height"].Number;
				data.Canvas.TileWidth = (int)obj["canvas"].Obj["tileWidth"].Number;
				data.Canvas.TileHeight = (int)obj["canvas"].Obj["tileHeight"].Number;
				data.Canvas.NumLayers = (int)obj["canvas"].Obj["numLayers"].Number;
				foreach (var item in obj["canvas"].Obj["layers"].Obj) {
					data.Canvas.Layers[int.Parse(item.Key)] = new Layer(item.Value.Obj);
				}
			}

			return data;
		}

		public static string GetFileSystemPath(string path) {
			string basePath = Application.dataPath;

			// if the path already begins with the Assets folder, remove that one from the base
			if (path.StartsWith("Assets") || path.StartsWith("/Assets")) {
				basePath = basePath.Replace("/Assets", "");
			}

			return Path.Combine(basePath, path);
		}

		public static void SaveTextureToAssetPath(Texture2D texture, string assetPath) {
			string path = Application.dataPath + "/../" + assetPath;
			File.WriteAllBytes(path, texture.EncodeToPNG());
		}

		public static Texture2D CreateBlankTexture(
			Color color, int width = 2, int height = -1, TextureFormat format = TextureFormat.RGBA32,
			bool mipmap = false, bool linear = false) {
			if (height < 0) {
				height = width;
			}

			// create empty texture
			Texture2D texture = new Texture2D(width, height, format, mipmap, linear);

			// get all pixels as an array
			var cols = texture.GetPixels();
			for (int i = 0; i < cols.Length; i++) {
				cols[i] = color;
			}

			// important steps to save changed pixel values
			texture.SetPixels(cols);
			texture.Apply();

			texture.hideFlags = HideFlags.HideAndDontSave;

			return texture;
		}

		static Texture2D LoadTexture(byte[] imageData) {
			var w = ReadInt32FromImageData(imageData, 3 + 15);
			var h = ReadInt32FromImageData(imageData, 3 + 15 + 2 + 2);
			var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
			texture.hideFlags = HideFlags.HideAndDontSave;
			texture.filterMode = FilterMode.Point;
			texture.LoadImage(imageData);
			return texture;
		}

		static int ReadInt32FromImageData(byte[] imageData, int offset) {
			return (imageData[offset] << 8) | imageData[offset + 1];
		}

		public static Texture2D CombineTextures(Texture2D aBaseTexture, Texture2D aToCopyTexture, float maxAlpha) {
			int aWidth = aBaseTexture.width;
			int aHeight = aBaseTexture.height;
			Texture2D aReturnTexture = new Texture2D(aWidth, aHeight, TextureFormat.RGBA32, false);

			Color[] aBaseTexturePixels = aBaseTexture.GetPixels();
			Color[] aCopyTexturePixels = aToCopyTexture.GetPixels();
			Color[] aColorList = new Color[aBaseTexturePixels.Length];
			int aPixelLength = aBaseTexturePixels.Length;

			for (int p = 0; p < aPixelLength; p++) {
				float minA = aBaseTexturePixels[p].a;
				float alpha = aCopyTexturePixels[p].a * maxAlpha;
				aColorList[p] = Color.Lerp(aBaseTexturePixels[p], aCopyTexturePixels[p], alpha);
				aColorList[p].a = Mathf.Lerp(minA, 1f, alpha);
			}

			aReturnTexture.SetPixels(aColorList);
			aReturnTexture.Apply(false);

			return aReturnTexture;
		}

		// ================================================================================
		//  extracting from zip file
		// --------------------------------------------------------------------------------

		private static Type zipFileClass = null;
		private static System.Reflection.MethodInfo readZipFileMethod = null;
		private static System.Reflection.MethodInfo extractMethod = null;

		public static Dictionary<string, byte[]> GetContentsFromZipFile(string fileName) {
			Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

			if (zipFileClass == null) {
				InitZipMethods();
			}

			if (zipFileClass != null) {
				using (var zipFile = readZipFileMethod.Invoke(null, new object[] { fileName }) as IDisposable) {
					var zipFileAsEnumeration = zipFile as IEnumerable;
					foreach (var entry in zipFileAsEnumeration) {
						MemoryStream stream = new MemoryStream();
						extractMethod.Invoke(entry, new object[] { stream });

						files.Add(entry.ToString().Replace("ZipEntry::", ""), stream.ToArray());
					}
				}
			}

			return files;
		}

		private static void InitZipMethods() {
			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in allAssemblies) {
				zipFileClass = assembly.GetType("Ionic.Zip.ZipFile");

				if (zipFileClass != null) {
					readZipFileMethod = zipFileClass.GetMethod("Read", new Type[] { typeof(string) });

					Type zipEntryClass = assembly.GetType("Ionic.Zip.ZipEntry");

					extractMethod = zipEntryClass.GetMethod("Extract", new Type[] { typeof(MemoryStream) });

					return;
				}
			}
		}

		private static bool IonicZipDllIsPresent() {
			if (zipFileClass != null) {
				return true;
			}

			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in allAssemblies) {
				var zipClass = assembly.GetType("Ionic.Zip.ZipFile");

				if (zipClass != null) {
					return true;
				}
			}

			return false;
		}
	}
}