using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Jacobi : MonoBehaviour {

    int max_iterations = 1000000;
    float tolerance = 1e-3f;

    List<List<float>> line;
    float[] rhs;

    public Text text;

//    [Range(0f,1f)]
//    public float t;
    float t=0;
    void initialize(float[] arreglo)
    {
        for(int i=0; i< arreglo.Length;i++)
        {
            arreglo[i] = 0f;
        }
    }
    void copy(float[] a, float[] b)
    {
        for(int i=0; i< a.Length; i++)
        {
            a[i] = b[i];
        }
    }

    float difference(float[] a, float[] b)
    {
        float d = 0f;
        for(int i=0; i< a.Length; i++)
        {
            d+= Mathf.Abs(a[i] - b[i]);
        }
        return d;
    }

    List<List<float>> iterate(float[] a, float[] b, float[] c)
    {
        initialize(a); 
        initialize(c);
        string total = "";
        List<List<float>> result = new List<List<float>>();


        for(int k=0; k<max_iterations; k++)
        {
//            print("iteracion "+ k);
            string s = "";
            List<float> itt = new List<float>();
            for(int i=1; i<= a.Length-2; i++)
            {
                a[i] = (rhs[i] - b[i-1] - c[i+1]) * (-0.5f);
                itt.Add(Mathf.Abs(a[i]));
                s+= " "+ Mathf.Abs(a[i]);
            }
            total+="\n"+s;
            result.Add(itt);
            total+="\n";
            float err = difference(a,c);
            copy(c,a);
            if(err < tolerance) 
            {
                break;
            }
        }
        print(total);
        return result;

    }

    [ContextMenu("asd")]
    void Start()
    {
        rhs = new float[]{ 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
                            0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f};
        float[] unknown, known;
        unknown = new float[16];
        known = new float[16];
        print("jacobi");
        line = iterate(unknown, known, known);

    }

    void Update()
    {
        t+= Time.deltaTime/6f;
        t = Mathf.Clamp01(t);
        int segment = (int)((float)line.Count * t);
        if(segment == line.Count)
            segment--;

        text.text = "Iteracion: " + (int)(54f * t);
        for(int i=1; i < rhs.Length; i++)
        {
            Debug.DrawLine(new Vector3(i-1,0,0), new Vector3(i-1, rhs[i]*2.5f + 0.1f, 0), Color.green); 
        }
        for(int i=1; i< line[segment].Count -1; i++)
        {
            Vector3 from = new Vector3(i, line[segment][i] , 0);
            Vector3 to = new Vector3(i+1, line[segment][i+1], 0);
            Debug.DrawLine(from, to, Color.red); 
        }

    }
}
