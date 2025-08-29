using System;

namespace Solhigson.Framework.Data;

[AttributeUsage(AttributeTargets.Property)]
public class ReaderCol(string name) : Attribute
{
    public string Name { get; set; } = name;
}