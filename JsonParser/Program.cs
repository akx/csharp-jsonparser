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
            ""title"": ""S\u3F3C"",
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
			StringReader sr = new StringReader(testJSON);
			JsonParser jp = new JsonParser(sr);
			var root = jp.Parse();
			Debug.Print(root.ToJSON());

		}
	}
}
