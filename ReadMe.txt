/*--------------------------------------------------------------------------
* DynamicJson
* ver 1.2.0.0 (May. 21th, 2010)
*
* created and maintained by neuecc <ils@neue.cc>
* licensed under Microsoft Public License(Ms-PL)
* http://neue.cc/
* http://dynamicjson.codeplex.com/
*--------------------------------------------------------------------------*/

// InstallGuide

Add Reference DynamicJson.dll
or
include DynamicJson.cs and Add Reference "System.Runtime.Serialization"

// HowToUse

using Codeplex.Data;

// Read and Access

// Parse (from JsonString to DynamicJson)
var json = DynamicJson.Parse(@"{""foo"":""json"", ""bar"":100, ""nest"":{ ""foobar"":true } }");

var r1 = json.foo; // "json" - dynamic(string)
var r2 = json.bar; // 100 - dynamic(double)
var r3 = json.nest.foobar; // true - dynamic(bool)
var r4 = json["nest"]["foobar"]; // can access indexer

// Operate

// Check Defined Peroperty
// .name() is shortcut of IsDefined("name")
var b1_1 = json.IsDefined("foo"); // true
var b2_1 = json.IsDefined("foooo"); // false
var b1_2 = json.foo(); // true            
var b2_2 = json.foooo(); // false;

// Add
json.Arr = new string[] { "NOR", "XOR" }; // Add Array
json.Obj1 = new { }; // Add Object
json.Obj2 = new { foo = "abc", bar = 100 }; // Add and Init

// Delete
// ("name") is shortcut of Delete("name")
json.Delete("foo");
json.Arr.Delete(0);
json("bar");
json.Arr(1);

// Replace
json.Obj1 = 5000;

// Create New JsonObject
dynamic newjson = new DynamicJson();
newjson.str = "aaa";
newjson.obj = new { foo = "bar" };

// Serialize
var reJson = json.ToString(); // {"nest":{"foobar":true},"Arr":["XOR"],"Obj1":5000,"Obj2":{"foo":"abc","bar":100}}


// Enumerate

// DynamicJson - (IsArray)
var arrayJson = DynamicJson.Parse(@"[1,10,200,300]");
foreach (int item in arrayJson)
{
    Console.WriteLine(item); // 1, 10, 200, 300
}

// DynamicJson - (IsObject)
var objectJson = DynamicJson.Parse(@"{""foo"":""json"",""bar"":100}");
foreach (KeyValuePair<string, object> item in objectJson)
{
    Console.WriteLine(item.Key + ":" + item.Value); // foo:json, bar:100
}


// Convert/Deserialize

public class FooBar
{
    public string foo { get; set; }
    public int bar { get; set; }
}

// (type) is shortcut of Deserialize<type>()
var array1 = arrayJson.Deserialize<int[]>();
var array2 = (int[])arrayJson;
int[] array3 = arrayJson;

// mapping by public property name
var foobar1 = objectJson.Deserialize<FooBar>();
var foobar2 = (FooBar)objectJson;
FooBar foobar3 = objectJson;

// with linq
var objectJsonList = DynamicJson.Parse(@"[{""bar"":50},{""bar"":100}]");
var barSum = ((FooBar[])objectJsonList).Select(fb => fb.bar).Sum(); // 150
var dynamicWithLinq = ((dynamic[])objectJsonList).Select(d => d.bar);

// Serialize to JSON from Object

// Serialize (from Object to JsonString)
var obj = new
{
    Name = "Foo",
    Age = 30,
    Address = new
    {
        Country = "Japan",
        City = "Tokyo"
    },
    Like = new[] { "Microsoft", "Xbox" }
};
// {"Name":"Foo","Age":30,"Address":{"Country":"Japan","City":"Tokyo"},"Like":["Microsoft","Xbox"]}
var jsonStringFromObj = DynamicJson.Serialize(obj);

// [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]
var foobar = new FooBar[] { 
        new FooBar { foo = "fooooo!", bar = 1000 }, 
        new FooBar { foo = "orz", bar = 10 } };
var jsonFoobar = DynamicJson.Serialize(foobar);


// Notice: CornerCase

var nestJson = DynamicJson.Parse(@"{""tes"":10,""nest"":{""a"":0}");

nestJson.nest(); // This equals json.IsDefined("nest")
nestJson.nest("a"); // This equals json.nest.Delete("a")

// if name is C#'s reserved word then put prefix "@"
var json = DynamicJson.Parse(@"{""int"":10,""event"":null}");
var r1 = json.@int; // 10.0
var r2 = json.@event; // null


// History

2010-05-21 ver 1.2.0.0
Fix - Deserialize(cast) can't convert to dynamic[]
Fix - Deserialize(cast) throw exception if has getonly property

2010-05-06 ver 1.1.0.0
Add - foreach support
Add - Dynamic Shortcut of IsDefined,Delete,Deserialize
Fix - Deserialize
Delete - Length

2010-04-30 ver 1.0.0.0
1st Release