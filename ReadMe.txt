/*--------------------------------------------------------------------------
* DynamicJson
* ver 1.0.0.0 (Apr. 30th, 2010)
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

// Parse (from JsonString to DynamicJson)
var json = DynamicJson.Parse(@"{""foo"":""json"", ""bar"":100, ""nest"":{ ""foobar"":true } }");

var r1 = json.foo; // "json" - dynamic(string)
var r2 = json.bar; // 100 - dynamic(double)
var r3 = json.nest.foobar; // true - dynamic(bool)
var r4 = json["nest"]["foobar"]; // can access indexer

// Check Defined
var b1 = json.IsDefined("foo"); // true
var b2 = json.IsDefined("foooo"); // false

// Operate
json.Arr = new string[] { "NOR", "XOR" }; // Add
json.foo = 5000; // Replace
json.Delete("bar"); // Delete
            
// Serialize
var reJson = json.ToString(); // {"foo":5000,"nest":{"foobar":true},"Arr":["NOR","XOR"]}

// Access Array
Console.WriteLine(json.Arr[1]); // XOR
foreach (var item in (object[])json.Arr) // please cast object[]
    Console.WriteLine(item); // NOR XOR

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

// Composite
dynamic root = new DynamicJson(); // create root json container
root.obj = new { }; // add blank object
root.obj.str = "aaa";
root.obj.@bool = true; // if propertyName is C#'s Reserved word then put prefix "@"
root.array = new[] { 1, 200 }; // add array
root.obj2 = new { str2 = "bbbb", ar = new object[] { "foobar", null, 100 } }; // add object and init

// {"obj":{"str":"aaa","bool":true},"array":[1,200],"obj2":{"str2":"bbbb","ar":["foobar",null,100]}}
Console.WriteLine(root.ToString()); 


// History

2010-04-30 ver 1.0.0.0
1st Release