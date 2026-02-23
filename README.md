# Re:Fract // Reloaded
A [Resonite](https://resonite.com/) mod which exposes the Unity Post Processing stack to Resonite cameras.

Requires [BepisLoader](https://modding.resonite.net/getting-started/installation/), [BepInExRenderer](https://thunderstore.io/c/resonite/p/ResoniteModding/BepInExRenderer/), and [InterprocessLib](https://thunderstore.io/c/resonite/p/Nytra/InterprocessLib/) (which itself requires [RenderiteHook](thunderstore.io/c/resonite/p/ResoniteModding/RenderiteHook/)).

## Main Features
- Lets you dynamically alter things such as bloom, depth of field and much more
- Interfaces through dynamic variables for ease-of-creation
- Values can be driven and animated via ProtoFlux as a result
- Persistent across item and world saves
- Will make you the coolest kid on the block

## Installation
*TBD*

## Usage
**Want to make your own Re:Fract camera?** The [usage instructions](https://github.com/BlueCyro/ReFract/blob/master/Usage.md) of the original Re:Fract still apply.
<br><sup>(There *is* a new `Re.Fract_RemoveAlpha` bool that isn't in those instructions though)</sup>

**Just want to take a pretty photo?** I recommend these existing cameras:
- orange's [DeliciousCam](https://uni-pocket.com/en/items/531c61df-2fc8-4470-8b1f-bba910b11f2c) has a ton of bells and whistles, a great choice for someone who wants to tweak every aspect of their photo 
- yosh's **Lightweight ReFract Camera** is a lot simpler and gentler on your computer, though it only supports focus for now. You can find it in their public folder: `resrec:///U-yosh/R-E10773FA7D4EC7F497B13DA7C8FC1500027547052763A2CED4004747637F32AD`

## Examples
None yet, you're too far back in the commit history! 😭

## Known Issues
- Minor inconvenience: right now there's a small chance that the post-processing effects won't get applied in a photo. Usually just taking it again will fix it.

## Special Thanks
- [Nytra](https://github.com/Nytra) for making [InterprocessLib](https://github.com/Nytra/ResoniteInterprocessLib), which is what made bringing Re:Fract back possible!
- [Zozokasu](https://github.com/Zozokasu) for making [ResoniteSpout](https://github.com/Zozokasu/ResoniteSpout), which was a good example of using InterprocessLib and communicating with the renderer.
- [Cyro](https://github.com/BlueCyro) for making [the OG Re:Fract](https://github.com/BlueCyro/ReFract)!
- [pocoworks](https://github.com/pocoworks) for [updating Re:Fract to Resonite](https://github.com/pocoworks/ReFract).
- [yosh](https://github.com/yoshiyoshyosh) for making [the final update to Re:Fract](https://github.com/yoshiyoshyosh/ReFract/releases/tag/1.1.3-resonite-alphaoption), which this mod is based on.
