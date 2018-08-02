using System;
using System.Collections;
using UnityEngine;

namespace AnimationImporter {
	[Serializable]
	public class ImportedAnimationSlice {
		#region Fields & Properties
		// ----------------------------------------------------------------------------------------------------
		public string Name {
			get { return this.name; }
			set { this.name = value; }
		}
		[SerializeField]
		private string name;

		public BoundsInt Bounds {
			get { return this.bounds; }
			set {
				this.bounds = value;
				UpdateNormalizedPivot();
			}
		}
		[SerializeField]
		private BoundsInt bounds;

		public Vector2Int Pivot {
			get { return this.pivot; }
			set {
				this.pivot = value;
				UpdateNormalizedPivot();
			}
		}
		[SerializeField]
		private Vector2Int pivot;

		public Vector2 NormalizedPivot {
			get { return this.normalizedPivot; }
		}
		[SerializeField]
		private Vector2 normalizedPivot;

		public Vector2Int SourceSize {
			get { return this.sourceSize; }
		}
		[SerializeField]
		private Vector2Int sourceSize;
		// ----------------------------------------------------------------------------------------------------
		#endregion

		#region Initialization
		// ----------------------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ImportedAnimationSlice"/> class.
		/// </summary>
		public ImportedAnimationSlice(Vector2Int sourceSize) {
			this.sourceSize = sourceSize;
		}
		// ----------------------------------------------------------------------------------------------------
		#endregion

		#region Pivot Methods
		// ----------------------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the normalized pivot.
		/// </summary>
		private void UpdateNormalizedPivot() {
			Vector2 maxSize = new Vector2(this.sourceSize.x, this.sourceSize.y);
			Vector2 pivotPosition = new Vector2(this.bounds.x + (this.bounds.size.x - this.pivot.x), this.bounds.y + (this.bounds.size.y - this.pivot.y));
			this.normalizedPivot = new Vector2(pivotPosition.x / maxSize.x, 1- (pivotPosition.y / maxSize.y));
		}
		// ----------------------------------------------------------------------------------------------------
		#endregion
	}
}
