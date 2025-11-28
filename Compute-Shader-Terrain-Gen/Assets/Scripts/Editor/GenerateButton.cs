using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class MapGeneratoreditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        TerrainGenerator mapgen = (TerrainGenerator)target;


        if (DrawDefaultInspector() && mapgen.autoupdate)
            mapgen.DrawMapInEditor();

        if (GUILayout.Button("Generate"))
        {
            mapgen.DrawMapInEditor();
        }
    }
}
