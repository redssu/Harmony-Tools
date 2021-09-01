# Harmony-Tools

A collection of tools to help translate Danganronpa V3: Killing Harmony video game. Currently, only the PC version of this game is supported.

All tools are non-interactive and can be used via your favorite terminal such as Konsole, CMD or PowerShell.

Here you will also find a tool called "Explorer-Extension" that adds a new contextual menu list that contains shortcuts to other tools, making them faster and more convenient to use. Explorer-Extension currently only supports Windows.

## Installation

In each release, I'm attaching the ZIP file with ```Installer.exe```  and ```bin``` directory inside it. The installer will copy all files from ```bin``` into ```%ProgramFiles%\HarmonyTools``` directory and it will add this directory to the Enviroment Path Variable, so it can be used in every directory via terminal. Note that all executables in ```bin``` directory are prefixed with "HT" just in case of conflicts with other programs, so you have to run ```HTStx``` instead of ```Stx```.

**Warning**: Installer needs administrator privileges.

After successful install, it is recommended to register Context Menu using Explorer-Extension tool.


## Contributing

Pull requests are welcome. I just started programming in C#, so in the code of tools there are definitely things that can be done better, more efficiently.


## Notes

Most of these tools are basically wrappers to tools made by [CapitanSwag](https://github.com/jpmac26) - he did most of the hard work - these tools would not be created without his effort. My goal was to create a translator friendly set of tools, so I used code of [DRV3-Sharp](https://github.com/jpmac26/DRV3-Sharp) and made them completely non-interactive. 

The repository created by [EDDxample](https://github.com/EDDxample) ([Ultimate-DRv3-Toolset](https://github.com/EDDxample/ultimate-drv3-toolset)) was also a big source of information for me.  Without it, I couldn't write a font packing tool.


## Usage

### EXPLORER-EXTENSION

Tool usage:

```ExplorerExtension (--register | --unregister) [--lang=(EN | PL)] [--delete-original]```

If the parameter ```--register``` is set, the tool will make a new entries into the System Registry resulting in new Explorer Context Menu, which appears when user is right-clicking onto a file or directory.

If the parameter ```--delete-original``` is set during the registering operation, Context Menu Entries will invoke commands with
 ```--delete-original``` parameter.

The ```--lang``` parameter is used to switch between (currently) two languages. The default one is English (EN). This setting changes only the text in Context Menu.

If the parameter ```--unregister``` is set, the tool will remove previously created entries in System Registry.

**Note**: This program assumes, that you installed all tools with provided installer - it uses the tools that are prefixed with "HT" and assumes, that they are in ```%ProgramFiles%\HarmonyTools``` directory.

 

### STX

STX files moslty contain character dialogs or fonts.

This tool extracts a ".STX" file to ".TXT" format, or packs a ".TXT" file to ".STX" format. To convert fonts, see the **FONT** tool section.

Tool Usage:

```stx (--unpack|--pack) file_path [--delete-original] [--pause-after-error]```

If the parameter ``` --delete-original``` is set, the original file will be deleted afterwards.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

If you are packing a ".TXT" into ".STX" and the file name ends with ".STX.TXT", the tool will only remove the ".TXT" suffix from the name. 


### DAT

DAT files contain data formatted in form of table, typically used in mini-games or class trials.

**Warning**: Some DAT files don't contain tables and cannot be opened by this tool. Such files are mostly located in the "wrd_data" folder.

This tool extracts a ".DAT" file to ".CSV" format, or packs a ".CSV" file to ".DAT" format. ".CSV" files can be easily opened by Office Excel or LibreOffice Calc.

Tool Usage:

```dat (--unpack|--pack) file_path [--delete-original] [--pause-after-error]```

If the parameter ``` --delete-original``` is set, the original file will be deleted afterwards.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

If you are packing a ".CSV" into ".DAT" and the file name ends with ".DAT.CSV", the tool will only remove the ".CSV" suffix from the name. 


### WRD

WRD files contain game scripts, from which you can read who is speaking in the diffrent lines of STX file.

**Note**: Packing a WRD file is not currently supported.

Tool Usage:

```wrd --unpack file_path [--translate] [--delete-original] [--pause-after-error]```

If the parameter ```--translate``` is set, the Opcodes will be translated to a more readable sentences.

If the parameter ```--delete-original``` is set, the original file will be deleted afterwards.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.


### SPC

SPC archives are general purpose archives that contain other files, for example ".STX" or ".SRD". They can be compared to the well-known ZIP archives.

Tool Usage:

```spc (--unpack|--pack) object_path [--delete-original] [--pause-after-error]```

```object_path``` should be a path to the directory if you are trying to pack or a file if you are trying to unpack.

If ```--delete-original ``` is set, the original object will be deleted afterwards.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

When unpacking an SPC archive, the tool will create a directory with the same name as the archive with the ".decompressed" suffix.

When packing a directory into an SPC archive, the tool will remove the ".decompressed" suffix from the directory name and create a ".SPC" file based on that.

**Warning**: Do not modify the ```__spc_info.json``` file inside the created directory. It's crucial to packing the file back to SPC Archive.


### SRD

The SRD archives store files related to textures or game models. This tool only allows you to extract and replace textures.

Tool Usage:

```srd (--unpack|--pack) object_path [--delete-original] [--pause-after-error]```

```object_path``` should be path to the directory containing the textures that will replace those inside the SRD archive or the path to a SRD Archive meant for unpacking.

If the parameter ``` --delete-original``` is set, the original object will be deleted afterwards.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

Note that this tool will extract only textures from an SRD archive.

When unpacking an SRD archive, the tool will create a directory with the same name as the archive with the ".decompressed" suffix.

When packing an directory into an SRD archive, the tool will only replace textures that exist in ```_.srd``` file (which is a copy of original ".SRD" file). This tool does not support advanced compression used by Danganronpa V3 devs, so you should replace only textures that are absolutely necessary for replacement. Replacing all textures leads to huge ".SRD" file size. In order to replace only some textures - delete other textures from the tool generated directory so that they are not replaced during the operation. The tool will remove ".decompressed" suffix from directory name and create a ".SRD" file based on directory name.

**Warning**: Do not remove the ```_.srd``` and ```_.srdi``` (if exists) or ```_.srdv``` (if exists). Without them the directory cannot be converted back into SRD file.


### FONT

Tool Usage:

```font (--unpack|--pack) object_path [--gen-debug-image] [--pause-after-error]```

**PACKING**

```object_path``` should be a path to the directory

If ```--gen-debug-image``` parameter is set when packing, the tool will create a file with the same name as the directory, adding ``` __DEBUG_IMAGE``` to it. This file will contain the texture which is made of all glyphs.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

The directory that will be packed into the font file must contain the file ``` __font_info.json```, which contains a JSON object with the following properties:

- "FontName" - contains the name of the used font in the form of a string. For example: "ComicSans.otf" - This is for informational purposes only, but is required
- "Charset" - charset which is used in this font. When packing a font - you can write everything you want to, the tool will replace it based on glyph files
- "ScaleFlag" - this is a magic value from original font file
- "Resources" -Provides a list of resources in the form of a list. This list should contain two strings;
  - "font_table"
  - The name of the font texture, which can be found when extracting the font file with the SRD tool

The file `` __font_info.json`` is generated automatically after unpacking the correct font file. You can use this as a reference or just use the same file.

There should be ".BMP" files in the directory that are used as glyph textures. Each ".BMP" file should contain only one glyph. They should be named as increasing numbers (e.g .: ```0000.bmp```, ```0001.bmp``` etc.). Numbers should be padded with zeros to match the length of the largest number (so if there is a file ```127.bmp``` there should be two zeros before each one-digit name and one zero before each of the two-digit name).

Each ".BMP" file should have a corresponding ".JSON" file with the same name. The ".JSON" file should have the following structure:

``` js
{
  "Glyph": "S",   // <- the character that will be represented by the corresponding texture
  "Kerning": {    // <- spacing
    "Left": 2,
    "Right": 3,
    "Vertical": 4
  }
}
```



**UNPACKING**

```object_path``` should be a path to a ".STX" or ".SRD" file

The tool will create a directory based on the name of ```object_path```, adding ".decompressed_font" to the end of the file name.

If ```--pause-after-error``` parameter is set - the tool will wait for user interaction before exiting if any error occurred.

The directory will contain files in a specific schema, described in the **PACKING** section.





