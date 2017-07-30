#define GRID_SIZE 256.0
#define NUMTHREADS [numthreads(32,32,1)]
#define CELL_SIZE 1.0
#define HALF_CELL_SIZE 0.5
#define VELOCITY_DISSIPATION 0.999
#define GRADIENT_SCALE 1.0


float2 ID_TO_UV(uint2 id)
{
    float2 pos = id;
    pos+= 0.5;
    return (pos / GRID_SIZE);
}

bool is_boundary(uint2 id)
{
    if(id.x < 1 || id.x > GRID_SIZE-1)
        return true;
    if(id.y < 1 || id.y > GRID_SIZE-1)
        return true;

    return false;
}