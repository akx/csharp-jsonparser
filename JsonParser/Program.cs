using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace JsonParser {
	internal static class Program {
		private static void Main(string[] args) {
			FeatureTest();
			Console.Write("\n\n\n======\n\n\n");
			FromObjectTest();
			Console.Write("\n\n\n======\n\n\n");
			TestJSONFiles();
			Console.ReadKey();
		}

		private static void TestJSONFiles() {
			foreach (FileInfo fi in new DirectoryInfo(".").GetFiles("*.json")) {
				Console.Write(fi.Name.PadRight(16) + " ... ");
				using (var file = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read)) {
					var tr = new StreamReader(file, Encoding.UTF8);

					JsonValue obj;
					try {
						obj = JsonParser.Parse(tr);
					}
					catch (Exception exc) {
						if (!fi.Name.Contains("fail")) {
							Console.WriteLine("Shouldn't have failed at this file.");
							throw;
						}
						Console.WriteLine("  OK, failed with " + exc.Message);
						continue;
					}
					Console.WriteLine("  OK, success, regenerated JSON has {0} chars.", obj.ToJSON().Length);
				}
			}
		}

		private static void FeatureTest() {
			var root = JsonParser.Parse(@"{
    ""glossary"": {
        ""title"": ""example glossary"",
		""GlossDiv"": {
			""double"": 3.141,
			""integer"": -1024,
			""deleted"": true,
            ""title"": ""\\\\S\u3F3C"",
			""GlossList"": {
                ""GlossEntry"": {
                    ""ID"": ""SGML"",
					""SortAs"": ""SGML"",
					""GlossTerm"": ""Standard Generalized Markup Language"",
					""Acronym"": ""SGML"",
					""Abbrev"": ""ISO 8879:1986"",
					""GlossDef"": {
                        ""para"": ""A meta-markup language, used to create markup languages such as DocBook."",
						""GlossSeeAlso"": [""GML"", ""XML""]
                    },
					""GlossSee"": ""markup""
                }
            }
        }
    }
}");
			JsonParser.Parse(root.ToJSON()); // reparseable?
			Console.WriteLine(".glossary = {0}", root.Get("glossary"));
			var div = root.ResolvePath("glossary", "GlossDiv");
			Console.WriteLine("integer = {0}", div.Get("integer").IntValue);
			Console.WriteLine("double = {0}", div.Get("double").DoubleValue);
			Console.WriteLine("count = {0}", div.Count);
			Console.WriteLine("stringstringdict:");
			foreach (var kvp in div.GetStringStringDict()) {
				Console.WriteLine("  {0} : {1}", kvp.Key.PadRight(30), kvp.Value);
			}
			var entry = div.ResolvePath("GlossList", "GlossEntry");
			var seeAlso = entry.ResolvePath("GlossDef", "GlossSeeAlso");
			Console.WriteLine("SeeAlso first: {0}", seeAlso.Get(0).StrValue);
			Console.WriteLine("SeeAlso first by full path: {0}", root.ResolvePath("glossary.GlossDiv.GlossList.GlossEntry.GlossDef.GlossSeeAlso.0").StrValue);
			Console.WriteLine("glossee: {0}", entry.ResolvePath("GlossSee").StrValue);
			Console.WriteLine("Deleted: {0}", root.ResolvePath("glossary.GlossDiv.deleted").BoolValue);

			using (var ms = new MemoryStream()) {
				var sw = new StreamWriter(ms, Encoding.UTF32);
				root.ToJSON(sw);
				sw.Flush();
				Console.WriteLine("Writing API - JSON UTF-32 bytes: {0}", ms.Position);
			}
		}

		private static void FromObjectTest() {
			var jv = JsonValue.FromObject(new List<object> {
				"foo",
				3.141,
				10.1f,
				JsonValue.Double(640.33f),
				-1024,
				null,
				new Dictionary<string, bool> {
					{"hello", true},
					{"yes", false},
				},
				new Dictionary<int, List<object>> {
					{120, new List<object> {15, 16, 18}},
					{140, new List<object> {"yes", true, "false", null, 650.50, Decimal.Parse("548120.123106333", NumberStyles.Currency, CultureInfo.InvariantCulture)}},
				}
			});
			Console.Write(jv.ToJSON());
		}
	}
}