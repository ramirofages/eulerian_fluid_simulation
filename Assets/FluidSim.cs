using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim : MonoBehaviour {

    public ComputeShader inject_compute;
    public ComputeShader advect_compute;
    public ComputeShader pressure_compute;
    public ComputeShader calculate_divergence;
    public ComputeShader gradient_substract_compute;

    public Texture2D force_field_texture;


    int inject_density_kernel;
    int inject_velocity_kernel;
    int add_density_kernel;
    int advect_kernel;
    int pressure_kernel;
    int calculate_divergence_kernel;
    int gradient_substract_kernel;
    int add_velocity_kernel;

    public Material density_material;
    RenderTexture density_tex_0;
    RenderTexture density_tex_1;

    RenderTexture velocity_tex_0;
    RenderTexture velocity_tex_1;

    RenderTexture pressure_tex_0;
    RenderTexture pressure_tex_1;

    RenderTexture divergence_tex;

    bool using_density_0;
    int texture_resolution = 32;
    int dispatch_group_count = 1;

    FluidDebugger debugger;
    void Start()
    {
        SetupDensityTex();
        SetupVelocityTex();
        SetupPressureTex();
        SetupKernels();


        inject_compute.SetTexture(inject_velocity_kernel,"force_tex", force_field_texture);
 
//        inject_compute.SetTexture(inject_density_kernel,"textureRW", density_tex_0);


//        inject_compute.Dispatch(inject_density_kernel,dispatch_group_count,dispatch_group_count,1);


        debugger = GetComponent<FluidDebugger>();
        //debugger.UpdateGrid(velocity_tex_0, false);



//        CalculateDivergence();
//
//        pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_0);
//        pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_1);
//        SolvePressure();

        using_density_0 = false;
        using_velocity_0 = true;

    }

    bool using_velocity_0;

    bool using_pressure_0 = false;
    void SolvePressure()
    {
        using_pressure_0 = true;
        pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_0);
        pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_1);
        pressure_compute.SetTexture(pressure_kernel,"divergenceR", divergence_tex);

        for(int i=0; i< 10; i++)
        {
            pressure_compute.Dispatch(pressure_kernel, dispatch_group_count, dispatch_group_count,1);
            using_pressure_0 = !using_pressure_0;
            SwapPressure(using_pressure_0);
        }

    }
    void SwapPressure(bool contents_on_pressure_0)
    {
        if(contents_on_pressure_0)
        {
            pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_0);
            pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_1);
        }
        else
        {
            pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_1);
            pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_0);
        }
    }
    void GradientSubstract()
    {
        if(using_velocity_0)
        {
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "source_velocity", velocity_tex_0);
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "velocityRW", velocity_tex_1);

        }
        else
        {
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "source_velocity", velocity_tex_1);
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "velocityRW", velocity_tex_0);

        }

        if(using_pressure_0)
        {
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "pressureR", pressure_tex_0);

        }
        else
        {
            gradient_substract_compute.SetTexture(gradient_substract_kernel, "pressureR", pressure_tex_1);

        }

        gradient_substract_compute.Dispatch(gradient_substract_kernel,dispatch_group_count,dispatch_group_count,1);
        using_velocity_0 = !using_velocity_0;
    }

    void AdvectVelocity()
    {
        if(using_velocity_0)
        {
            //advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
            advect_compute.SetTexture(advect_kernel,"source", velocity_tex_0);
            advect_compute.SetTexture(advect_kernel,"target", velocity_tex_1);
        }
        else
        {
            //advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_1);
            advect_compute.SetTexture(advect_kernel,"source", velocity_tex_1);
            advect_compute.SetTexture(advect_kernel,"target", velocity_tex_0);

        }


        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,dispatch_group_count,dispatch_group_count,1);
        using_velocity_0 = !using_velocity_0;
    }

    void CalculateDivergence()
    {
        if(using_velocity_0)
            calculate_divergence.SetTexture(calculate_divergence_kernel, "velocityR", velocity_tex_0);
        else
            calculate_divergence.SetTexture(calculate_divergence_kernel, "velocityR", velocity_tex_1);


        calculate_divergence.SetTexture(calculate_divergence_kernel, "pressureW", pressure_tex_0);
        calculate_divergence.SetTexture(calculate_divergence_kernel, "divergenceW", divergence_tex);

        using_pressure_0 = true;
        calculate_divergence.Dispatch(calculate_divergence_kernel,dispatch_group_count,dispatch_group_count,1);
    }


    void Update()
    {
        AdvectVelocity();
        if(Input.GetButtonDown("Jump"))
        {
            if(using_velocity_0)
                inject_compute.SetTexture(inject_velocity_kernel,"textureRW", velocity_tex_0);
            else
                inject_compute.SetTexture(inject_velocity_kernel,"textureRW", velocity_tex_1);

            inject_compute.Dispatch(inject_velocity_kernel,dispatch_group_count,dispatch_group_count,1);
        }
//        AdvectDensity();
//        AddDensity();
        CalculateDivergence();
        debugger.UpdateGrid(divergence_tex,true);

        SolvePressure();

//        if(using_pressure_0)
//            debugger.UpdateGrid(pressure_tex_0,true);
//        else
//            debugger.UpdateGrid(pressure_tex_1,true);


        
        GradientSubstract();


//        if(using_velocity_0)
//            debugger.UpdateGrid(velocity_tex_0,false);
//        else
//            debugger.UpdateGrid(velocity_tex_1,false); 
        


        if(using_velocity_0)
        {
            density_material.SetTexture("_Density", velocity_tex_0);
        }
        else
        {
            density_material.SetTexture("_Density", velocity_tex_1);
        }
    }

    void SetupKernels()
    {
        inject_density_kernel   = inject_compute.FindKernel("inject_density");
        inject_velocity_kernel  = inject_compute.FindKernel("inject_velocity");
        advect_kernel           = advect_compute.FindKernel("CSMain");
        pressure_kernel         = pressure_compute.FindKernel("CSMain");
        add_density_kernel      = inject_compute.FindKernel("add_density");
        add_velocity_kernel     = inject_compute.FindKernel("add_velocity");
        calculate_divergence_kernel = calculate_divergence.FindKernel("CSMain");
        gradient_substract_kernel = gradient_substract_compute.FindKernel("CSMain");

    }

    void SwapDensityTextures()
    {
        if(using_density_0)
        {
            advect_compute.SetTexture(advect_kernel,"source", density_tex_0);
            advect_compute.SetTexture(advect_kernel,"target", density_tex_1);

        }
        else
        {
            advect_compute.SetTexture(advect_kernel,"source", density_tex_1);
            advect_compute.SetTexture(advect_kernel,"target", density_tex_0);

        }
        using_density_0 = !using_density_0;
    }
    void AdvectDensity()
    {

        if(using_density_0)
        {
            advect_compute.SetTexture(advect_kernel,"source", density_tex_0);
            advect_compute.SetTexture(advect_kernel,"target", density_tex_1);
            density_material.SetTexture("_Density",density_tex_1);

        }
        else
        {
            advect_compute.SetTexture(advect_kernel,"source", density_tex_1);
            advect_compute.SetTexture(advect_kernel,"target", density_tex_0);
            density_material.SetTexture("_Density",density_tex_0);
        }

        if(using_velocity_0)
            density_material.SetTexture("_Density", velocity_tex_0);
        else
            density_material.SetTexture("_Density", velocity_tex_1);
        //
        //        if(using_pressure_0)
        //            density_material.SetTexture("_Density", pressure_tex_0);
        //        else
        //            density_material.SetTexture("_Density", pressure_tex_1);


        if(using_velocity_0)
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
        else
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_1);


        using_density_0 = !using_density_0;
        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,dispatch_group_count,dispatch_group_count,1);

    }
    void AddDensity()
    {
        Vector2 mouse_pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouse_pos.y /= (float)Screen.height;
        mouse_pos.x /= (float)Screen.width;

        if(Input.GetMouseButton(0))
        {


            if(using_density_0)
                inject_compute.SetTexture(add_density_kernel,"textureRW", density_tex_0);
            else
                inject_compute.SetTexture(add_density_kernel,"textureRW", density_tex_1);

            inject_compute.SetVector("mouse_pos", mouse_pos);
            inject_compute.SetFloat("dt", Time.deltaTime);
            inject_compute.Dispatch(add_density_kernel,dispatch_group_count,dispatch_group_count,1);

            if(using_velocity_0)
                inject_compute.SetTexture(add_velocity_kernel,"textureRW", velocity_tex_0);
            else
                inject_compute.SetTexture(add_velocity_kernel,"textureRW", velocity_tex_1);

            inject_compute.Dispatch(add_velocity_kernel,dispatch_group_count,dispatch_group_count,1);

        }
        inject_compute.SetVector("old_mouse_pos", mouse_pos);

    }

    void SetupDensityTex()
    {
        density_tex_0 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        density_tex_1 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        density_tex_0.wrapMode = TextureWrapMode.Clamp;
        density_tex_1.wrapMode = TextureWrapMode.Clamp;
        density_tex_0.enableRandomWrite = true;
        density_tex_1.enableRandomWrite = true;

        density_tex_0.Create();
        density_tex_1.Create();


    }

    void SetupPressureTex()
    {
        pressure_tex_0 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        pressure_tex_1 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        pressure_tex_0.wrapMode = TextureWrapMode.Clamp;
        pressure_tex_1.wrapMode = TextureWrapMode.Clamp;
        pressure_tex_0.enableRandomWrite = true;
        pressure_tex_1.enableRandomWrite = true;

        pressure_tex_0.Create();
        pressure_tex_1.Create();


        divergence_tex = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        divergence_tex.wrapMode = TextureWrapMode.Clamp;
        divergence_tex.enableRandomWrite = true;
        divergence_tex.Create();

    }
    void SetupVelocityTex()
    {
        velocity_tex_0 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        velocity_tex_1 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        velocity_tex_0.wrapMode = TextureWrapMode.Clamp;
        velocity_tex_1.wrapMode = TextureWrapMode.Clamp;

        velocity_tex_0.enableRandomWrite = true;
        velocity_tex_1.enableRandomWrite = true;

        velocity_tex_0.Create();
        velocity_tex_1.Create();


    }
    void OnDisable()
    {
        density_tex_0.Release();
        density_tex_1.Release();
        velocity_tex_0.Release();
        velocity_tex_1.Release();
        pressure_tex_0.Release();
        pressure_tex_1.Release();

    }

//    void OnGUI()
//    {
//        char[] chars = new char[4];
//
//        for(int i=0; i<32; i++)
//        {
//            for(int j=0; j<32; j++)
//            {
//                string s = (3.1446876).ToString();
//                s.CopyTo(0,chars,0,4);
//                GUI.Label(new Rect(i*35f,j*20f,40f,20f), new string(chars));
//            }
//        }
//
//
//    }


}
