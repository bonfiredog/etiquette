# Oil Painting - Unity3D Image Effect
#### (c) 2018 by [flockaroo](http://www.flockaroo.at) (Florian Berger) - email: <flockaroo@gmail.com>

******

### How to use

Select your camera node and then simply add "OilPaintingEffect" script to camera components (can be found in Assets/flockaroo/OilPainting/).
You can drag/drop it to there or choose it from the menu (Component/Scripts/Flockaroo/OilPainting).

![How to use - Image](/home/floh/floh/projects/unity/Unity_OilPainting/Assets/flockaroo/OilPainting/howto.png){ width="100%" }

__Warning!!__ The subfolder "flockaroo_OilPainting" in "Resources" is needed by the effect script for unique identification of files and should not be removed or renamed.

<div style="page-break-after: always;"></div>

### Parameters

The shader provides the following parameters:

#### Input/Output
 | Parameter       | function
 |-----------------|--------------
 | Input Texture   | take this texture as input instead of the camera
 | Render To Texture | render to texture instead of screen
 | Output Texture  | texture being rendered to if above is checked
 | Output Mipmap   | generate mipmap for output texture

#### Main faders
 | Parameter       | function
 |-----------------|--------------
 | Effect Fade     | 0 = effect image ... 1 = original content
 | Pan Fade        | 0 = effect image ... 1 = original content - pan from left to right

#### Source
 | Parameter       | function
 |-----------------|--------------
 | Src Bright      | adjust brightness of the content before applying the effect
 | Src Contrast    | adjust contrast of the content before applying the effect
 | Src Color       | adjust color intensity of the content before applying the effect
 | Src Blur        | blur the content before applying effect (only works from Unity-5.5 upwards)

<!-- | Src Color       | the color intensity of the effect-->
#### Effect
 | Parameter       | function
 |-----------------|--------------
 | Brush Detail    | detail of the brush strokes
 | Brush Fill      | how much the brush strokes fill the screen area
 | Num Strokes     | number of strokes to be drawn
 | LayerScale      | lower number gives less different scales of strokes
 | Canvas          | roughness of canvas (bumpyness in the brush strokes)
 | Flicker Freq    | controls the frequency of the flickering (flickers/sec)
 | Flicker Strength| makes the stroke position vary from frame to frame
 | Light Ang       | angle of light in screen plane [degrees] 0..360
 | Light Offs      | angle of light to screen plane normal [degrees] 0..90
 | Paint Diff      | diffuse lighting of paint strokes
 | Paint Spec      | specular reflections of paint strokes
 | Paint Shiny     | shininess of paint strokes
 | Color Spread    | adds some color variance between different strokes
 | Screen FOV      | field of view of screen in degrees (affects specular reflection)
 | Stroke Ang      | rotate strokes relative to gradient (e.g. 90 degree gives a very fuzzy look)
 | Stroke Bend     | -1.0 bend towards dark content, +1.0 bend towards bright content (e.g. for clouds)
 | Stroke Dir      | swaps front and back of stroke while maintaining its curve
 | Stroke Contour  | harder contour of strokes - gives a slight color blob at the start of the stroke
 | Multi Stroke    | if you enter 6 here you get 6*6 = 36 different strokes
 | Stroke Seed     | seed for randomly generated strokes
 | Vignette        | darkening the window border
 | Canvas Bg       | strength of background canvas pattern
 | Canvas Tint     | color tint of background

##### Some Hints:
Lower Values of "Brush Fill", "Brush Detail", "Layer Scale" and "Num Strokes" give a sketchier look. <br>
A negative "Stroke Bend" would be best for images with clouds (bending the white strokes around the cloud shape),
whereas a positive value is more usful if you have dark ojects painted on a bright background. <br>
A "Screen FOV" of makes a specular with contant light/eye angles, as if viewer and light are far away from the image.
the bigger you make this angle the more you get a localized hilight on the image plane. <br>
If "Multi Stroke" is 0 or 1 then all strokes look the same (apart from their size), if on very plain areas the similarity is too obvious to the eye you can use bigger values so you get a diversity of different stroke shapes.<br>
 
<!--<div style="page-break-after: always;"></div>-->

#### Other
 | Parameter       | function
 |-----------------|--------------
 | Flip Y          | image Y flip
 | Geom Flip Y     | Y-flip of effect-internal geometry (use this if "Effect Fade" and "Pan Fade" wont work properly)
 | HDRP Gamma      | check this if you are using linear color space (only active in hdrp mode)
 
##### concerning "Flip Y" and "Geom Flip Y":
The screen coordinates of unity are a bit mysterious. even more when working on different platforms. The Y-coordinate seems to be flipped between versions even on the same system, and also flipped depending on the system.

So for "Flip Y" and "Geom Flip Y" follow these rules:

If you have the source ("Effect Fade" to 1) flipped and the effect correct, just check "Geom Flip Y".

If you have the source correct and the effect flipped, check both "Geom Flip Y" and "Flip Y". 

If both are equally flipped just check "Flip Y". 


### Color Spaces
All flockaroo-effects are initially designed for Gamma-Space. Gamma-space was the default in earlier versions.
At first HDRP came up with using HDR-Color space, which is a linear colorspace.
So in HDRP and URP versions there's an additional parameter named "HDRP Gamma", which should compensate for washing out contrasts in linear colorspace.
However in later versions linear color space is also being used in the Standard render-pipeline.
So all versions (std/URP/HDRP) have now an additional parameter "linear2Gamma", which should be enabled as soon as you are using linear color space.
As for today the "HDRP Gamma" parameter should be disabled in most cases. 
It can sometimes give better results e.g. in HDRP, but linear2Gamma should be disabled then. both being enabled won't make much sense in general.


### HDRP (disabled by default)
The hdrp file is disabled by default !!! here's how to use it: <br>
Unity wont compile this effect properly if no hdrp support is present
on your version, so in the hdrp "...HDRP.cs" file in the very first line the "//#USE_HDRP" must be uncommmented to make use the hdrp effect.<br>
You also have to add it to the list of effects known to your project:<br>
"Edit/Project Settings... -> HDRP Default Settings -> After Post Process"<br>
..and then add it as an effect volume by clicking "Add Override" and the
selecting <br>"Post-processing/Custom/Flockaroo/..." <br>from the menu.<br><br>
BEWARE!! The effect is disabled by default until you set MasterFade to a non-zero value.


### URP (disabled by default)
The URP file is disabled by default !!! here's how to use it: <br>
Unity wont compile this effect properly if no URP-support is present
on your version, so in the urp "...URP.cs" file in the very first line the "//#USE_URP" must be uncommmented to make use the urp effect.<br>
Then under "Assets/Settings/ForwardRenderer" press "Add Renderer Feature" in the Inspector Tab.
In newer versions there are several URP Renderers URP Balanced, URP HighDynamic, URP Performant instead of Forward.
Beware: This must be the same Renderer as is configured under "ProjectSettings/Quality/Rendering".<br>
For older versions you might also want to comment the line "#define URP_VERSION_GE_13" for the effect to work.<br>
It should also run in Unity 6, but rendergraph needs to be disabled by switching to compatibility mode "Edit/ProjectSettings/Graphics/URP/RenderGraph"
<br>
<br>
![How to use URP - Image](howto_urp.png){ width="100%" }
<br>
<br>
BEWARE!! For now the effect can not be used after Post Processing. <br>Furthermore some Post-Processing-Effects like "Bloom" dont work properly. Disable those effects for proper functionality.
