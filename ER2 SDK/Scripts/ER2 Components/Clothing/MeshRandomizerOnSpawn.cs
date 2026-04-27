
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MeshRandomizerOnSpawn;

public class MeshRandomizerOnSpawn : MonoBehaviour
{
    public MeshFilter[] meshes;
    public RandomMeshData[] randomAlternatives;

    // Start is called before the first frame update
    void Start()
    {
        Set(-1);
    }

    public void Set(int index=-1)
    {
        if (randomAlternatives!=null && randomAlternatives.Length>1 && meshes != null)
        {
            if (index == -1)
                index = Random.Range(0, randomAlternatives.Length);
            else
                index = Mathf.Clamp(index, 0, randomAlternatives.Length);

            RandomMeshData rmd = randomAlternatives[index];
            if (rmd==null) return;

            for (int i=0; i< meshes.Length && i < rmd.alternative_meshes.Length ; i++)
            {
                MeshFilter mf = meshes[i];
                if (!mf) continue;

                mf.sharedMesh = rmd.alternative_meshes[i];
            }
        }
    }

    [System.Serializable]
    public class RandomMeshData
    {
        public Mesh[] alternative_meshes;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(MeshRandomizerOnSpawn))]
public class MeshRandomizerOnSpawnEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshRandomizerOnSpawn myScript = (MeshRandomizerOnSpawn)target;

        if (myScript.meshes!=null && myScript.meshes.Length > 0 && myScript.randomAlternatives != null && myScript.randomAlternatives.Length == 0 && GUILayout.Button("Setup"))
        {
            List<Mesh> base_meshes = new List<Mesh> ();
            foreach (MeshFilter mf in myScript.meshes)
                base_meshes.Add(mf.sharedMesh);
            myScript.randomAlternatives = new RandomMeshData[]
            {
                new MeshRandomizerOnSpawn.RandomMeshData()
                {
                    alternative_meshes = base_meshes.ToArray()
                }
            };
        }

        if (myScript.randomAlternatives != null && myScript.randomAlternatives.Length>0)
        {
            for (int i = 0; i < myScript.randomAlternatives.Length; i++)
            {
                if (GUILayout.Button("Set " + i))
                {
                    myScript.Set(i);
                }
            }
        }
    }
}
#endif
