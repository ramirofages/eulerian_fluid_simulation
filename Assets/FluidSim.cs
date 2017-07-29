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

    int add_temperature_kernel;
    int calculate_buoyancy_kernel;


    public Material density_material;
    RenderTexture density_tex_0;
    RenderTexture density_tex_1;

    RenderTexture velocity_tex_0;
    RenderTexture velocity_tex_1;

    RenderTexture pressure_tex_0;
    RenderTexture pressure_tex_1;

    RenderTexture divergence_tex;

    RenderTexture temperature_tex_0;
    RenderTexture temperature_tex_1;

    RenderTexture debug_texture;

    int texture_resolution = 32;
    int dispatch_group_count = 1;

    FluidDebugger debugger;
    void Start()
    {
        SetupDensityTex();
        SetupTemperatureTex();
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


    }


    void SolvePressure()
    {

        pressure_compute.SetTexture(pressure_kernel,"divergenceR", divergence_tex);

        for(int i=0; i< 40; i++)
        {
            pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_0);
            pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_1);
            pressure_compute.Dispatch(pressure_kernel, dispatch_group_count, dispatch_group_count,1);
            Swap(ref pressure_tex_0, ref pressure_tex_1);
        }

    }

    void GradientSubstract()
    {

        gradient_substract_compute.SetTexture(gradient_substract_kernel, "source_velocity", velocity_tex_0);
        gradient_substract_compute.SetTexture(gradient_substract_kernel, "velocityRW", velocity_tex_1);


        gradient_substract_compute.SetTexture(gradient_substract_kernel, "pressureR", pressure_tex_0);
        gradient_substract_compute.Dispatch(gradient_substract_kernel,dispatch_group_count,dispatch_group_count,1);

        Swap(ref velocity_tex_0, ref velocity_tex_1);

    }

    void AdvectVelocity()
    {
        advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
        advect_compute.SetTexture(advect_kernel,"source", velocity_tex_0);
        advect_compute.SetTexture(advect_kernel,"target", velocity_tex_1);

        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,dispatch_group_count,dispatch_group_count,1);
        Swap(ref velocity_tex_0, ref velocity_tex_1);
    }

    void CalculateDivergence()
    {

        calculate_divergence.SetTexture(calculate_divergence_kernel, "velocityR", velocity_tex_0);

        calculate_divergence.SetTexture(calculate_divergence_kernel, "pressureW", pressure_tex_0);
        calculate_divergence.SetTexture(calculate_divergence_kernel, "divergenceW", divergence_tex);

        calculate_divergence.Dispatch(calculate_divergence_kernel,dispatch_group_count,dispatch_group_count,1);
    }

    void AddForces(){}



    void Update()
    {
        AdvectVelocity();
        AddForces();
        if(Input.GetButtonDown("Jump"))
        {
            inject_compute.SetTexture(inject_velocity_kernel,"textureRW", velocity_tex_0);
            inject_compute.Dispatch(inject_velocity_kernel,dispatch_group_count,dispatch_group_count,1);
        }

        CalculateDivergence();
        SolvePressure();
        GradientSubstract();

//        debugger.UpdateGrid(velocity_tex_0,true);
//        density_material.SetTexture("_Density", velocity_tex_0);

        AddTemperature();
        debugger.UpdateGrid(temperature_tex_0,true);
       
        debug_texture = velocity_tex_0;

    }


    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0,0,200,200), debug_texture, ScaleMode.ScaleToFit, false);
    }
    void AddTemperature()
    {
        inject_compute.SetFloat("dt", Time.deltaTime);
        inject_compute.SetTexture(add_temperature_kernel,"textureRW", temperature_tex_0);
        inject_compute.Dispatch(add_temperature_kernel,dispatch_group_count,dispatch_group_count,1);
    }
    void Swap(ref RenderTexture read,ref RenderTexture written)
    {
        RenderTexture tmp = written;
        written = read;
        read = tmp;
    }
    void SetupKernels()
    {
        inject_density_kernel   = inject_compute.FindKernel("inject_density");
        inject_velocity_kernel  = inject_compute.FindKernel("inject_velocity");
        advect_kernel           = advect_compute.FindKernel("CSMain");
        pressure_kernel         = pressure_compute.FindKernel("CSMain");
        add_density_kernel      = inject_compute.FindKernel("add_density");
        add_velocity_kernel     = inject_compute.FindKernel("add_velocity");
        add_temperature_kernel  = inject_compute.FindKernel("add_temperature");
        calculate_divergence_kernel = calculate_divergence.FindKernel("CSMain");
        gradient_substract_kernel = gradient_substract_compute.FindKernel("CSMain");

    }

//    void SwapTemperatureTextures()
//    {
//
//    }

//    void AdvectDensity()
//    {
//
//        if(using_density_0)
//        {
//            advect_compute.SetTexture(advect_kernel,"source", density_tex_0);
//            advect_compute.SetTexture(advect_kernel,"target", density_tex_1);
//            density_material.SetTexture("_Density",density_tex_1);
//
//        }
//        else
//        {
//            advect_compute.SetTexture(advect_kernel,"source", density_tex_1);
//            advect_compute.SetTexture(advect_kernel,"target", density_tex_0);
//            density_material.SetTexture("_Density",density_tex_0);
//        }
//
//        if(using_velocity_0)
//            density_material.SetTexture("_Density", velocity_tex_0);
//        else
//            density_material.SetTexture("_Density", velocity_tex_1);
//        //
//        //        if(using_pressure_0)
//        //            density_material.SetTexture("_Density", pressure_tex_0);
//        //        else
//        //            density_material.SetTexture("_Density", pressure_tex_1);
//
//
//        if(using_velocity_0)
//            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
//        else
//            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_1);
//
//
//        using_density_0 = !using_density_0;
//        advect_compute.SetFloat("delta_time",Time.deltaTime);
//        advect_compute.Dispatch(advect_kernel,dispatch_group_count,dispatch_group_count,1);
//
//    }
    void AddDensity()
    {
        Vector2 mouse_pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouse_pos.y /= (float)Screen.height;
        mouse_pos.x /= (float)Screen.width;

        if(Input.GetMouseButton(0))
        {

            inject_compute.SetTexture(add_density_kernel,"textureRW", density_tex_0);


            inject_compute.SetVector("mouse_pos", mouse_pos);
            inject_compute.SetFloat("dt", Time.deltaTime);
            inject_compute.Dispatch(add_density_kernel,dispatch_group_count,dispatch_group_count,1);

            inject_compute.SetTexture(add_velocity_kernel,"textureRW", velocity_tex_0);


            inject_compute.Dispatch(add_velocity_kernel,dispatch_group_count,dispatch_group_count,1);

        }
        inject_compute.SetVector("old_mouse_pos", mouse_pos);

    }

    void SetupDensityTex()
    {
        density_tex_0 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        density_tex_1 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        density_tex_0.wrapMode = TextureWrapMode.Clamp;
        density_tex_1.wrapMode = TextureWrapMode.Clamp;
        density_tex_0.enableRandomWrite = true;
        density_tex_1.enableRandomWrite = true;

        density_tex_0.Create();
        density_tex_1.Create();


    }
    void SetupTemperatureTex()
    {
        temperature_tex_0 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        temperature_tex_1 = new RenderTexture(texture_resolution,texture_resolution,0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        temperature_tex_0.wrapMode = TextureWrapMode.Clamp;
        temperature_tex_1.wrapMode = TextureWrapMode.Clamp;
        temperature_tex_0.enableRandomWrite = true;
        temperature_tex_1.enableRandomWrite = true;

        temperature_tex_0.Create();
        temperature_tex_1.Create();
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
        temperature_tex_0.Release();
        temperature_tex_1.Release();
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
