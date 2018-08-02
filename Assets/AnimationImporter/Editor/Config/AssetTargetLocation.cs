using System.IO;
using UnityEngine;

namespace AnimationImporter
{
	[System.Serializable]
	public class AssetTargetLocation
	{
		[SerializeField]
		private AssetTargetLocationType _locationType;
		public AssetTargetLocationType LocationType
		{
			get { return _locationType; }
			set { _locationType = value; }
		}

		[SerializeField]
		private string _globalDirectory = "Assets";
		public string GlobalDirectory
		{
			get { return _globalDirectory; }
			set { _globalDirectory = value; }
		}
		
		private string _subDirectoryName;
		public string SubDirectoryName
		{
			get {return _subDirectoryName; }
		}

		// ================================================================================
		//  constructor
		// --------------------------------------------------------------------------------

		public AssetTargetLocation(AssetTargetLocationType type, string subFolderName) : this(type)
		{
			_subDirectoryName = subFolderName;
		}

		public AssetTargetLocation(AssetTargetLocationType type)
		{
			LocationType = type;
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		public string GetAndEnsureTargetDirectory(string assetDirectory)
		{
			string directory = GetTargetDirectory(assetDirectory);

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return directory;
		}

		public string GetTargetDirectory(string assetDirectory)
		{
			if (LocationType == AssetTargetLocationType.GlobalDirectory)
			{
				return GlobalDirectory;
			}
			else if (LocationType == AssetTargetLocationType.SubDirectory)
			{
				return Path.Combine(assetDirectory, SubDirectoryName);
			}

			return assetDirectory;
		}
	}
}