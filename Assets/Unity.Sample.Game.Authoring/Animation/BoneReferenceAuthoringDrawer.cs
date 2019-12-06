using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BoneReferenceAuthoring))]
public class BoneReferenceAuthoringDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
//        if (prop.isExpanded)
//            return 3 * EditorGUIUtility.singleLineHeight;

//        return EditorGUI.GetPropertyHeight(prop);

        return 2 * EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {


//        EditorGUI.BeginProperty(pos, label, prop);


//        var indent = EditorGUI.indentLevel;
//        EditorGUI.indentLevel = 0;
//
//        var labelWidth = EditorGUIUtility.labelWidth;
//        EditorGUIUtility.labelWidth = 30;

//        prop.isExpanded = EditorGUI.Foldout(pos, prop.isExpanded, label);
//        if (prop.isExpanded)
        {
//            var rigPropWidth = pos.width/2;
            var rigRect = new Rect(pos.x, pos.y /*+ EditorGUIUtility.singleLineHeight*/, pos.width, EditorGUIUtility.singleLineHeight);
            var rigLabel = new GUIContent("Rig");
            EditorGUI.PropertyField(rigRect,prop.FindPropertyRelative("RigAsset"),rigLabel);

            var boneRect = new Rect(pos.x, pos.y + EditorGUIUtility.singleLineHeight*1, pos.width , EditorGUIUtility.singleLineHeight);
            var boneLabel = new GUIContent("Bone");
            EditorGUI.PropertyField(boneRect,prop.FindPropertyRelative("BoneName"), boneLabel);

        }


//        EditorGUI.indentLevel = indent;
//        EditorGUIUtility.labelWidth = labelWidth;

  //      EditorGUI.EndProperty();
    }
}
