# GitHub Copilot Instructions for Bulk Recipe Generator (Continued)

## Mod Overview and Purpose

**Bulk Recipe Generator (Continued)** is an updated version of the Lowlofir's original mod. The primary purpose of this mod is to automatically generate bulk recipes for items that are crafted too quickly in the game. These bulk recipes multiply the ingredient count, product count, and work amount, making it easier and more efficient to produce large quantities of items.

## Key Features and Systems

1. **Settings for Quality Handling**:
   - **Skip Quality Recipes**: Allows players to skip bulk recipe generation for items that have quality.
   - **Quality Creation Method**: Offers a setting to choose how products with quality are created—either individually or in bulk with the same quality.
   - **Quality Definition**: Provides a way to define the threshold of work a recipe must have to qualify for bulk generation instead of the hardcoded 120.

2. **Automatic Recipe Scanning**:
   - On game launch, the mod scans all recipes and generates bulk variants for those with a short crafting time.

3. **User Interface Enhancements**:
   - Right-clicking an original recipe in the "Add Bill" list displays the corresponding bulk recipe.

4. **Compatibility**:
   - The mod is safe to add to existing saves and can work with recipes from other mods, although this has not been thoroughly tested yet.

## Coding Patterns and Conventions

### C# Code Structure

- **Main Class (`Ad2.cs`)**:
  - Contains core functionality such as finding recipes, generating new recipes, and managing the transformation of recipe labels.

- **Harmony Patches**:
  - Located in the `HarmonyPatches` directory, these patches modify existing RimWorld methods to integrate new behaviors, such as modifying the GUI for float menu options and ensuring recipes are available.

- **Utility Class (`Util.cs`)**:
  - Provides helper methods like `Clamp` to ensure values are within a specified range.

### XML Integration

- **Definitions (`Things_Misc.xml`)**:
  - Contains XML definitions for things, which are used to describe the properties of items within the game.

## Harmony Patching

The mod uses Harmony to patch existing methods in RimWorld, allowing for modifications without altering the original game code. Key patches include:

- **`FloatMenuOption_DoGUI.cs`**: Modifies the GUI for float menu options to include bulk recipe options.
- **`FloatMenuOption_Chosen.cs`**: Prefixes the method to handle the selection of float menu options.
- **`RecipeDef_AvailableNow.cs`**: Postfixes the method to ensure recipes are available for generation.

## Suggestions for Copilot

1. **Code Generation**:
   - Use Copilot to generate new harmony patches for additional features or bug fixes.
   - Generate utility methods to handle common tasks, such as calculating bulk recipe ingredients or products.

2. **XML Defs**:
   - Use Copilot to assist in creating or modifying XML definitions for new items or changes in existing items.

3. **Settings Integration**:
   - Use Copilot to help with the implementation of new settings, such as those for handling quality recipes or defining work thresholds.

4. **Debugging and Testing**:
   - Use Copilot to generate code snippets for logging and debugging purposes to help identify and fix issues.

5. **Documentation**:
   - Use Copilot to generate documentation for new features or changes to existing features, ensuring that players and developers understand how to use the mod.

By following these guidelines and utilizing GitHub Copilot's suggestions, developers can enhance the Bulk Recipe Generator (Continued) mod, making it more robust and user-friendly.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

