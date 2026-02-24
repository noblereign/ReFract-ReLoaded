# Re:Fract // Reloaded
[![Thunderstore Badge](https://modding.resonite.net/assets/available-on-thunderstore.svg)](https://thunderstore.io/c/resonite/p/Noble/ReFract_Reloaded/)

A [Resonite](https://resonite.com/) mod which exposes the Unity Post Processing stack to Resonite cameras.

Requires [BepisLoader](https://modding.resonite.net/getting-started/installation/), [BepInExRenderer](https://thunderstore.io/c/resonite/p/ResoniteModding/BepInExRenderer/), and [InterprocessLib](https://thunderstore.io/c/resonite/p/Nytra/InterprocessLib/) (which itself requires [RenderiteHook](thunderstore.io/c/resonite/p/ResoniteModding/RenderiteHook/)).

## Main Features
- Lets you dynamically alter things such as bloom, depth of field and much more
- Interfaces through dynamic variables for ease-of-creation
- Values can be driven and animated via ProtoFlux as a result
- Persistent across item and world saves
- Will make you the coolest kid on the block

## Installation
### Automatic (Recommended)
1. Install [BepisLoader](https://github.com/ResoniteModding/BepisLoader)
2. Install via Thunderstore/Gale, or download from [Releases](https://github.com/noblereign/ReFract-ReLoaded/releases)

## Usage
**Want to make your own Re:Fract camera?** The [usage instructions](https://github.com/BlueCyro/ReFract/blob/master/Usage.md) of the original Re:Fract still apply.
<br><sup>(There *is* a new `Re.Fract_RemoveAlpha` bool that isn't in those instructions though)</sup>

**Just want to take a pretty photo?** I recommend these existing cameras:
- orange's [DeliciousCam](https://uni-pocket.com/en/items/531c61df-2fc8-4470-8b1f-bba910b11f2c) has a ton of bells and whistles, a great choice for someone who wants to tweak every aspect of their photo 
- yosh's **Lightweight ReFract Camera** is a lot simpler and gentler on your computer, though it only supports focus for now. You can find it in their public folder: `resrec:///U-yosh/R-E10773FA7D4EC7F497B13DA7C8FC1500027547052763A2CED4004747637F32AD`

## Examples
<img width="720" height="408" alt="2026-02-22 10 00 25" src="https://github.com/user-attachments/assets/d836a34d-4533-4dc6-b110-a7428aa5cda1" />
<img width="720" height="408" alt="2026-02-21 00 53 13 fixed-alpha" src="https://github.com/user-attachments/assets/21731ec5-1288-489e-85c6-aff76b4ca894" />
<img width="720" height="408" alt="2026-02-20 22 38 06 fixed-alpha" src="https://github.com/user-attachments/assets/06f5468e-f9f8-4651-89d0-7554830c8333" />
<img width="720" height="408" alt="2026-02-20 17 19 33 fixedalpha" src="https://github.com/user-attachments/assets/b84b9c06-07ec-42aa-a7b2-d869e803115d" />

## Known Issues
- Minor inconvenience: right now there's a small chance that the post-processing effects won't get applied in a photo. Usually just taking it again will fix it.

## Special Thanks
- [Nytra](https://github.com/Nytra) for making [InterprocessLib](https://github.com/Nytra/ResoniteInterprocessLib), which is what made bringing Re:Fract back possible!
- [Zozokasu](https://github.com/Zozokasu) for making [ResoniteSpout](https://github.com/Zozokasu/ResoniteSpout), which was a good example of using InterprocessLib and communicating with the renderer.
- [Cyro](https://github.com/BlueCyro) for making [the OG Re:Fract](https://github.com/BlueCyro/ReFract)!
- [pocoworks](https://github.com/pocoworks) for [updating Re:Fract to Resonite](https://github.com/pocoworks/ReFract).
- [yosh](https://github.com/yoshiyoshyosh) for making [the final update to Re:Fract](https://github.com/yoshiyoshyosh/ReFract/releases/tag/1.1.3-resonite-alphaoption), which this mod is based on.

## Notes and disclaimers
> [!WARNING]
> **Gemini Code Assist was used in the making of this mod**, primarily to write code that I *knew* I needed to write, but didn't know *how*. I made sure the code it spat out wasn't completely useless garbage, but, y'know.
>
> I tested the mod extensively throughout making it and fixed as many bugs as I could, but I'm sure there's still so much that could be improved code-wise. If you have any improvements to suggest, please submit an issue/PR!

> [!NOTE]
> **Please feel free to port the mod to other loaders** if you wish to. I used Bepis because it's what I felt the most comfortable working with this time, but InterprocessLib theoretically supports MonkeyLoader and ResoniteModLoader. I don't know how that support necessarily works, but if you're smarter than I am, you can probably figure it out.
