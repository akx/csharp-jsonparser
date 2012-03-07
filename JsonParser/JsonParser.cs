using System;
using System.Collections.Generic;
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

	public class JsonValue
	{
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
				return "";
			}
		}

		public bool BoolValue {
			get { return (_type == JsonValueType.Boolean ? _boolValue : false); }
		}

		public int IntValue {
			get { return (_type == JsonValueType.Integer ? _intValue : 0); }
		}

		public double DoubleValue {
			get { return (_type == JsonValueType.Double ? _doubleValue : 0.0); }
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
		
		public static explicit operator int(JsonValue val) {
			return val.IntValue; 
		}
		public static explicit operator double(JsonValue val) {
			return val.DoubleValue;
		}
		public static explicit operator string(JsonValue val) {
			return val.StrValue;
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
			var r = string.Format("JsonValue({0}: {1})", _type, StrValue);
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
						if(chr == '\\') {
							sb.Append("\\\\");
							continue;
						}
						if(Char.IsControl(chr)) { // XXX: Use shorter representations?
							sb.Append("\\u");
							sb.AppendFormat("{0:4X}", (int) chr);
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
	}

	class JsonParser {
		private TextReader reader;
		private ParseState state;
		private Stack<JsonValue> stack = new Stack<JsonValue>();
		private static readonly Regex _numberRe = new Regex("^-?[0-9]*(\\.[0-9]*)?([eE][+-][0-9]*)?$");
		private JsonValue root;

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
