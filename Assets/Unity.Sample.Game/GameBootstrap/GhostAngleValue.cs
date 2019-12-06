using System;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Unity.NetCode;
#endif

public class GhostAngleValueAttribute : Attribute
{
}

#if UNITY_EDITOR
class GhostSnapshotValueAngle : GhostSnapshotValue
{
    public override void AddImports(HashSet<string> imports)
    {
        imports.Add("UnityEngine");
    }

    public override bool SupportsQuantization => true;

    public override bool CanProcess(FieldInfo field, string componentName, string fieldName)
    {
        return field.FieldType == typeof(float) && field.GetCustomAttribute<GhostAngleValueAttribute>() != null;
    }
    public override bool CanProcess(Type type, string componentName, string fieldName)
    {
        throw new NotImplementedException();
    }

    public override string GetTemplatePath(int quantization)
    {
        if (quantization < 1)
            throw new NotImplementedException("Unquantized angle values not supported");
        return "Assets/Scripts/Networking/GhostSnapshotValueAngle.txt";
    }
}
#endif
