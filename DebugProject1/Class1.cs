using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0290
#pragma warning disable IDE0305

namespace Project1 {
	class Class1 {
		public int Number { get; set; }
		public string String { get; set; }
		public DateTime DateTime { get; set; }
		public DayOfWeek DayOfWeek { get; set; }
		public Class1 Class { get; set; }
		public List<string> List { get; set; }
		public List<Class1> List2 { get; set; }
		public List<List<string>> MultiList { get; set; }
		public Dictionary<string, int> Dictionary { get; set; }

		public string f_String = "FieldString";
		public int f_Number = 0;
		public DateTime f_DateTime;


		public static string StaticString { get; set; } = "StaticString";
		public const string ConstString = "ConstString";

		public Action<Class1> Action { get; set; }

		public ReadOnlyCollection<string> ReadOnlyCollection { get; set; }
		public ReadOnlyDictionary<string, string> ReadOnlyDictionary { get; set; }


	}

}
