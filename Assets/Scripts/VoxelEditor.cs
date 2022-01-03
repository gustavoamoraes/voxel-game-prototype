using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Voxel))]
public class VoxelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Voxel cubeGen = (Voxel)target;

        if (DrawDefaultInspector())
        {
            if(cubeGen.autoUpdate)
                cubeGen.GenerateCube();
        }

        if (GUILayout.Button("Generate New Cube"))
        {
            cubeGen.GenerateCube();
        }
    }
}
