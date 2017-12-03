using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDebugger : MonoBehaviour {

    public GameObject cube;
    public int texture_resolution;
    Texture2D target;
    Transform[][] cubes;
    public Material test;
    public bool is_disabled;
    MaterialPropertyBlock prop;
	void Awake () 
    {
        if(is_disabled) return;

        target = new Texture2D(texture_resolution, texture_resolution,TextureFormat.RGBAFloat,false,true);
        target.wrapMode = TextureWrapMode.Clamp;
        cubes = new Transform[texture_resolution][];
        prop = new MaterialPropertyBlock();
        prop.SetVector("_GridResolution", new Vector2(texture_resolution, texture_resolution));

        for(int i=0; i< texture_resolution; i++)
        {
            cubes[i] = new Transform[texture_resolution];
            for(int j=0; j< texture_resolution; j++)
            {
                prop.SetVector("_Index", new Vector2(i,j));
                cubes[i][j] = GameObject.Instantiate<GameObject>(cube, new Vector3(i,0,j), Quaternion.identity).transform;
                cubes[i][j].GetComponent<MeshRenderer>().SetPropertyBlock(prop);
            }
        }
	}

    public void UpdateGrid(RenderTexture source)
    {
        if(is_disabled) return;
        prop.SetTexture("_MainTex",source);
        for(int i=0; i< texture_resolution; i++)
        {
            for(int j=0; j< texture_resolution; j++)
            {
                prop.SetVector("_Index", new Vector2(i,j));
                cubes[i][j].GetComponent<MeshRenderer>().SetPropertyBlock(prop);
            }
        }
    }

}
