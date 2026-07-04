# Forspoken Parambin Tool

## Overview

The **Forspoken Parambin Tool** is a command-line utility designed to help modders unpack and repack `.parambin` files from the game *Forspoken*. It converts binary parameter data into easy-to-edit Tab-Separated Values (`.tsv`) files, allowing you to modify game parameters in spreadsheet software like Excel or Libre Office, and then seamlessly repacks those changes back into game-ready `.parambin` files.

The tool can be run either by standard command-line execution or through an **Interactive Mode** that allows you to run multiple commands simply by double-clicking the executable.


---

## Features

* **Unpack:** Extract a single `.parambin` file into a folder of parsed `.tsv` files.
* **Unpack All (Batch):** Extract an entire directory of `.parambin` files at once.
* **Repack:** Compile a directory of edited `.tsv` files back into a `.parambin` file using the original binary as a base.
* **Interactive CLI:** Double-click the `.exe` to launch a persistent console window where you can type or paste commands directly without needing to open Command Prompt.
* **Graceful Fallbacks:** If you delete a `.tsv` file from your unpacked directory, the tool automatically skips it and safely repacks the original data for that table.


*(Though this tool can be used for localization and text edits, there is a simpler tool for that here https://github.com/Rinual/ForspokenTools)*

---

## Usage

### Interactive Mode

Simply double-click `ForspokenBinTool.exe`. A console window will open where you can type commands directly. You can copy and paste file paths (even those with spaces wrapped in quotes) directly into the prompt.

### Command Line Mode

Run the tool from your terminal or command prompt by passing the arguments directly to the executable.

### Commands

* `unpack <input.parambin> <output_directory>`
* `unpackall <input_directory> <output_directory>`
* `repack <editedParamBin_tsv_directory> <originalParambin.parambin> <output_directory>`
* `debug` (Displays advanced debugging and testing options)
* `testsuite help` (Displays information about the mass validation suite)

---

## Sample Commands

**Unpacking a single file:**

```cmd
ForspokenBinTool unpack "C:\Modding\parkour.parambin" "C:\Modding\Unpacked"

```

**Batch unpacking a folder:**

```cmd
ForspokenBinTool unpackall "C:\Modding\Original_Bins" "C:\Modding\Unpacked_All"

```

**Repacking edited TSV files:**

```cmd
ForspokenBinTool repack "C:\Modding\Unpacked\parkour" "C:\Modding\parkour.parambin" "C:\Modding\Repacked_Bins"

```

---

## Editing the TSV Files

Once you unpack a `.parambin` file, you can open the resulting `.tsv` files in any spreadsheet editor (like Excel, Libre Office, Google Sheets). To ensure your edits repack successfully, follow these rules:

### 1. Reading the Headers

The first row of every `.tsv` file contains the column headers. Alongside the internal `TagId`, the header displays the **Data Type** for that column (e.g., `Float`, `Fixid`, `IntegerArray`). This tells you exactly how the game expects that data to be formatted. If a column has no data type listed, it is **unmapped** (see rule 5).

### 2. Do Not Edit Element IDs

The very first column in every table contains the `ElementID`. These IDs are critical anchors that the repacker uses to map your rows back to the original binary. **Do not alter or delete the Element IDs**, or the repacker will fail to inject your row.

### 3. Editing Fixids (Resolved Text)

To make modding easier, the unpacker tries to resolve numeric `Fixid` values into human-readable text. They will appear in the `.tsv` like this:
`12345678 ("magic_spell_fireball")`

When you repack the file, the encoder **only reads the numeric ID** before the first space.

* **To change a value:** Change the number at the front.
* **The text is ignored:** You do not need to update the text inside the parentheses when you change the ID. The text is purely there for your reference while editing.

### 4. Editing Arrays

Columns labeled with array data types (e.g., `FloatArray`, `FixidArray`, `StringArray`) contain multiple values wrapped in brackets.

* **Format:** `[value1, value2, value3]`
* If you add or remove items from an array, ensure the entire string remains wrapped in `[` and `]`, and that items are separated by commas.
* Avoid changing the total number of entries in an array unless you are sure the game supports it. The engine seems to reserve strict array sizes, even for unused arrays. 

### 5. Handling Unmapped Data (Advanced)

If a column header does not list a specific Data Type, it means that data is currently unmapped in the `SchemaRegistry.cs` as well as the manual schema map in `SchemaConfig.cs`. 

* By default, the unpacker and repacker treat all unmapped data as raw **UInt32** integers.
* If you type a standard whole number here, it will repack fine.
* The unmapped data will never be `strings` or `arrays`, those have all been confirmed present when using the `testsuite` command and verifying all data is written back 1:1 with all array heaps and string heaps present.
* **Editing Unmapped Floats:** If you suspect an unmapped column is actually meant to be a `Float`, typing a decimal like `1.5` will not work because the fallback encoder only expects integers. You have two options:
1. *(Recommended)* Open `SchemaConfig.cs` in the source code, map the `TableId` and `TagId` to `ParambinDataType.Float`, and recompile the tool.
2. *(Workaround)* Convert the float value into its raw UInt32 decimal equivalent using an online converter (e.g., `1.0` becomes `1065353216`), and paste that raw integer into the TSV. The game engine will read the bits identically.

## Limitations

Out of the game's **1,418 parameter tables**, there are exactly **18 tables** that contain too little data (only 1 row) for the auto-schema mapper to accurately determine their data types.

By default, the unpacker will export these 18 tables with a `_READ_ONLY.tsv` suffix. If you attempt to repack a modded folder containing these files, the repacker will safely ignore your edits to those specific tables and inject the original game data instead. This prevents the tool from corrupting your `.parambin` files due to incorrect data mapping.

To bypass this safety lock, see the **Debug & Advanced Usage** section below.

---

## Debug & Advanced Usage

### The `--OverwriteSchema` Flag

If you want to experiment with modding any of the 18 `_READ_ONLY` tables, you can append `--OverwriteSchema` to your commands. This will use a custom mapping that results in proper repacking, but there was some guesswork involved.

*Example:*

```cmd
ForspokenBinTool unpack "parkour.parambin" "output" --OverwriteSchema

```

**Warning:** This uses my "Best Guess" mapping for that table. The layout and data types of these specific tables are not guaranteed, and editing them may cause game crashes if the mapping was incorrect. 
This custom mapping can be edited or expanded in `SchemaConfig.cs`

### Mass Repack Tester (`testsuite`)

The tool includes a built-in validation suite designed to verify that repacking untouched `.tsv` files results in a 1:1 match with the original `.parambin` files. This is really only needed if you are changing custom mapping

*Usage:*

```cmd
ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir> 
or
ForspokenBinTool testsuite <original_parambins_dir> <temp_work_dir> --OverwriteSchema

```

*Note: The `testsuite` command will delete the contents of your `temp_work_dir` during its routine. Do not use an important folder for this temporary output.*

---

## Building from Source & Custom Schema Mapping

If you want to manually fix the 18 tables with unconfirmed schemas or add your own overrides to those or other schema maps, you will need to modify the source code and compile the tool yourself.

### 1. Setting up Custom Mappings

Open the source code and navigate to `SchemaConfig.cs`. Inside, you will find the registry mappings for table data types. You can manually map the column data types (e.g., `UInt32`, `Float`, `Int16`) for the flagged tables using their `TableId` and `TagId`.

When you run the tool with the `--OverwriteSchema` flag, it will prioritize your custom mappings in `SchemaConfig.cs` over the automated parser.

If you ever want to finalize any confirmed changes to the schema, remove them from `SchemaConfig.cs` and add them to `SchemaRegistry.cs`. This will remove the need to use `--OverwriteSchema` ever. 

Here are the rewritten and new sections you can drop right into your README:

### 2. Building and Running the Project

You will need the .NET SDK installed to compile and run the C# project.

1. Clone or download the source code repository.
2. Open a terminal (or Command Prompt/PowerShell) and navigate to the root folder of the project (where the `.csproj` file is located).
3. Run the following command to build and launch the tool directly:

```cmd
dotnet run

```
This will instantly compile the code and start the Interactive Mode directly in your terminal 
*(Note: If you are using Visual Studio, you can still just open the solution and click "Start without debugging").*

---

## Contributing Confirmed Schemas

If you experiment with the `--OverwriteSchema` flag and successfully verify the correct data types for previously unmapped tables, we would love to support your findings.

To contribute your discoveries:

1. Move your confirmed mappings out of `SchemaConfig.cs` and add them permanently to `SchemaRegistry.cs`.
2. Fork this repository on GitHub and commit your updated registry.
3. Submit a **Pull Request (PR)**. In your PR description, please include a brief note about which tables you mapped and how you confirmed them in-game (or via the `testsuite`).

---

# Workflow Recommendation

Recommended workflow:

1. Export TSV
2. Make edits
3. Repack
4. Test in-game

---

# Troubleshooting

Make sure:

* You edited the correct TSV
* The TSV is tab-separated
* FixIds were not modified
* TSV was not sorted and the order of rows and columns were not modified
* You repacked using the edited TSV

---

## Broken strings in game

Possible causes:

* TSV editor corrupted tabs
* Opcode data was manually altered
* Encoding changed unexpectedly

Use UTF-8 encoding when possible.

---

# Credits

Format and tooling by Rinual.

Original FFXV Bdevresource research used as the starting point for the project by Kizari. (https://github.com/Kizari/Flagrum)

id2s discovery by MajoMix (https://github.com/majomix/forspoken-lockit)
