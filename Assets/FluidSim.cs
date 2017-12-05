using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim : MonoBehaviour {

    public ComputeShader inject_compute;
    public ComputeShader advect_compute;
    public ComputeShader pressure_compute;
    public ComputeShader calculate_divergence;
    public ComputeShader gradient_substract_compute;
    public ComputeShader force_addition;

    public Texture2D force_field_texture;

    public float ambient_temp = 0.5f;
    public float mass_density = 1f;
    public float temp_scale = 1f;

    [Range(0f,10f)]
    public float temperature_amount = 0.2f;

    [Range(0f,10f)]
    public float density_amount = 3f;


    int add_density_kernel;
    int advect_kernel;
    int pressure_kernel;
    int calculate_divergence_kernel;
    int gradient_substract_kernel;
    int add_velocity_kernel;

    int force_addition_kernel;

    int add_temperature_kernel;
    int calculate_buoyancy_kernel;


    public Material density_material;
    public MeshRenderer visualize_density_renderer;

    RenderTexture density_tex_0;
    RenderTexture density_tex_1;

    RenderTexture velocity_tex_0;
    RenderTexture velocity_tex_1;

    RenderTexture pressure_tex_0;
    RenderTexture pressure_tex_1;

    RenderTexture divergence_tex;

    RenderTexture temperature_tex_0;
    RenderTexture temperature_tex_1;


    int texture_resolution = 256;
    int dispatch_group_count;

    FluidDebugger debugger;



    void Start()
    {
        dispatch_group_count = texture_resolution / 32;
        SetupDensityTex();
        SetupTemperatureTex();
        SetupVelocityTex();
        SetupPressureTex();
        SetupKernels();
        //AddDensity(); 

        debugger = GetComponent<FluidDebugger>();
    }


    void SolvePressure()
    {

        pressure_compute.SetTexture(pressure_kernel,"divergenceR", divergence_tex);

        for(int i=0; i< 50; i++)
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
        
    void Advect(ref RenderTexture source,ref RenderTexture target)
    {
        advect_compute.SetTexture(advect_kernel,"velocityR", velocity_tex_0);
        advect_compute.SetTexture(advect_kernel,"source", source);
        advect_compute.SetTexture(advect_kernel,"target", target);

        advect_compute.SetFloat("delta_time",Time.deltaTime);
        advect_compute.Dispatch(advect_kernel,dispatch_group_count,dispatch_group_count,1);
        Swap(ref source, ref target);
    }

    void CalculateDivergence()
    {

        calculate_divergence.SetTexture(calculate_divergence_kernel, "velocityR", velocity_tex_0);

        calculate_divergence.SetTexture(calculate_divergence_kernel, "pressureW", pressure_tex_0);
        calculate_divergence.SetTexture(calculate_divergence_kernel, "divergenceW", divergence_tex);

        calculate_divergence.Dispatch(calculate_divergence_kernel,dispatch_group_count,dispatch_group_count,1);
    }

    void AddDensity()
    {
        inject_compute.SetTexture(add_density_kernel,"textureRW", density_tex_0);
        inject_compute.SetFloat("dt", Time.deltaTime);
        inject_compute.SetFloat("density_amount", density_amount);
        inject_compute.Dispatch(add_density_kernel,dispatch_group_count,dispatch_group_count,1);
    }

    void AddForces()
    {

        AddTemperature();
        //AddDensity();
        AddDensity_mouse(); // usamos el mouse para agregar

        force_addition.SetFloat("density_mass", mass_density);
        force_addition.SetFloat("ambient_temperature", ambient_temp);
        force_addition.SetFloat("temperature_amount", temperature_amount);
        force_addition.SetFloat("temperature_scale", temp_scale);
        force_addition.SetFloat("dt", Time.deltaTime);
        force_addition.SetTexture(force_addition_kernel, "velocityW", velocity_tex_0);
        force_addition.SetTexture(force_addition_kernel, "temperatureR", temperature_tex_0);
        force_addition.SetTexture(force_addition_kernel, "densityR", density_tex_0);
        force_addition.Dispatch(force_addition_kernel, dispatch_group_count,dispatch_group_count,1);
    }



    void Update()
    {

        Advect(ref density_tex_0, ref density_tex_1);
        Advect(ref temperature_tex_0, ref temperature_tex_1);
        Advect(ref velocity_tex_0, ref velocity_tex_1);

        AddForces();

        CalculateDivergence();
        SolvePressure();
        GradientSubstract();

        Visualize(density_tex_0);

        debugger.UpdateGrid(pressure_tex_0);

    }

    void Visualize(RenderTexture tx)
    {
        visualize_density_renderer.material.SetTexture("_MainTex", tx);
    }


    void AddTemperature()
    {
        inject_compute.SetFloat("dt", Time.deltaTime);
        inject_compute.SetFloat("temperature_amount", temperature_amount);

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

        advect_kernel           = advect_compute.FindKernel("CSMain");
        pressure_kernel         = pressure_compute.FindKernel("CSMain");
        add_density_kernel      = inject_compute.FindKernel("add_mouse_density");
        add_velocity_kernel     = inject_compute.FindKernel("add_velocity");
        add_temperature_kernel  = inject_compute.FindKernel("add_temperature");
        calculate_divergence_kernel = calculate_divergence.FindKernel("CSMain");
        gradient_substract_kernel = gradient_substract_compute.FindKernel("CSMain");
        force_addition_kernel   = force_addition.FindKernel("add_force");
    }

    void AddDensity_mouse()
    {
        Vector2 mouse_pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouse_pos.y /= (float)Screen.height;
        mouse_pos.x /= (float)Screen.width;

        if(Input.GetMouseButton(0) || true)
        {
            inject_compute.SetTexture(add_density_kernel,"textureRW", density_tex_0);
            inject_compute.SetVector("mouse_pos", mouse_pos);
            inject_compute.SetFloat("density_amount", density_amount);

            inject_compute.SetFloat("dt", Time.deltaTime);
            inject_compute.Dispatch(add_density_kernel,dispatch_group_count,dispatch_group_count,1);
        }

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

    public bool debug_ui;
    public bool visualize_quad;
    bool visualize_vel;
    bool visualize_temp;
    bool visualize_density;
    void OnGUI()
    {
        //debug_ui = GUI.Toggle(new Rect(0, Screen.height-50,100,50),debug_ui, "Display UI");
        visualize_quad = GUI.Toggle(new Rect(0, Screen.height-80,200,50),visualize_quad, "Display quad");
        visualize_vel = GUI.Toggle(new Rect(0, Screen.height-110,200,50),visualize_vel, "Display velocity");
        visualize_temp = GUI.Toggle(new Rect(0, Screen.height-140,200,50),visualize_temp, "Display temperature");
        visualize_density = GUI.Toggle(new Rect(0, Screen.height-170,200,50),visualize_density, "Display density");

        visualize_density_renderer.enabled = visualize_quad;


        if(visualize_density)
        {
            GUI.DrawTexture(new Rect(0,0,400,400), density_tex_0, ScaleMode.ScaleToFit, false);
            GUI.Label(new Rect(180,420, 150,50), "     Densidad\n(mover el mouse)");
        }
        if(visualize_vel)
        {
            GUI.DrawTexture(new Rect(400,0,400,400), velocity_tex_0, ScaleMode.ScaleToFit, false);
            GUI.Label(new Rect(580,420, 150,50), "Velocidad");
        }
        if(visualize_temp)
        {
            GUI.DrawTexture(new Rect(800,0,400,400), temperature_tex_0, ScaleMode.ScaleToFit, false);
            GUI.Label(new Rect(980,420, 150,50), "Temperatura");
        }



    }

}
