# Excalibur Shader System (LumaLooks) — Standalone

> **This README was written by AI (GPT-5.6 Luna) :D**

quick message from me! to install this, you need to download the zip in releases, extract it, and drag the resulting folder into your plugins! im also ***not*** the original person to get this mod's DLL! im only the messenger! :P

## ⚠️ Important Notice

This repository is **not an original implementation** of the Excalibur shader system.

The code in this repository was taken from the shader-system portion of **Excalibur**, a closed-source paid mod. I am **not claiming authorship of that code**, and this repository is not affiliated with or endorsed by the original Excalibur developer.

Please refer to the original project's licensing and distribution terms before using, modifying, or redistributing this code.

## What Is This?

This repository contains a standalone version of the **shader system from Excalibur**, separated from the rest of the mod's functionality.

The goal was to make the shader system functional without the additional DLLs and other components that Excalibur normally requires.

In doing so, AI tools were used extensively to help analyze the existing code and get the shader system functioning independently.

### What Is Included

* The Excalibur shader system
* Changes necessary to make the shader system operate independently
* Supporting code required for the standalone implementation

### What Is **Not** Included

**The Excalibur camera mod, along with any other part, is not included in this repository.**

This project is specifically focused on the shader system and does not contain the camera-mod portion of Excalibur.

## Observations About the Original Code

While examining the original Excalibur code, I noticed several references that appear to be associated with **Claude/Anthropic tooling**, including references to a Claude-related path under the user's AppData directory.

For example, one such reference occurs around **[RenderEngine.cs](https://github.com/Duskdotso/LumaLooks/blob/main/Excalibur.ProPlus/LumaLooks/RenderEngine.cs#L2018), line 2018**, with several other locations containing similar references.

These observations led me to suspect that at least portions of the shader system may have been developed with AI assistance.

**This is an observation, not a definitive claim about how the original code was authored.** A reference to an AI-related development path alone does not establish that AI generated the code.

## AI Assistance

AI was also used extensively in creating this standalone version.

In particular, AI was used to help:

1. Analyze the existing shader-system code.
2. Identify dependencies on components normally provided by Excalibur.
3. Separate the shader system from those dependencies.
4. Resolve issues encountered while making it function independently.
5. Get the resulting standalone implementation into a usable state.

The purpose of this project was therefore **not to claim that the shader system was independently written**, but to make the existing shader-system component usable without the rest of Excalibur.

## Attribution

The original shader-system code originated from **Excalibur**.

I did not create the original shader system and make no claim of ownership over it.

This repository should not be interpreted as an official release, fork, or endorsed version of Excalibur.

If you are the original author or rights holder and have concerns regarding the contents of this repository, please contact me, and I'll delete this repository :D

## Disclaimer

This project is provided for informational and experimental purposes.

**Excalibur** and any associated names, code, trademarks, or other intellectual property remain the property of their respective owners.

No affiliation with, sponsorship by, or endorsement from the original Excalibur developer is implied.
