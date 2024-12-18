#ifndef PHOTOSHOP_BLENDMODES_INCLUDED
#define PHOTOSHOP_BLENDMODES_INCLUDED
 
//
// Ported from https://www.shadertoy.com/view/XdS3RW
//
// Original License:
//
// Creative Commons CC0 1.0 Universal (CC-0) 
//
// 25 of the layer blending modes from Photoshop.
//
// The ones I couldn't figure out are from Nvidia's advanced blend equations extension spec -
// http://www.opengl.org/registry/specs/NV/blend_equation_advanced.txt
// 
// ~bj.2013
//
 
// Helpers
  
/** @private */
float pinLight(float s, float d)
{
    return (2.0*s - 1.0 > d) ? 2.0*s - 1.0 : (s < 0.5 * d) ? 2.0*s : d;
}
 
/** @private */
float vividLight(float s, float d)
{
    return (s < 0.5) ? 1.0 - (1.0 - d) / (2.0 * s) : d / (2.0 * (1.0 - s));
}
 
/** @private */
float hardLight(float s, float d)
{
    return (s < 0.5) ? 2.0*s*d : 1.0 - 2.0*(1.0 - s)*(1.0 - d);
}
 
/** @private */
float softLight(float s, float d)
{
    return (s < 0.5) ? d - (1.0 - 2.0*s)*d*(1.0 - d) 
                : (d < 0.25) ? d + (2.0*s - 1.0)*d*((16.0*d - 12.0)*d + 3.0) 
                : d + (2.0*s - 1.0) * (sqrt(d) - d);
}
 
/** @private */
float overlay( float s, float d )
{
    return (d < 0.5) ? 2.0*s*d : 1.0 - 2.0*(1.0 - s)*(1.0 - d);
}
 
//    rgb<-->hsv functions by Sam Hocevar
//    http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
/** @private */
float3 rgb2hsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
 
/** @private */
float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
 
// Public API Blend Modes
 
float3 ColorBurn(float3 s, float3 d)
{
    return 1.0 - (1.0 - d) / s;
}
 
float3 LinearBurn(float3 s, float3 d )
{
    return s + d - 1.0;
}
 
float3 DarkerColor(float3 s, float3 d)
{
    return (s.x + s.y + s.z < d.x + d.y + d.z) ? s : d;
}
 
float3 Lighten(float3 s, float3 d)
{
    return max(s, d);
}
 
float3 Screen(float3 s, float3 d)
{
    return s + d - s * d;
}
 
float3 ColorDodge(float3 s, float3 d)
{
    return d / (1.0 - s);
}
 
float3 LinearDodge(float3 s, float3 d)
{
    return s + d;
}
 
float3 LighterColor(float3 s, float3 d)
{
    return (s.x + s.y + s.z > d.x + d.y + d.z) ? s : d;
}
 
float3 Overlay(float3 s, float3 d)
{
    float3 c;
    c.x = overlay(s.x, d.x);
    c.y = overlay(s.y, d.y);
    c.z = overlay(s.z, d.z);
    return c;
}
 
float3 DoSoftLight(float3 s, float3 d)
{
    float3 c;
    c.x = softLight(s.x, d.x);
    c.y = softLight(s.y, d.y);
    c.z = softLight(s.z, d.z);
    return c;
}
 
float3 HardLight(float3 s, float3 d)
{
    float3 c;
    c.x = hardLight(s.x, d.x);
    c.y = hardLight(s.y, d.y);
    c.z = hardLight(s.z, d.z);
    return c;
}
 
float3 VividLight(float3 s, float3 d)
{
    float3 c;
    c.x = vividLight(s.x, d.x);
    c.y = vividLight(s.y, d.y);
    c.z = vividLight(s.z, d.z);
    return c;
}
 
float3 LinearLight(float3 s, float3 d)
{
    return 2.0*s + d - 1.0;
}
 
float3 PinLight(float3 s, float3 d)
{
    float3 c;
    c.x = pinLight(s.x, d.x);
    c.y = pinLight(s.y, d.y);
    c.z = pinLight(s.z, d.z);
    return c;
}
 
float3 HardMix(float3 s, float3 d)
{
    return floor(s+d);
}
 
float3 Difference(float3 s, float3 d)
{
    return abs(d-s);
}
 
float3 Exclusion(float3 s, float3 d)
{
    return s + d - 2.0*s*d;
}
 
float3 Subtract(float3 s, float3 d)
{
    return s-d;
}
 
float3 Divide(float3 s, float3 d)
{
    return s/d;
}
 
float3 Add(float3 s, float3 d)
{
    return s+d;
}
 
float3 Hue(float3 s, float3 d)
{
    d = rgb2hsv(d);
    d.x = rgb2hsv(s).x;
    return hsv2rgb(d);
}
 
float3 Color(float3 s, float3 d)
{
    s = rgb2hsv(s);
    s.z = rgb2hsv(d).z;
    return hsv2rgb(s);
}
 
float3 Saturation(float3 s, float3 d)
{
    d = rgb2hsv(d);
    d.y = rgb2hsv(s).y;
    return hsv2rgb(d);
}
 
void Luminosity_float(float3 s, float3 d, out float3 c)
{
    const float3 l = float3(0.3, 0.59, 0.11);
    float dLum = dot(d, l);
    float sLum = dot(s, l);
    float lum = sLum - dLum;
    // c = dLum;
    c = d + lum;
    // float minC = min(min(c.x, c.y), c.z);
    // float maxC = max(max(c.x, c.y), c.z);

    // if(minC < 0.0)
    //     c = sLum + ((c - sLum) * sLum) / (sLum - minC);
    // else if(maxC > 1.0)
    //     c = sLum + ((c - sLum) * (1.0 - sLum)) / (maxC - sLum);
    // else return c;
}
 
#endif // PHOTOSHOP_BLENDMODES_INCLUDED