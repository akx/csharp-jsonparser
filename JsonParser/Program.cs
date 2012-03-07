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
			var el = JsonParser.Parse(testJSON);
			var json = el.ToJSON();
			Debug.Print(el.ToString());
			Debug.Print(json);
			var el2 = JsonParser.Parse(json);
			Debug.Print("eq? {0}", el.Equals(el2));
			var ent = el.ResolvePath("glossary", "GlossDiv", "GlossList", "GlossEntry");
			var seeAlso = ent.ResolvePath("GlossDef", "GlossSeeAlso");
			Debug.Print("SeeAlso first: {0}", seeAlso.Get(0).StrValue);
			Debug.Print("glossee: {0}", ent.ResolvePath("GlossSee").StrValue);
		}
	}
}
