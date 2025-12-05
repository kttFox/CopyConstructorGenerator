using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1 {
	class Class1 {


		public Class1() {

		}

		public W w { get; } = W.a;

		public int? num1 { get; set; }
		public string GetOnly { get; } = "getto";


		public static string _static = "static";

		const string constString = "こんすと";

		public List<List<List<Class1>>> _multiList;

		public string test { get; set; }
		public double prop { get; set; }

		private int num { get; set; }

		private bool flag { get; set; }

		public string str;
		private string privateStr;

		public Class1 class1 { get; set; }

		public List<string> list { get; set; }

		public Dictionary<string, int> _dic;
		public Dictionary<Dictionary<List<int>, List<string>>, int> _dic2;

		Action<Class1> action;

		ReadOnlyCollection<string> ReadOnlyCollection {get; set;}
		ReadOnlyDictionary<string,string> ReadOnlyDictionary {get; set;}

	}


	enum W {
		a, b, c, d
	}
}
