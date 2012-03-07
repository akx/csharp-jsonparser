﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonParser
{

	public enum ParseState
	{
		ListValue,
		DictKey,
		DictValue,
		DictColon,
		Root
	};

	public enum JsonValueType {
		Dict,
		List,
		String,
		Integer,
		Double,
		Boolean,
		Null
	}

	public class JsonValue : IEquatable<JsonValue> {
		private JsonValueType _type;
		private Dictionary<JsonValue, JsonValue> _dictValue;
		private List<JsonValue> _listValue;
		private string _strValue;
		private bool _boolValue;
		private int _intValue;
		private double _doubleValue;

		public JsonValueType Type {
			get { return _type; }
		}

		#region Value Getters

		public JsonValue Get(string key) {
			if (_type == JsonValueType.Dict) {
				var dict = DictValue;
				if (dict != null) {
					foreach (var kvp in dict) {
						if (kvp.Key.StrValue == key) return kvp.Value;
					}
				}
			}
			if(_type == JsonValueType.List) {
				return Get(Convert.ToInt32(key));
			}
			return JsonValue.Null();
		}

		public JsonValue Get(int index) {
			if (_type == JsonValueType.List) {
				if (index >= 0 && index < _listValue.Count) {
					return _listValue[index];
				}
			}
			return JsonValue.Null();
		}

		public JsonValue ResolvePath(string path) {
			return ResolvePath(path.Split('.'));
		}

		public JsonValue ResolvePath(params string[] keys) {
			var curr = this;
			foreach (var key in keys) {
				if (!(curr._type == JsonValueType.Dict || curr._type == JsonValueType.List)) return JsonValue.Null();
				curr = curr.Get(key);
			}
			return curr;
		}

		public Dictionary<JsonValue, JsonValue> DictValue {
			get { return (_type == JsonValueType.Dict ? _dictValue : null); }
		}

		public List<JsonValue> ListValue {
			get { return (_type == JsonValueType.List ? _listValue : null); }
		}

		public string StrValue {
			get {
				if(_type == JsonValueType.String) return _strValue;
				if (_type == JsonValueType.Boolean) return _boolValue.ToString(CultureInfo.InvariantCulture);
				if (_type == JsonValueType.Integer) return _intValue.ToString(CultureInfo.InvariantCulture);
				if (_type == JsonValueType.Double) return _doubleValue.ToString(CultureInfo.InvariantCulture);
				if (_type == JsonValueType.Dict) return string.Format("(dict with {0} entries)", _dictValue.Count);
				if (_type == JsonValueType.List) return string.Format("(list with {0} entries)", _listValue.Count);
				return "";
			}
		}

		public bool BoolValue {
			get {
				if (_type == JsonValueType.String) return !string.IsNullOrWhiteSpace(_strValue);
				if (_type == JsonValueType.Boolean) return _boolValue;
				if (_type == JsonValueType.Integer) return (_intValue != 0);
				if (_type == JsonValueType.Double) return (_doubleValue != 0);
				if (_type == JsonValueType.List) return (_listValue.Count > 0);
				return false;
			}
		}

		public int IntValue {
			get {
				if (_type == JsonValueType.Boolean) return (_boolValue ? 1 : 0);
				if (_type == JsonValueType.Integer) return _intValue;
				if (_type == JsonValueType.Double) return (int)_doubleValue;
				return 0;
			}
		}

		public double DoubleValue {
			get {
				if (_type == JsonValueType.Boolean) return (_boolValue ? 1.0 : 0.0);
				if (_type == JsonValueType.Integer) return (double)_intValue;
				if (_type == JsonValueType.Double) return _doubleValue;
				return 0.0;
			}
		}

		public int Count {
			get {
				if (_type == JsonValueType.Boolean && _boolValue == false) return 0;
				if (_type == JsonValueType.Null) return 0;
				if (_type == JsonValueType.String) return _strValue.Length;
				if (_type == JsonValueType.List) return _listValue.Count;
				if (_type == JsonValueType.Dict) return _dictValue.Count;
				return 1;
			}
		}

		#endregion

		#region Casts

		public Dictionary<string, string> GetStringStringDict() {
			var outDict = new Dictionary<string, string>();
			var inDict = this.DictValue;
			if(inDict != null) {
				foreach (var kvp in inDict) {
					outDict[kvp.Key.StrValue] = kvp.Value.StrValue;
				}
			}
			return outDict;
		}

		
		public static implicit operator int(JsonValue val) {
			return val.IntValue; 
		}
		public static implicit operator double(JsonValue val) {
			return val.DoubleValue;
		}
		public static implicit operator string(JsonValue val) {
			return val.StrValue;
		}
		public static implicit operator bool(JsonValue val) {
			return val.BoolValue;
		}


		#endregion

		#region Builders
		public static JsonValue Dictionary() {
			return new JsonValue() {_type = JsonValueType.Dict, _dictValue = new Dictionary<JsonValue, JsonValue>()};
		}

		public static JsonValue List() {
			return new JsonValue() { _type = JsonValueType.List, _listValue = new List<JsonValue>() };
		}

		public static JsonValue String(string content) {
			return new JsonValue() { _type = JsonValueType.String, _strValue = content };
		}

		public static JsonValue Boolean(bool value) {
			return new JsonValue() { _type = JsonValueType.Boolean, _boolValue = value };
		}

		public static JsonValue Null() {
			return new JsonValue() {_type = JsonValueType.Null};
		}

		public static JsonValue Double(double d) {
			return new JsonValue() { _type = JsonValueType.Double, _doubleValue = d };
		}

		public static JsonValue Integer(int i) {
			return new JsonValue() { _type = JsonValueType.Integer, _intValue = i };
		}
		#endregion

		public override string ToString() {
			var r = string.Format("JSON {0} ({1})", _type, StrValue);
			return r;
		}

		#region JSON Serializers

		public string ToJSON() {
			var sb = new StringBuilder();
			ToJSON(sb);
			return sb.ToString();
		}
		public void ToJSON(StringBuilder sb) {
			
			switch (_type) {
				case JsonValueType.Dict: {
					sb.Append('{');
					bool first = true;
					foreach (var kvp in _dictValue) {
						if (!first) sb.Append(',');
						kvp.Key.ToJSON(sb);
						sb.Append(':');
						kvp.Value.ToJSON(sb);
						first = false;
					}
					sb.Append('}');
				} break;

				case JsonValueType.List: {
					sb.Append('[');
					bool first = true;
					foreach (var val in _listValue) {
						if (!first) sb.Append(',');
						val.ToJSON(sb);
						first = false;
					}
					sb.Append(']');
				} break;
				case JsonValueType.String:
					sb.Append('"');
					foreach (var chr in _strValue) {
						switch (chr) {
							case '\\':
								sb.Append("\\\\");
								continue;
							case '\n':
								sb.Append("\\n");
								continue;
							case '\r':
								sb.Append("\\r");
								continue;

						}
						if(Char.IsControl(chr)) {
							sb.Append("\\u");
							sb.AppendFormat("{0:X4}", (int) chr);
							continue;
						}
						sb.Append(chr);
					}
					sb.Append('"');
					break;
				case JsonValueType.Integer:
					sb.Append(_intValue.ToString(CultureInfo.InvariantCulture));
					break;
				case JsonValueType.Double:
					sb.Append(_doubleValue.ToString(CultureInfo.InvariantCulture));
					break;
				case JsonValueType.Boolean:
					sb.Append(_boolValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
					break;
				case JsonValueType.Null:
					sb.Append("null");
					break;
			}
		}

		#endregion

		#region Equality

		public bool Equals(JsonValue other) {
			if (ReferenceEquals(other, null)) return false;
			if (ReferenceEquals(this, other)) return true;
			if (other._type != _type) return false;
			if (_type == JsonValueType.List) {
				if(_listValue.Count != other._listValue.Count) return false;
				for (int i = 0; i < _listValue.Count; i++) {
					if (_listValue[i] != other._listValue[i]) return false;
				}
				return true;
			}
			if (_type == JsonValueType.Dict) {
				foreach(var key in _dictValue.Keys) {
					JsonValue myV = _dictValue[key], otherV;
					if(!other._dictValue.TryGetValue(key, out otherV)) {
						return false;
					}
					if(!myV.Equals(otherV)) return false;
				}
				return true;
			}

			if (StrValue == other.StrValue) return true; // XXX: This may not be appropriate
			return false;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(obj, null)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (JsonValue)) {
				return false;
			}
			return Equals((JsonValue) obj);
		}

		public static bool operator ==(JsonValue left, JsonValue right) {
			return Equals(left, right);
		}

		public static bool operator !=(JsonValue left, JsonValue right) {
			return !Equals(left, right);
		}

		public override int GetHashCode() {
			unchecked {
				return Type.GetHashCode() << 24 | StrValue.GetHashCode();
			}
		}

		#endregion
	}

	class JsonParser {
		private TextReader reader;
		private ParseState state;
		private Stack<JsonValue> stack = new Stack<JsonValue>();
		private static readonly Regex _numberRe = new Regex("^-?[0-9]*(\\.[0-9]*)?([eE][+-][0-9]*)?$");
		private JsonValue root;

		public static JsonValue Parse(string JSON) {
			return new JsonParser(new StringReader(JSON)).Parse();
		}

		public JsonParser(TextReader reader) {
			this.reader = reader;
			this.state = ParseState.Root;
		}

		public JsonValue Parse() {
			while (ParseNext()) {
				// ...
			}
			return root;
		}

		private void Push(JsonValue obj) {
			if (state == ParseState.Root) root = obj;
			stack.Push(obj);
		}

		private bool ParseNext() {
			char chr;
			
			while(true) {
				int chrInt = reader.Peek();
				if (chrInt == -1) return false;
				chr = (char) chrInt;
				if(!(Char.IsWhiteSpace(chr) || chr == ',')) {
					break;
				}
				reader.Read();
			}
			
			if(state == ParseState.DictColon) {
				if(chr != ':') throw new Exception("Was expecting :, got " + chr);
				reader.Read(); // eat the colon
				state = ParseState.DictValue;
				return true;
			}

			if(chr == '{') {
				reader.Read(); // Nom the curlybrace
				var dict = JsonValue.Dictionary();
				Put(dict);
				Push(dict);
				state = ParseState.DictKey;
				return true;
			}
			if (chr == '[') {
				reader.Read(); // Nom the brace
				var list = JsonValue.List();
				Put(list);
				Push(list);
				state = ParseState.ListValue;
				return true;
			}

			if(chr == '}') {
				reader.Read(); // Nom the curlybrace
				if(state != ParseState.DictKey) throw new Exception("Wasn't expecting } at this point");
				if(stack.Peek().Type != JsonValueType.Dict) throw new Exception("Unexpected }, last object on stack not a dict");
				stack.Pop();
				return true;
			}

			if (chr == ']') {
				reader.Read(); // Nom the brace
				if (stack.Peek().Type != JsonValueType.List) throw new Exception("Unexpected ], last object on stack not a list");
				stack.Pop();
				var topType = stack.Peek().Type;
				switch(topType) {
					case JsonValueType.List:
						state = ParseState.ListValue;
						break;
					case JsonValueType.Dict:
						state = ParseState.DictKey;
						break;
					default:
						throw new Exception("Unexpected stacktop type: " + topType.ToString());
				}
				return true;
			}

			// basic values

			JsonValue val = ReadJsonConstant(chr);
			if (val == null) {
				if (chr == '"') {
					reader.Read(); // Nom the quote
					val = ReadJSONString();
				} else {
					val = ReadJSONNumber();
				}
			}


			if(state == ParseState.DictKey) {
				Push(val);
				state = ParseState.DictColon;
				return true;
			}

			if (Put(val)) return true;

			throw  new Exception("Unexpected parse state.");
			
		}

		private bool Put(JsonValue val) {
			if(state == ParseState.DictValue) {
				PutInDict(val);
				return true;
			}

			if (state == ParseState.ListValue) {
				PutInList(val);
				return true;
			}
			return false;
		}

		private void PutInList(JsonValue val) {
			stack.Peek().ListValue.Add(val);
			state = ParseState.ListValue;
		}

		private void PutInDict(JsonValue val) {
			var key = stack.Pop();
			var jsonValue = stack.Peek();
			jsonValue.DictValue[key] = val;
			state = ParseState.DictKey;
		}

		private JsonValue ReadJSONNumber() {
			var sb = new StringBuilder(15);
			while (true) {
				int chr = reader.Peek();
				if (chr == -1) {
					break;
				}
				sb.Append((char) chr);
				var match = _numberRe.Match(sb.ToString(), 0);
				if (!match.Success) {
					sb.Remove(sb.Length - 1, 1); // snip the last character off
					break;
				}
				reader.Read(); // okay, read the last one then
			}
			var numStr = sb.ToString();
			double d = Double.Parse(numStr, CultureInfo.InvariantCulture);
			if (d == Math.Floor(d)) return JsonValue.Integer((int) d);
			return JsonValue.Double(d);

		}

		private void _ReadConstant(string content) {
			char[] arr = new char[content.Length];
			int len = reader.Read(arr, 0, arr.Length);
			var cString = new String(arr);
			if(cString != content.Substring(1)) {
				throw new Exception("Was expecting '" + content + "', got '" + cString + "'");
			}
		}

		private JsonValue ReadJsonConstant(int chr) {
			if (chr == 'f') {
				_ReadConstant("false");
				return JsonValue.Boolean(false);
			}
			if (chr == 't') {
				_ReadConstant("true");
				return JsonValue.Boolean(true);
			}
			if (chr == 'n') {
				_ReadConstant("null");
				return JsonValue.Null();
			}
			return null;
		}

		private JsonValue ReadJSONString() {
			var sb = new StringBuilder(32);
			bool q = false;
			while (true) {
				int chrR = reader.Read();
				if (chrR == -1) {
					throw new Exception("End of stream while scanning string");
				}
				var chr = (char) chrR;
				if (chr == '"') {
					break;
				}
				if (chr == '\\') {
					q = true;
					continue;
				}
				if (q) {
					switch (chr) {
						case '"':
							sb.Append('"');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case '/':
							sb.Append('/');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'u':
							var hexb = new char[4];
							reader.Read(hexb, 0, 4);
							var ch = (char) Convert.ToInt32(new string(hexb), 16);
							sb.Append(ch);
							break;
						default:
							sb.Append(chr);
							break;
					}
					q = false;
					continue;
				}
				sb.Append(chr);
			}
			return JsonValue.String(sb.ToString());
		}
	}
}
