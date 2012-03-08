using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonParser
{
	class Program {
		private const string testJSON = @"{
    ""glossary"": {
        ""title"": ""example glossary"",
		""GlossDiv"": {
			""awesomeness"": 3.141,
			""uberawesome"": -1024,
			""deleted"": true,
            ""title"": ""\\\\S\u3F3C\b\r\n"",
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
}";


		static void Main(string[] args) {
			FeatureTest();
			Console.Write("\n\n\n======\n\n\n");
			TestJSONFiles();
			Console.ReadKey();
		}

		private static void TestJSONFiles() {
			foreach (FileInfo fi in new DirectoryInfo(".").GetFiles("*.json")) {
				Console.WriteLine(fi.Name + " ...");
				using (var file = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read)) {
					var tr = new StreamReader(file, Encoding.UTF8);
					
					JsonValue obj = null;
					try {
						obj = JsonParser.Parse(tr);
					} catch(Exception exc) {
						if(!fi.Name.Contains("fail")) {
							Console.WriteLine("Shouldn't have failed at this file.");
							throw;
						}
						Console.WriteLine("  OK, failed with " + exc.Message);
						continue;
					}
					Console.WriteLine("  OK, success.");
				}
			}
		}

		private static void FeatureTest() {
			var el = JsonParser.Parse(testJSON);
			var json = el.ToJSON();
			Console.WriteLine(el.ToString());
			Console.WriteLine(json);
			var el2 = JsonParser.Parse(json);
			var ent = el.ResolvePath("glossary", "GlossDiv", "GlossList", "GlossEntry");
			var seeAlso = ent.ResolvePath("GlossDef", "GlossSeeAlso");
			Console.WriteLine("SeeAlso first: {0}", seeAlso.Get(0).StrValue);
			Console.WriteLine("SeeAlso2 first: {0}", el.ResolvePath("glossary.GlossDiv.GlossList.GlossEntry.GlossDef.GlossSeeAlso.0").StrValue);
			Console.WriteLine("glossee: {0}", ent.ResolvePath("GlossSee").StrValue);
			Console.WriteLine("Deleted: {0}", el.ResolvePath("glossary.GlossDiv.deleted").BoolValue);
		}
	}
}
