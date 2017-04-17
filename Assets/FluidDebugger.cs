using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDebugger : MonoBehaviour {

    public GameObject cube;
    public int texture_resolution;
    Texture2D target;
    Transform[][] cubes;
    public Material test;
	void Awake () 
    {
        target = new Texture2D(texture_resolution, texture_resolution,TextureFormat.RGBAFloat,false,true);
        target.wrapMode = TextureWrapMode.Clamp;
        cubes = new Transform[texture_resolution][];
        for(int i=0; i< texture_resolution; i++)
        {
            cubes[i] = new Transform[texture_resolution];
            for(int j=0; j< texture_resolution; j++)
            {
                cubes[i][j] = GameObject.Instantiate<GameObject>(cube, new Vector3(i,0,j), Quaternion.identity).transform;
            }
        }
	}

    public void UpdateGrid(RenderTexture source, bool show_R)
    {
        RenderTexture.active = source;
        target.ReadPixels(new Rect(0,0,source.width,source.height),0,0);
        target.Apply();
        for(int i=0; i< texture_resolution; i++)
        {
            for(int j=0; j< texture_resolution; j++)
            {
                Vector3 pos = cubes[i][j].position;
                if(show_R)
                    pos.y = target.GetPixel(i,j).r;
                else
                    pos.y = target.GetPixel(i,j).g;
                cubes[i][j].position = pos;
            }
        }
        //test.SetTexture("_MainTex",target);
    }

}
