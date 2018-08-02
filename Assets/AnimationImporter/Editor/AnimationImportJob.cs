using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace AnimationImporter
{
	public class AnimationImportJob
	{
		private string assetPath;

		public string Name { get { return Path.GetFileNameWithoutExtension(FileName); } }
		public string FileName { get { return Path.GetFileName(assetPath); } }
		public string AssetDirectory { get { return GetBasePath(assetPath); } }

		private string _directoryPathForSprites = "";
		public string DirectoryPathForSprites
		{
			get
			{
				if (!Directory.Exists(_directoryPathForSprites))
				{
					Directory.CreateDirectory(_directoryPathForSprites);
				}

				return _directoryPathForSprites;
			}
			set
			{
				_directoryPathForSprites = value;
			}
		}

		private string _directoryPathForAnimations = "";
		public string DirectoryPathForAnimations
		{
			get
			{
				if (!Directory.Exists(_directoryPathForAnimations))
				{
					Directory.CreateDirectory(_directoryPathForAnimations);
				}

				return _directoryPathForAnimations;
			}
			set
			{
				_directoryPathForAnimations = value;
			}
		}

		private string _directoryPathForAnimationControllers = "";
		public string DirectoryPathForAnimationControllers
		{
			get
			{
				if (!Directory.Exists(_directoryPathForAnimationControllers))
				{
					Directory.CreateDirectory(_directoryPathForAnimationControllers);
				}

				return _directoryPathForAnimationControllers;
			}
			set
			{
				_directoryPathForAnimationControllers = value;
			}
		}

		public string ImageAssetFilename
		{
			get
			{
				return DirectoryPathForSprites + "/" + Name + ".png";
			}
		}

		public PreviousImportSettings PreviousImportSettings = null;

		// additional import settings
		public string AdditionalCommandLineArguments = null;
		public bool CreateUnityAnimations = true;
		public ImportAnimatorController ImportAnimatorController = ImportAnimatorController.None;
		public bool UseExistingAnimatorController = false;

		// ================================================================================
		//  constructor
		// --------------------------------------------------------------------------------

		public AnimationImportJob(string assetPath)
		{
			this.assetPath = assetPath;
		}

		// ================================================================================
		//  progress
		// --------------------------------------------------------------------------------

		public delegate void ProgressUpdatedDelegate(float progress);
		public event ProgressUpdatedDelegate ProgressUpdated;

		private float _progress = 0;
		public float Progress
		{
			get
			{
				return _progress;
			}
		}

		public void SetProgress(float progress)
		{
			_progress = progress;

			if (ProgressUpdated != null)
			{
				ProgressUpdated(_progress);
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private string GetBasePath(string path)
		{
			string extension = Path.GetExtension(path);
			if (extension.Length > 0 && extension[0] == '.')
			{
				extension = extension.Remove(0, 1);
			}

			string fileName = Path.GetFileNameWithoutExtension(path);
			string lastPart = "/" + fileName + "." + extension;

			return path.Replace(lastPart, "");
		}
	}
}