/* JSONObject.cs -- Simple C# JSON parser
  version 1.4 - March 17, 2014

  ## changed by Stephan Hövelbrinks (stephan.hoevelbrinks@craftinglegends.com)
     -- added InvariantCulture to string conversion
	 -- removed RegularExpressions System.Text.RegularExpressions from WinRT version

  Copyright (C) 2012 Boomlagoon Ltd.

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Boomlagoon Ltd.
  contact@boomlagoon.com

*/

#if !UNITY_WINRT
#define PARSE_ESCAPED_UNICODE
#endif

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WII || UNITY_PS3 || UNITY_XBOX360 || UNITY_FLASH
#define USE_UNITY_DEBUGGING
#endif

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if PARSE_ESCAPED_UNICODE
using System.Text.RegularExpressions;
#endif

#if USE_UNITY_DEBUGGING
using UnityEngine;
#else
using System.Diagnostics;
#endif

namespace AnimationImporter
{
	namespace Boomlagoon.JSON
	{

		public static class Extensions
		{
			public static T Pop<T>(this List<T> list)
			{
				var result = list[list.Count - 1];
				list.RemoveAt(list.Count - 1);
				return result;
			}
		}

		static class JsonLogger
		{
#if USE_UNITY_DEBUGGING
			public static void Log(string str)
			{
				Debug.Log(str);
			}
			public static void Error(string str)
			{
				Debug.LogError(str);
			}
#else
		public static void Log(string str) {
			Debug.WriteLine(str);
		}
		public static void Error(string str) {
			Debug.WriteLine(str);
		}
#endif
		}

		public enum JsonValueType
		{
			String,
			Number,
			Object,
			Array,
			Boolean,
			Null
		}

		public class JsonValue
		{

			public JsonValue(JsonValueType type)
			{
				Type = type;
			}

			public JsonValue(string str)
			{
				Type = JsonValueType.String;
				Str = str;
			}

			public JsonValue(double number)
			{
				Type = JsonValueType.Number;
				Number = number;
			}

			public JsonValue(JsonObject obj)
			{
				if (obj == null)
				{
					Type = JsonValueType.Null;
				}
				else {
					Type = JsonValueType.Object;
					Obj = obj;
				}
			}

			public JsonValue(JsonArray array)
			{
				Type = JsonValueType.Array;
				Array = array;
			}

			public JsonValue(bool boolean)
			{
				Type = JsonValueType.Boolean;
				Boolean = boolean;
			}

			/// <summary>
			/// Construct a copy of the JSONValue given as a parameter
			/// </summary>
			/// <param name="value"></param>
			public JsonValue(JsonValue value)
			{
				Type = value.Type;
				switch (Type)
				{
					case JsonValueType.String:
						Str = value.Str;
						break;

					case JsonValueType.Boolean:
						Boolean = value.Boolean;
						break;

					case JsonValueType.Number:
						Number = value.Number;
						break;

					case JsonValueType.Object:
						if (value.Obj != null)
						{
							Obj = new JsonObject(value.Obj);
						}
						break;

					case JsonValueType.Array:
						Array = new JsonArray(value.Array);
						break;
				}
			}

			public JsonValueType Type { get; private set; }
			public string Str { get; set; }
			public double Number { get; set; }
			public JsonObject Obj { get; set; }
			public JsonArray Array { get; set; }
			public bool Boolean { get; set; }
			public JsonValue Parent { get; set; }

			public static implicit operator JsonValue(string str)
			{
				return new JsonValue(str);
			}

			public static implicit operator JsonValue(double number)
			{
				return new JsonValue(number);
			}

			public static implicit operator JsonValue(JsonObject obj)
			{
				return new JsonValue(obj);
			}

			public static implicit operator JsonValue(JsonArray array)
			{
				return new JsonValue(array);
			}

			public static implicit operator JsonValue(bool boolean)
			{
				return new JsonValue(boolean);
			}

			/// <returns>String representation of this JSONValue</returns>
			public override string ToString()
			{
				switch (Type)
				{
					case JsonValueType.Object:
						return Obj.ToString();

					case JsonValueType.Array:
						return Array.ToString();

					case JsonValueType.Boolean:
						return Boolean ? "true" : "false";

					case JsonValueType.Number:
						return Number.ToString(System.Globalization.CultureInfo.InvariantCulture);

					case JsonValueType.String:
						return "\"" + Str + "\"";

					case JsonValueType.Null:
						return "null";
				}
				return "null";
			}

		}

		public class JsonArray : IEnumerable<JsonValue>
		{

			private readonly List<JsonValue> values = new List<JsonValue>();

			public JsonArray()
			{
			}

			/// <summary>
			/// Construct a new array and copy each value from the given array into the new one
			/// </summary>
			/// <param name="array"></param>
			public JsonArray(JsonArray array)
			{
				values = new List<JsonValue>();
				foreach (var v in array.values)
				{
					values.Add(new JsonValue(v));
				}
			}

			/// <summary>
			/// Add a JSONValue to this array
			/// </summary>
			/// <param name="value"></param>
			public void Add(JsonValue value)
			{
				values.Add(value);
			}

			public JsonValue this[int index]
			{
				get { return values[index]; }
				set { values[index] = value; }
			}

			/// <returns>
			/// Return the length of the array
			/// </returns>
			public int Length
			{
				get { return values.Count; }
			}

			/// <returns>String representation of this JSONArray</returns>
			public override string ToString()
			{
				var stringBuilder = new StringBuilder();
				stringBuilder.Append('[');
				foreach (var value in values)
				{
					stringBuilder.Append(value.ToString());
					stringBuilder.Append(',');
				}
				if (values.Count > 0)
				{
					stringBuilder.Remove(stringBuilder.Length - 1, 1);
				}
				stringBuilder.Append(']');
				return stringBuilder.ToString();
			}

			public IEnumerator<JsonValue> GetEnumerator()
			{
				return values.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return values.GetEnumerator();
			}

			/// <summary>
			/// Attempt to parse a string as a JSON array.
			/// </summary>
			/// <param name="jsonString"></param>
			/// <returns>A new JSONArray object if successful, null otherwise.</returns>
			public static JsonArray Parse(string jsonString)
			{
				var tempObject = JsonObject.Parse("{ \"array\" :" + jsonString + '}');
				return tempObject == null ? null : tempObject.GetValue("array").Array;
			}

			/// <summary>
			/// Empty the array of all values.
			/// </summary>
			public void Clear()
			{
				values.Clear();
			}

			/// <summary>
			/// Remove the value at the given index, if it exists.
			/// </summary>
			/// <param name="index"></param>
			public void Remove(int index)
			{
				if (index >= 0 && index < values.Count)
				{
					values.RemoveAt(index);
				}
				else {
					JsonLogger.Error("index out of range: " + index + " (Expected 0 <= index < " + values.Count + ")");
				}
			}

			/// <summary>
			/// Concatenate two JSONArrays
			/// </summary>
			/// <param name="lhs"></param>
			/// <param name="rhs"></param>
			/// <returns>A new JSONArray that is the result of adding all of the right-hand side array's values to the left-hand side array.</returns>
			public static JsonArray operator +(JsonArray lhs, JsonArray rhs)
			{
				var result = new JsonArray(lhs);
				foreach (var value in rhs.values)
				{
					result.Add(value);
				}
				return result;
			}

		}

		public class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>
		{

			private enum JsonParsingState
			{
				Object,
				Array,
				EndObject,
				EndArray,
				Key,
				Value,
				KeyValueSeparator,
				ValueSeparator,
				String,
				Number,
				Boolean,
				Null
			}

			private readonly IDictionary<string, JsonValue> values = new Dictionary<string, JsonValue>();

#if PARSE_ESCAPED_UNICODE
			private static readonly Regex UnicodeRegex = new Regex(@"\\u([0-9a-fA-F]{4})");
			private static readonly byte[] UnicodeBytes = new byte[2];
#endif

			public JsonObject()
			{
			}

			/// <summary>
			/// Construct a copy of the given JSONObject.
			/// </summary>
			/// <param name="other"></param>
			public JsonObject(JsonObject other)
			{
				values = new Dictionary<string, JsonValue>();

				if (other != null)
				{
					foreach (var keyValuePair in other.values)
					{
						values[keyValuePair.Key] = new JsonValue(keyValuePair.Value);
					}
				}
			}

			/// <param name="key"></param>
			/// <returns>Does 'key' exist in this object.</returns>
			public bool ContainsKey(string key)
			{
				return values.ContainsKey(key);
			}

			public JsonValue GetValue(string key)
			{
				JsonValue value;
				values.TryGetValue(key, out value);
				return value;
			}

			public string GetString(string key)
			{
				var value = GetValue(key);
				if (value == null)
				{
					JsonLogger.Error(key + "(string) == null");
					return string.Empty;
				}
				return value.Str;
			}

			public double GetNumber(string key)
			{
				var value = GetValue(key);
				if (value == null)
				{
					JsonLogger.Error(key + " == null");
					return double.NaN;
				}
				return value.Number;
			}

			public JsonObject GetObject(string key)
			{
				var value = GetValue(key);
				if (value == null)
				{
					JsonLogger.Error(key + " == null");
					return null;
				}
				return value.Obj;
			}

			public bool GetBoolean(string key)
			{
				var value = GetValue(key);
				if (value == null)
				{
					JsonLogger.Error(key + " == null");
					return false;
				}
				return value.Boolean;
			}

			public JsonArray GetArray(string key)
			{
				var value = GetValue(key);
				if (value == null)
				{
					JsonLogger.Error(key + " == null");
					return null;
				}
				return value.Array;
			}

			public JsonValue this[string key]
			{
				get { return GetValue(key); }
				set { values[key] = value; }
			}

			public void Add(string key, JsonValue value)
			{
				values[key] = value;
			}

			public void Add(KeyValuePair<string, JsonValue> pair)
			{
				values[pair.Key] = pair.Value;
			}

			/// <summary>
			/// Attempt to parse a string into a JSONObject.
			/// </summary>
			/// <param name="jsonString"></param>
			/// <returns>A new JSONObject or null if parsing fails.</returns>
			public static JsonObject Parse(string jsonString)
			{
				if (string.IsNullOrEmpty(jsonString))
				{
					return null;
				}

				JsonValue currentValue = null;

				var keyList = new List<string>();

				var state = JsonParsingState.Object;

				for (var startPosition = 0; startPosition < jsonString.Length; ++startPosition)
				{

					startPosition = SkipWhitespace(jsonString, startPosition);

					switch (state)
					{
						case JsonParsingState.Object:
							if (jsonString[startPosition] != '{')
							{
								return Fail('{', startPosition);
							}

							JsonValue newObj = new JsonObject();
							if (currentValue != null)
							{
								newObj.Parent = currentValue;
							}
							currentValue = newObj;

							state = JsonParsingState.Key;
							break;

						case JsonParsingState.EndObject:
							if (jsonString[startPosition] != '}')
							{
								return Fail('}', startPosition);
							}

							if (currentValue.Parent == null)
							{
								return currentValue.Obj;
							}

							switch (currentValue.Parent.Type)
							{

								case JsonValueType.Object:
									currentValue.Parent.Obj.values[keyList.Pop()] = new JsonValue(currentValue.Obj);
									break;

								case JsonValueType.Array:
									currentValue.Parent.Array.Add(new JsonValue(currentValue.Obj));
									break;

								default:
									return Fail("valid object", startPosition);

							}
							currentValue = currentValue.Parent;

							state = JsonParsingState.ValueSeparator;
							break;

						case JsonParsingState.Key:
							if (jsonString[startPosition] == '}')
							{
								--startPosition;
								state = JsonParsingState.EndObject;
								break;
							}

							var key = ParseString(jsonString, ref startPosition);
							if (key == null)
							{
								return Fail("key string", startPosition);
							}
							keyList.Add(key);
							state = JsonParsingState.KeyValueSeparator;
							break;

						case JsonParsingState.KeyValueSeparator:
							if (jsonString[startPosition] != ':')
							{
								return Fail(':', startPosition);
							}
							state = JsonParsingState.Value;
							break;

						case JsonParsingState.ValueSeparator:
							switch (jsonString[startPosition])
							{

								case ',':
									state = currentValue.Type == JsonValueType.Object ? JsonParsingState.Key : JsonParsingState.Value;
									break;

								case '}':
									state = JsonParsingState.EndObject;
									--startPosition;
									break;

								case ']':
									state = JsonParsingState.EndArray;
									--startPosition;
									break;

								default:
									return Fail(", } ]", startPosition);
							}
							break;

						case JsonParsingState.Value:
							{
								var c = jsonString[startPosition];
								if (c == '"')
								{
									state = JsonParsingState.String;
								}
								else if (char.IsDigit(c) || c == '-')
								{
									state = JsonParsingState.Number;
								}
								else {
									switch (c)
									{

										case '{':
											state = JsonParsingState.Object;
											break;

										case '[':
											state = JsonParsingState.Array;
											break;

										case ']':
											if (currentValue.Type == JsonValueType.Array)
											{
												state = JsonParsingState.EndArray;
											}
											else {
												return Fail("valid array", startPosition);
											}
											break;

										case 'f':
										case 't':
											state = JsonParsingState.Boolean;
											break;


										case 'n':
											state = JsonParsingState.Null;
											break;

										default:
											return Fail("beginning of value", startPosition);
									}
								}

								--startPosition; //To re-evaluate this char in the newly selected state
								break;
							}

						case JsonParsingState.String:
							var str = ParseString(jsonString, ref startPosition);
							if (str == null)
							{
								return Fail("string value", startPosition);
							}

							switch (currentValue.Type)
							{

								case JsonValueType.Object:
									currentValue.Obj.values[keyList.Pop()] = new JsonValue(str);
									break;

								case JsonValueType.Array:
									currentValue.Array.Add(str);
									break;

								default:
									JsonLogger.Error("Fatal error, current JSON value not valid");
									return null;
							}

							state = JsonParsingState.ValueSeparator;
							break;

						case JsonParsingState.Number:
							var number = ParseNumber(jsonString, ref startPosition);
							if (double.IsNaN(number))
							{
								return Fail("valid number", startPosition);
							}

							switch (currentValue.Type)
							{

								case JsonValueType.Object:
									currentValue.Obj.values[keyList.Pop()] = new JsonValue(number);
									break;

								case JsonValueType.Array:
									currentValue.Array.Add(number);
									break;

								default:
									JsonLogger.Error("Fatal error, current JSON value not valid");
									return null;
							}

							state = JsonParsingState.ValueSeparator;

							break;

						case JsonParsingState.Boolean:
							if (jsonString[startPosition] == 't')
							{
								if (jsonString.Length < startPosition + 4 ||
									jsonString[startPosition + 1] != 'r' ||
									jsonString[startPosition + 2] != 'u' ||
									jsonString[startPosition + 3] != 'e')
								{
									return Fail("true", startPosition);
								}

								switch (currentValue.Type)
								{

									case JsonValueType.Object:
										currentValue.Obj.values[keyList.Pop()] = new JsonValue(true);
										break;

									case JsonValueType.Array:
										currentValue.Array.Add(new JsonValue(true));
										break;

									default:
										JsonLogger.Error("Fatal error, current JSON value not valid");
										return null;
								}

								startPosition += 3;
							}
							else {
								if (jsonString.Length < startPosition + 5 ||
									jsonString[startPosition + 1] != 'a' ||
									jsonString[startPosition + 2] != 'l' ||
									jsonString[startPosition + 3] != 's' ||
									jsonString[startPosition + 4] != 'e')
								{
									return Fail("false", startPosition);
								}

								switch (currentValue.Type)
								{

									case JsonValueType.Object:
										currentValue.Obj.values[keyList.Pop()] = new JsonValue(false);
										break;

									case JsonValueType.Array:
										currentValue.Array.Add(new JsonValue(false));
										break;

									default:
										JsonLogger.Error("Fatal error, current JSON value not valid");
										return null;
								}

								startPosition += 4;
							}

							state = JsonParsingState.ValueSeparator;
							break;

						case JsonParsingState.Array:
							if (jsonString[startPosition] != '[')
							{
								return Fail('[', startPosition);
							}

							JsonValue newArray = new JsonArray();
							if (currentValue != null)
							{
								newArray.Parent = currentValue;
							}
							currentValue = newArray;

							state = JsonParsingState.Value;
							break;

						case JsonParsingState.EndArray:
							if (jsonString[startPosition] != ']')
							{
								return Fail(']', startPosition);
							}

							if (currentValue.Parent == null)
							{
								return currentValue.Obj;
							}

							switch (currentValue.Parent.Type)
							{

								case JsonValueType.Object:
									currentValue.Parent.Obj.values[keyList.Pop()] = new JsonValue(currentValue.Array);
									break;

								case JsonValueType.Array:
									currentValue.Parent.Array.Add(new JsonValue(currentValue.Array));
									break;

								default:
									return Fail("valid object", startPosition);
							}
							currentValue = currentValue.Parent;

							state = JsonParsingState.ValueSeparator;
							break;

						case JsonParsingState.Null:
							if (jsonString[startPosition] == 'n')
							{
								if (jsonString.Length < startPosition + 4 ||
									jsonString[startPosition + 1] != 'u' ||
									jsonString[startPosition + 2] != 'l' ||
									jsonString[startPosition + 3] != 'l')
								{
									return Fail("null", startPosition);
								}

								switch (currentValue.Type)
								{

									case JsonValueType.Object:
										currentValue.Obj.values[keyList.Pop()] = new JsonValue(JsonValueType.Null);
										break;

									case JsonValueType.Array:
										currentValue.Array.Add(new JsonValue(JsonValueType.Null));
										break;

									default:
										JsonLogger.Error("Fatal error, current JSON value not valid");
										return null;
								}

								startPosition += 3;
							}
							state = JsonParsingState.ValueSeparator;
							break;

					}
				}
				JsonLogger.Error("Unexpected end of string");
				return null;
			}

			private static int SkipWhitespace(string str, int pos)
			{
				for (; pos < str.Length && char.IsWhiteSpace(str[pos]); ++pos) {
					;
				}

				return pos;
			}

			private static string ParseString(string str, ref int startPosition)
			{
				if (str[startPosition] != '"' || startPosition + 1 >= str.Length)
				{
					Fail('"', startPosition);
					return null;
				}

				var endPosition = str.IndexOf('"', startPosition + 1);
				if (endPosition <= startPosition)
				{
					Fail('"', startPosition + 1);
					return null;
				}

				while (str[endPosition - 1] == '\\')
				{
					endPosition = str.IndexOf('"', endPosition + 1);
					if (endPosition <= startPosition)
					{
						Fail('"', startPosition + 1);
						return null;
					}
				}

				var result = string.Empty;

				if (endPosition > startPosition + 1)
				{
					result = str.Substring(startPosition + 1, endPosition - startPosition - 1);
				}

				startPosition = endPosition;

#if PARSE_ESCAPED_UNICODE
				// Parse Unicode characters that are escaped as \uXXXX
				do
				{
					Match m = UnicodeRegex.Match(result);
					if (!m.Success)
					{
						break;
					}

					string s = m.Groups[1].Captures[0].Value;
					UnicodeBytes[1] = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
					UnicodeBytes[0] = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
					s = Encoding.Unicode.GetString(UnicodeBytes);

					result = result.Replace(m.Value, s);
				} while (true);
#endif

				return result;
			}

			private static double ParseNumber(string str, ref int startPosition)
			{
				if (startPosition >= str.Length || (!char.IsDigit(str[startPosition]) && str[startPosition] != '-'))
				{
					return double.NaN;
				}

				var endPosition = startPosition + 1;

				for (;
					endPosition < str.Length && str[endPosition] != ',' && str[endPosition] != ']' && str[endPosition] != '}';
					++endPosition) {
					;
				}

				double result;
				if (
					!double.TryParse(str.Substring(startPosition, endPosition - startPosition), System.Globalization.NumberStyles.Float,
									 System.Globalization.CultureInfo.InvariantCulture, out result))
				{
					return double.NaN;
				}
				startPosition = endPosition - 1;
				return result;
			}

			private static JsonObject Fail(char expected, int position)
			{
				return Fail(new string(expected, 1), position);
			}

			private static JsonObject Fail(string expected, int position)
			{
				JsonLogger.Error("Invalid json string, expecting " + expected + " at " + position);
				return null;
			}

			/// <returns>String representation of this JSONObject</returns>
			public override string ToString()
			{
				var stringBuilder = new StringBuilder();
				stringBuilder.Append('{');

				foreach (var pair in values)
				{
					stringBuilder.Append("\"" + pair.Key + "\"");
					stringBuilder.Append(':');
					stringBuilder.Append(pair.Value.ToString());
					stringBuilder.Append(',');
				}
				if (values.Count > 0)
				{
					stringBuilder.Remove(stringBuilder.Length - 1, 1);
				}
				stringBuilder.Append('}');
				return stringBuilder.ToString();
			}

			public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
			{
				return values.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return values.GetEnumerator();
			}

			/// <summary>
			/// Empty this JSONObject of all values.
			/// </summary>
			public void Clear()
			{
				values.Clear();
			}

			/// <summary>
			/// Remove the JSONValue attached to the given key.
			/// </summary>
			/// <param name="key"></param>
			public void Remove(string key)
			{
				if (values.ContainsKey(key))
				{
					values.Remove(key);
				}
			}
		}
	}
}