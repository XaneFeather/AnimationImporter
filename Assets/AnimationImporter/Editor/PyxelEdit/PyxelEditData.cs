using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AnimationImporter.Boomlagoon.JSON;

namespace AnimationImporter.PyxelEdit
{
	public class PyxelEditData
	{
		public Tileset Tileset = new Tileset();
		public Canvas Canvas = new Canvas();
		public string Name;
		public Animations Animations = new Animations();
		public string Version;
	}

	public class Tileset
	{
		public int TileWidth;
		public int TileHeight;
		public int TilesWide;
		public bool FixedWidth;
		public int NumTiles;
	}

	public class Animations : Dictionary<int, Animation>
	{
	}

	public class Canvas
	{
		public int Width;
		public int Height;
		public int TileWidth;
		public int TileHeight;
		public int NumLayers;
		public Layers Layers = new Layers();
	}

	public class Layers : Dictionary<int, Layer>
	{
	}

	public class Layer
	{
		public string Name;
		public int Alpha;
		public bool Hidden = false;
		public string BlendMode = "normal";

		public TileRefs TileRefs = new TileRefs();

		public Texture2D Texture = null;

		public Layer(JsonObject obj)
		{
			Name = obj["name"].Str;
			Alpha = (int)obj["alpha"].Number;
			Hidden = obj["hidden"].Boolean;
			BlendMode = obj["blendMode"].Str;

			foreach (var item in obj["tileRefs"].Obj)
			{
				TileRefs[int.Parse(item.Key)] = new TileRef(item.Value.Obj);
			}
		}
	}

	public class TileRefs : Dictionary<int, TileRef>
	{
	}

	public class TileRef
	{
		public int Index;
		public int Rot;
		public bool FlipX;

		public TileRef(JsonObject obj)
		{
			Index = (int)obj["index"].Number;
			Rot = (int)obj["rot"].Number;
			FlipX = obj["flipX"].Boolean;
		}
	}

	public class Animation
	{
		public string Name;
		public int BaseTile = 0;
		public int Length = 7;
		public int[] FrameDurationMultipliers;
		public int FrameDuration = 200;

		public Animation(JsonObject value)
		{
			Name = value["name"].Str;
			BaseTile = (int)value["baseTile"].Number;
			Length = (int)value["length"].Number;

			var list = value["frameDurationMultipliers"].Array;
			FrameDurationMultipliers = new int[list.Length];
			for (int i = 0; i < list.Length; i++)
			{
				FrameDurationMultipliers[i] = (int)list[i].Number;
			}

			FrameDuration = (int)value["frameDuration"].Number;
		}
	}
}