# Harbour water authoring

Integration: disable only the old `CityMesh_130` renderer and call
`BraxCityWaterBuild.Apply(cityRoot)` after the city's static lighting bake and
before saving the generated scene. Keep the original glTF, material and texture.

The original extent is Unity X 84..884, Z -340..460, mean Y -1.7 m. The generator
retains that footprint. Eight small patches near the quay use a two-metre grid;
three flat patches cover the remainder. Total generated geometry is 11 renderers,
4,500 vertices and 8,198 triangles before any build-time processing. Their shared
material has one opaque pass, five analytic wave components, one soft sky-cubemap
sample and no grab/depth texture. No camera, light, probe, collider or runtime
component is created. These counts are source design values, not device timings.

The mean water height stays -1.7 m. Two broad waves have a combined maximum
displacement of 0.034 m. Their displacement fades to zero at the quay and at all
outer edges of the moving mesh. Three smaller wave slopes animate the reflected
sky; pixel derivatives and distance attenuate unresolved frequencies. Full
precision is retained for world coordinates, time, phases and derivatives.

The shader samples the existing default sky reflection with an explicit soft mip.
It introduces no reflection capture. If the bound cubemap is black, it uses an
explicit sky-gradient approximation from the current scene's ambient colours.
It does not produce reflections of local buildings, refraction or underwater
visibility. This opaque treatment is intended for deep, turbid harbour water.
The custom shader is referenced from the generated material outside Editor;
the durable `.shader.txt` source remains beside this generator.

## Assets and review status

No suitable water normal map was verified in the available asset audit. Normals
are computed from the authored wave equations; masonry, asphalt and timber
normals are not reused. The external Water 001 candidate was not downloaded or
verified as a usable file and is not a dependency of this module.

The generator validates imported shader errors, triangle winding, mean level,
coverage and absence of extra render cameras/lights/probes/colliders. This does
not replace WebGL shader compilation, visual review while moving, or a physical
phone performance test. Review the quay, pier edge and water/sky horizon in a real
Unity build. Check long-session motion precision, foreground occlusion, seams at
patch boundaries and day-time reflection intensity before accepting its look.

## Implementation references

- Unity 6 Built-In reflection example:
  https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/shaders/writing-shaders-birp/built-in-shader-examples/reflections
- Unity 6 reflection-probe usage (`Off` still uses the default sky):
  https://docs.unity3d.com/cn/6000.0/ScriptReference/Rendering.ReflectionProbeUsage.html
- Unity 6 shader import diagnostics:
  https://docs.unity3d.com/cn/6000.0/ScriptReference/ShaderUtil.ShaderHasError.html
- Microsoft HLSL `fwidth` reference (pixel derivatives):
  https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-fwidth
