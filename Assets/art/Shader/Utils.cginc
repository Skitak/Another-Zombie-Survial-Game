float invLerp(float from, float to, float value){
  return (value - from) / (to - from);
}

void Vignette_float(float2 uv, float scale,float hardness, out float value)
{
    uv -= .5f;
    value = length(uv)*scale;
    
    // hardness
    value = clamp(invLerp(hardness,1,value),0,1);
}

void Grayscale_float(float3 color, out float value)
{
    value = (color.x + color.y + color.z) / 3;
}

void RGBtoCMYK_float(float3 rgb, out float4 cmyk)
{
    float k = 1.0 - max(rgb.r, max(rgb.g, rgb.b));
    float c = (1.0 - rgb.r - k) / (1.0 - k);
    float m = (1.0 - rgb.g - k) / (1.0 - k);
    float y = (1.0 - rgb.b - k) / (1.0 - k);
    cmyk = float4(saturate(c), saturate(m), saturate(y), saturate(k));
}

// void RGBtoCMYK_float (float3 rgb, out float4 cmyk) {
//     float r = rgb.r;
//     float g = rgb.g;
//     float b = rgb.b;
//     float k = min(1.0 - r, min(1.0 - g, 1.0 - b));
//     float3 cmy = 0;
//     float invK = 1.0 - k;
//     if (invK != 0.0) {
//         cmy.x = (1.0 - r - k) / invK;
//         cmy.y = (1.0 - g - k) / invK;
//         cmy.z = (1.0 - b - k) / invK;
//     }
//     cmyk = clamp(float4(cmy, k), 0.0, 1.0);
// }