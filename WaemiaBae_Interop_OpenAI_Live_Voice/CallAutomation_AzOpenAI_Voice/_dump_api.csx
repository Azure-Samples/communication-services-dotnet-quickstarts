using System;
using System.Reflection;

var asm = typeof(Azure.Communication.Media.MediaConnection).Assembly;
foreach (var t in asm.GetExportedTypes())
{
    if (t.Namespace != "Azure.Communication.Media") continue;
    Console.WriteLine($"TYPE: {t.Name}");
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        Console.WriteLine($"  M: {m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        Console.WriteLine($"  P: {p.PropertyType.Name} {p.Name}");
    foreach (var e in t.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        Console.WriteLine($"  E: {e.Name}");
}
