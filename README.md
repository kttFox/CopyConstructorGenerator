# CopyConstructorGenerator
Visual Studio 2022 / 2026 Extension  

コピーコンストラクタを作成します。  
class, List, Dictionary に対応しています。

# CopyConstructorGenerator
Visual Studio 2022 / 2026 Extension  

コピーコンストラクタを作成します。  
class, List, Dictionary に対応しています。  
nullable に対応

```cs
class Class1 {
	/// <summary>
	/// コピーコンストラクター
	/// </summary>
	public Class1 ( Class1 value ) {
		this.Number = value.Number;
		this.String = value.String;
		this.DateTime = value.DateTime;
		this.DayOfWeek = value.DayOfWeek;
		this.ClassA = new Class1( value.ClassA );
		this.ClassB = value.ClassB is not null ? new Class1( value.ClassB ) : null;
		this.List = value.List.ToList();
		this.List2 = value.List2?.Select( x => new Class1( x ) ).ToList();
		this.MultiList = value.MultiList.Select( x => x.ToList() ).ToList();
		this.Dictionary = value.Dictionary.ToDictionary( k => k.Key, v => v.Value );

		this.f_String = value.f_String;
		this.f_Number = value.f_Number;
		this.f_DateTime = value.f_DateTime;
	}

	public int Number { get; set; }
	public string String { get; set; }
	public DateTime DateTime { get; set; }
	public DayOfWeek DayOfWeek { get; set; }
	public Class1 ClassA { get; set; }
	public Class1? ClassB { get; set; }
	public List<string> List { get; set; }
	public List<Class1>? List2 { get; set; }
	public List<List<string>> MultiList { get; set; }
	public Dictionary<string, int> Dictionary { get; set; }

	public string f_String = "FieldString";
	public int f_Number = 0;
	public DateTime f_DateTime;
}
```

## Visual Studio 拡張機能  
https://marketplace.visualstudio.com/items?itemName=kttFox.CopyConstructorGenerator

## Nuget パッケージ  
https://www.nuget.org/packages/CopyConstructorGenerator
