using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationImporter
{
	public static class AnimationClipUtility
	{
		class AnimationClipSettings
		{
			SerializedProperty mProperty;

			private SerializedProperty Get(string property) { return mProperty.FindPropertyRelative(property); }

			public AnimationClipSettings(SerializedProperty prop) { mProperty = prop; }

			public float StartTime { get { return Get("m_StartTime").floatValue; } set { Get("m_StartTime").floatValue = value; } }
			public float StopTime { get { return Get("m_StopTime").floatValue; } set { Get("m_StopTime").floatValue = value; } }
			public float OrientationOffsetY { get { return Get("m_OrientationOffsetY").floatValue; } set { Get("m_OrientationOffsetY").floatValue = value; } }
			public float Level { get { return Get("m_Level").floatValue; } set { Get("m_Level").floatValue = value; } }
			public float CycleOffset { get { return Get("m_CycleOffset").floatValue; } set { Get("m_CycleOffset").floatValue = value; } }

			public bool LoopTime { get { return Get("m_LoopTime").boolValue; } set { Get("m_LoopTime").boolValue = value; } }
			public bool LoopBlend { get { return Get("m_LoopBlend").boolValue; } set { Get("m_LoopBlend").boolValue = value; } }
			public bool LoopBlendOrientation { get { return Get("m_LoopBlendOrientation").boolValue; } set { Get("m_LoopBlendOrientation").boolValue = value; } }
			public bool LoopBlendPositionY { get { return Get("m_LoopBlendPositionY").boolValue; } set { Get("m_LoopBlendPositionY").boolValue = value; } }
			public bool LoopBlendPositionXz { get { return Get("m_LoopBlendPositionXZ").boolValue; } set { Get("m_LoopBlendPositionXZ").boolValue = value; } }
			public bool KeepOriginalOrientation { get { return Get("m_KeepOriginalOrientation").boolValue; } set { Get("m_KeepOriginalOrientation").boolValue = value; } }
			public bool KeepOriginalPositionY { get { return Get("m_KeepOriginalPositionY").boolValue; } set { Get("m_KeepOriginalPositionY").boolValue = value; } }
			public bool KeepOriginalPositionXz { get { return Get("m_KeepOriginalPositionXZ").boolValue; } set { Get("m_KeepOriginalPositionXZ").boolValue = value; } }
			public bool HeightFromFeet { get { return Get("m_HeightFromFeet").boolValue; } set { Get("m_HeightFromFeet").boolValue = value; } }
			public bool Mirror { get { return Get("m_Mirror").boolValue; } set { Get("m_Mirror").boolValue = value; } }
		}

		public static void SetLoop(this AnimationClip clip, bool value)
		{
			SerializedObject serializedClip = new SerializedObject(clip);
			AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));

			clipSettings.LoopTime = value;
			clipSettings.LoopBlend = false;

			serializedClip.ApplyModifiedProperties();
		}

		// ================================================================================
		//  curve bindings
		// --------------------------------------------------------------------------------

		public static EditorCurveBinding SpriteRendererCurveBinding
		{
			get
			{
				return new EditorCurveBinding
				{
					path = "", // assume SpriteRenderer is at same GameObject as AnimationController
					type = typeof(SpriteRenderer),
					propertyName = "m_Sprite"
				};
			}
		}

		public static EditorCurveBinding ImageCurveBinding
		{
			get
			{
				return new EditorCurveBinding
				{
					path = "", // assume Image is at same GameObject as AnimationController
					type = typeof(Image),
					propertyName = "m_Sprite"
				};
			}
		}
	}
}