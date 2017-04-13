using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim : MonoBehaviour {

    public ComputeShader inject_compute;
    public ComputeShader advect_compute;
    public ComputeShader pressure_compute;
    public ComputeShader calculate_divergence;

    int inject_density_kernel;
    int inject_velocity_kernel;
    int advect_kernel;
    int pressure_kernel;
    int calculate_divergence_kernel;


    public Material density_material;
    RenderTexture density_tex_0;
    RenderTexture density_tex_1;

    RenderTexture velocity_tex_0;
    RenderTexture velocity_tex_1;

    RenderTexture pressure_tex_0;
    RenderTexture pressure_tex_1;

    bool using_density_0;
    void Start()
    {
        SetupDensityTex();
        SetupVelocityTex();
        SetupPressureTex();
        SetupKernels();


        inject_compute.SetTexture(inject_density_kernel,"textureRW", density_tex_0);
        inject_compute.SetTexture(inject_velocity_kernel,"textureRW", velocity_tex_0);
 
        inject_compute.Dispatch(inject_density_kernel,16,16,1);
        inject_compute.Dispatch(inject_velocity_kernel,16,16,1);

        advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
        advect_compute.SetTexture(advect_kernel,"source", density_tex_0);
        advect_compute.SetTexture(advect_kernel,"target", density_tex_1);
        advect_compute.SetFloat("delta_time",Time.deltaTime);
        density_material.SetTexture("_Density",density_tex_1);

        advect_compute.Dispatch(advect_kernel,16,16,1);

//        calculate_divergence.SetTexture(calculate_divergence_kernel, "velocityR", velocity_tex_1);
//        calculate_divergence.SetTexture(calculate_divergence_kernel, "pressureW", pressure_tex_0);
//        calculate_divergence.Dispatch(calculate_divergence_kernel,16,16,1);
//
        pressure_compute.SetTexture(pressure_kernel,"pressureR", pressure_tex_0);
        pressure_compute.SetTexture(pressure_kernel,"pressureW", pressure_tex_1);
        //SolvePressure();

        using_density_0 = false;
        using_velocity_0 = true;
    }

    bool using_velocity_0;

    void SolvePressure()
    {
        bool contents_on_pressure_0 = true;

        for(int i=0; i< 30; i++)
        {
            pressure_compute.Dispatch(calculate_divergence_kernel,16,16,1);
            contents_on_pressure_0 = !contents_on_pressure_0;
            SwapPressure(contents_on_pressure_0);
        }

    }

    void AdvectVelocity()
    {
        if(using_velocity_0)
        {
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
            advect_compute.SetTexture(advect_kernel,"source", velocity_tex_0);
            advect_compute.SetTexture(advect_kernel,"target", velocity_tex_1);
        }
        else
        {
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_1);
            advect_compute.SetTexture(advect_kernel,"source", velocity_tex_1);
            advect_compute.SetTexture(advect_kernel,"target", velocity_tex_0);
        }
        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,16,16,1);
        using_velocity_0 = !using_velocity_0;
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
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
        else
            advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_1);

        
        using_density_0 = !using_density_0;
        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,16,16,1);

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
    void Update()
    {
        AdvectDensity();

    }
    void SetupKernels()
    {
        inject_density_kernel   = inject_compute.FindKernel("inject_density");
        inject_velocity_kernel  = inject_compute.FindKernel("inject_velocity");
        advect_kernel           = advect_compute.FindKernel("CSMain");
        pressure_kernel         = pressure_compute.FindKernel("CSMain");

        calculate_divergence_kernel = calculate_divergence.FindKernel("CSMain");
    }

    void SwapDensityTextures()
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
        using_density_0 = !using_density_0;
    }

    void SetupDensityTex()
    {
        density_tex_0 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        density_tex_1 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        density_tex_0.wrapMode = TextureWrapMode.Clamp;
        density_tex_1.wrapMode = TextureWrapMode.Clamp;
        density_tex_0.enableRandomWrite = true;
        density_tex_1.enableRandomWrite = true;

        density_tex_0.Create();
        density_tex_1.Create();


    }

    void SetupPressureTex()
    {
        pressure_tex_0 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        pressure_tex_1 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        pressure_tex_0.wrapMode = TextureWrapMode.Clamp;
        pressure_tex_1.wrapMode = TextureWrapMode.Clamp;
        pressure_tex_0.enableRandomWrite = true;
        pressure_tex_1.enableRandomWrite = true;

        pressure_tex_0.Create();
        pressure_tex_1.Create();
    }
    void SetupVelocityTex()
    {
        velocity_tex_0 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        velocity_tex_1 = new RenderTexture(512,512,0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
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
}
