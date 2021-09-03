using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ExplorerExtension {
    class Program {
        public static Dictionary<string, Dictionary<string, string>> texts = new Dictionary<string, Dictionary<string, string>>() {
            { 
                "EN", new Dictionary<string, string>() {  
                    { "UnpackDialogueName", "Unpack Dialogue file" },
                    { "UnpackSTXName", "Unpack STX file" },
                    { "UnpackDATName", "Unpack DAT file" },
                    { "UnpackSPCName", "Unpack SPC archive" },
                    { "UnpackSRDName", "Unpack SRD archive (only textures)" },
                    { "UnpackFontsName", "Unpack font file" },

                    { "UnpackAllDialogueName", "Unpack All Dialogue files" },
                    { "UnpackAllSTXName", "Unpack All STX files" },
                    { "UnpackAllDATName", "Unpack All DAT files" },
                    { "UnpackAllSPCName", "Unpack All SPC archives" },
                    { "UnpackAllSRDName", "Unpack All SRD archives" },

                    { "PackDialogueName", "Pack this file to Dialogue file" },
                    { "PackSTXName", "Pack this file to STX file" },
                    { "PackDATName", "Pack this file to DAT file" },
                    { "PackSPCName", "Pack this directory to SPC Archive" },
                    { "PackSRDName", "Pack this directory to SRD Archive" },
                    { "PackFontsName", "Pack this directory to font file" },

                    { "PackAllDialogueName", "Pack JSON files as STX files" },
                    { "PackAllSTXName", "Pack TXT files as STX files" },
                    { "PackAllDATName", "Pack CSV files as DAT files" },
                    { "PackAllSPCName", "Pack .decompressed directories as SPC archives" }
                }
            },
            {
                "PL", new Dictionary<string, string>() {
                    { "UnpackDialogueName", "Rozpakuj plik dialogow" },
                    { "UnpackSTXName", "Rozpakuj plik STX" },
                    { "UnpackDATName", "Rozpakuj plik DAT" },
                    { "UnpackSPCName", "Rozpakuj archiwum SPC" },
                    { "UnpackSRDName", "Rozpakuj archiwum SRD (tylko tekstury)" },
                    { "UnpackFontsName", "Rozpakuj plik czcionki" },

                    { "UnpackAllDialogueName", "Rozpakuj wszystkie pliki Dialogow" },
                    { "UnpackAllSTXName", "Rozpakuj wszystkie pliki STX" },
                    { "UnpackAllDATName", "Rozpakuj wszystkie pliki DAT" },
                    { "UnpackAllSPCName", "Rozpakuj wszystkie archiwa SPC" },
                    { "UnpackAllSRDName", "Rozpakuj wszystkie archiwa SRD" },

                    { "PackDialogueName", "Zapakuj plik jako Dialog" },
                    { "PackSTXName", "Spakuj ten plik jako plik STX" },
                    { "PackDATName", "Spakuj ten plik jako plik DAT" },
                    { "PackSPCName", "Spakuj ten katalog jako archiwum SPC" },
                    { "PackSRDName", "Spakuj ten katalog jako archiwum SRD" },
                    { "PackFontsName", "Spakuj ten katalog jako plik czcionki" },

                    { "PackAllDialogueName", "Spakuj pliki JSON jako STX" },
                    { "PackAllSTXName", "Spakuj pliki TXT jako STX" },
                    { "PackAllDATName", "Spakuj pliki CSV jako DAT" },
                    { "PackAllSPCName", "Spakuj foldery .decompressed jako archiwa SPC" }
                }
            }

        };

        public static string language = "EN";

        public static bool deleteOriginal = false;

        static void Main( string[] args ) {
            if ( args.Length < 1 ) {
                Console.WriteLine( "Usage: ExplorerExtension (--register | --unregister) [--lang=(EN | PL)] [--delete-original]" );
                return;
            }

            bool wantToRegister = true;

            foreach ( string arg in args ) {
                if ( arg.ToLower() == "--register" ) {
                    wantToRegister = true;
                }
                else if ( arg.ToLower() == "--unregister" ) {
                    wantToRegister = false;
                }
                else if ( arg.ToLower() == "--delete-original" ) { 
                    deleteOriginal = true;
                }
                else if ( arg.ToLower() == "--lang=pl" ) {
                    language = "PL";
                }
                else if ( arg.ToLower() == "--lang=en" ) {
                    language = "EN";
                }
                else if ( arg.StartsWith( "--" ) ) {
                    Console.WriteLine( "Error: Unknown argument: " + arg );
                    return;
                }
            }

            if ( wantToRegister ) {
                RegisterContextMenus();
            }
            else {
                UnregisterContextMenus();
            }
        }

        static bool doesKeyExists ( string keyName ) {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey( keyName, false );
            return key != null;
        }

        static void RegisterContextMenus () {
            string installationPath = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles );
            installationPath = installationPath + "\\HarmonyTools";
    
            if ( !Directory.Exists( installationPath ) ) {
                Console.WriteLine( "Error: HarmonyTools are not in \"" + installationPath + "\" directory" );
                return;
            }

            if ( !doesKeyExists( @"*\shell\HarmonyTools" ) ) {
                try { 
                    RegistryKey HarmonyTools = Registry.ClassesRoot.CreateSubKey( @"*\shell\HarmonyTools" );
                    HarmonyTools.SetValue( "Icon", installationPath + @"\Harmony-Tools-Icon.ico" );
                    HarmonyTools.SetValue( "MUIVerb", "Harmony Tools" );
                    HarmonyTools.SetValue( "Position", "Top" );
                    HarmonyTools.SetValue( "subcommands", "" );

                    RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey( "shell" );

                    // Unpack Dialogue
                    RegistryKey UnpackDialogueItem = HarmonyToolsShell.CreateSubKey( "1_UnpackDialogue" );
                    UnpackDialogueItem.SetValue( "MUIVerb", texts[ language ][ "UnpackDialogueName" ] );
                    UnpackDialogueItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );

                    RegistryKey UnpackDialogueCommand = UnpackDialogueItem.CreateSubKey( "command" );
                    UnpackDialogueCommand.SetValue( "", installationPath + "\\HTDialogue.exe --unpack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack Dialogue
                    RegistryKey PackDialogueItem = HarmonyToolsShell.CreateSubKey( "2_PackDialogue" );
                    PackDialogueItem.SetValue( "MUIVerb", texts[ language ][ "PackDialogueName" ] );
                    PackDialogueItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );
                    PackDialogueItem.SetValue( "CommandFlags", (uint) 0x40, RegistryValueKind.DWord );

                    RegistryKey PackDialogueCommand = PackDialogueItem.CreateSubKey( "command" );
                    PackDialogueCommand.SetValue( "", installationPath  + "\\HTDialogue.exe --pack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );
                    
                    // ----------

                    // Unpack STX
                    RegistryKey UnpackSTXItem = HarmonyToolsShell.CreateSubKey( "3_UnpackSTX" );
                    UnpackSTXItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSTXName" ] );
                    UnpackSTXItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );

                    RegistryKey UnpackSTXCommand = UnpackSTXItem.CreateSubKey( "command" );
                    UnpackSTXCommand.SetValue( "", installationPath + "\\HTStx.exe --unpack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack STX
                    RegistryKey PackSTXItem = HarmonyToolsShell.CreateSubKey( "4_PackSTX" );
                    PackSTXItem.SetValue( "MUIVerb", texts[ language ][ "PackSTXName" ] );
                    PackSTXItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );
                    PackSTXItem.SetValue( "CommandFlags", (uint) 0x40, RegistryValueKind.DWord );

                    RegistryKey PackSTXCommand = PackSTXItem.CreateSubKey( "command" );
                    PackSTXCommand.SetValue( "", installationPath + "\\HTStx.exe --pack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // ----------

                    // Unpack DAT
                    RegistryKey UnpackDATItem = HarmonyToolsShell.CreateSubKey( "5_UnpackDAT" );
                    UnpackDATItem.SetValue( "MUIVerb", texts[ language ][ "UnpackDATName" ] );
                    UnpackDATItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );

                    RegistryKey UnpackDATCommand = UnpackDATItem.CreateSubKey( "command" );
                    UnpackDATCommand.SetValue( "", installationPath + "\\HTDat.exe --unpack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack DAT
                    RegistryKey PackDATItem = HarmonyToolsShell.CreateSubKey( "6_PackDAT" );
                    PackDATItem.SetValue( "MUIVerb", texts[ language ][ "PackDATName" ] );
                    PackDATItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );
                    PackDATItem.SetValue( "CommandFlags", (uint) 0x40, RegistryValueKind.DWord );

                    RegistryKey PackDATCommand = PackDATItem.CreateSubKey( "command" );
                    PackDATCommand.SetValue( "", installationPath + "\\HTDat.exe --pack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // ----------

                    // Unpack SPC 
                    RegistryKey UnpackSPCItem = HarmonyToolsShell.CreateSubKey( "7_UnpackSPC" );
                    UnpackSPCItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSPCName" ] );
                    UnpackSPCItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-Icon.ico" );

                    RegistryKey UnpackSPCCommand = UnpackSPCItem.CreateSubKey( "command" );
                    UnpackSPCCommand.SetValue( "", installationPath + "\\HTSpc.exe --unpack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack SRD
                    RegistryKey UnpackSRDItem = HarmonyToolsShell.CreateSubKey( "8_UnpackSRD" );
                    UnpackSRDItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSRDName" ] );
                    UnpackSRDItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-Icon.ico" );

                    RegistryKey UnpackSRDCommand = UnpackSRDItem.CreateSubKey( "command" );
                    UnpackSRDCommand.SetValue( "", installationPath + "\\HTSrd.exe --unpack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack Font
                    RegistryKey UnpackFontsItem = HarmonyToolsShell.CreateSubKey( "9_UnpackFonts" );
                    UnpackFontsItem.SetValue( "MUIVerb", texts[ language ][ "UnpackFontsName" ] );
                    UnpackFontsItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-Icon.ico" );

                    RegistryKey UnpackFontsCommand = UnpackFontsItem.CreateSubKey( "command" );
                    UnpackFontsCommand.SetValue( "", installationPath + "\\HTFont.exe --unpack \"%1\" --pause-after-error" );
                }
                catch ( System.UnauthorizedAccessException ) {
                    Console.WriteLine( "Error: You don't have permission to register the context menu." );
                    Console.WriteLine( "Tip: Try to run this program as administrator" );
                    return;
                }
            }
            else {
                Console.WriteLine( "Warning: Context menu for all files already exists" );
            }

            if ( !doesKeyExists( @"Directory\shell\HarmonyTools" ) ) {
                try {
                    RegistryKey HarmonyTools = Registry.ClassesRoot.CreateSubKey( @"Directory\shell\HarmonyTools" );

                    HarmonyTools.SetValue( "Icon", installationPath + @"\Harmony-Tools-Icon.ico" );
                    HarmonyTools.SetValue( "MUIVerb", "Harmony Tools" );
                    HarmonyTools.SetValue( "Position", "Top" );
                    HarmonyTools.SetValue( "subcommands", "" );

                    RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey( "shell" );

                    // Context submenu items
                    // SPC
                    RegistryKey PackSPCItem = HarmonyToolsShell.CreateSubKey( "1_PackSPC" );
                    PackSPCItem.SetValue( "MUIVerb", texts[ language ][ "PackSPCName" ] );
                    PackSPCItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico" );

                    RegistryKey PackSPCCommand = PackSPCItem.CreateSubKey( "command" );
                    PackSPCCommand.SetValue( "", installationPath + "\\HTSpc.exe --pack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // SRD
                    RegistryKey PackSRDItem = HarmonyToolsShell.CreateSubKey( "2_PackSRD" );
                    PackSRDItem.SetValue( "MUIVerb", texts[ language ][ "PackSRDName" ] );
                    PackSRDItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico" );

                    RegistryKey PackSRDCommand = PackSRDItem.CreateSubKey( "command" );
                    PackSRDCommand.SetValue( "", installationPath + "\\HTSrd.exe --pack \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Fonts
                    RegistryKey PackFontsItem = HarmonyToolsShell.CreateSubKey( "3_PackFonts" );
                    PackFontsItem.SetValue( "MUIVerb", texts[ language ][ "PackFontsName" ] );
                    PackFontsItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico" );

                    RegistryKey PackFontsCommand = PackFontsItem.CreateSubKey( "command" );
                    PackFontsCommand.SetValue( "", installationPath + "\\HTFont.exe --pack \"%1\" --pause-after-error" );
                }
                catch ( System.UnauthorizedAccessException ) {
                    Console.WriteLine( "Error: You don't have permission to register the context menu." );
                    Console.WriteLine( "Tip: Try to run this program as administrator" );
                    Console.WriteLine( "Press <Enter> to close this window" );
                    while ( Console.ReadKey().Key != ConsoleKey.Enter ) {}
                    return;
                }
            }
            else {
                Console.WriteLine( "Warning: Context menu for directories already exists" );
            }

            if ( !doesKeyExists( @"Directory\Background\shell\HarmonyTools" ) ) {
                try {
                    RegistryKey HarmonyTools = Registry.ClassesRoot.CreateSubKey( @"Directory\Background\shell\HarmonyTools" );

                    HarmonyTools.SetValue( "Icon", installationPath + @"\Harmony-Tools-Icon.ico" );
                    HarmonyTools.SetValue( "MUIVerb", "Harmony Tools" );
                    HarmonyTools.SetValue( "Position", "Top" );
                    HarmonyTools.SetValue( "subcommands", "" );

                    RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey( "shell" );
                    
                    /**
                     * UNPACKING
                     */

                    // Unpack All Dialogue
                    RegistryKey UnpackAllDialogueItem = HarmonyToolsShell.CreateSubKey( "1_UnpackAllDialogue" );
                    UnpackAllDialogueItem.SetValue( "MUIVerb", texts[ language ][ "UnpackAllDialogueName" ] );
                    UnpackAllDialogueItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );

                    RegistryKey UnpackAllDialogueCommand = UnpackAllDialogueItem.CreateSubKey( "command" );
                    UnpackAllDialogueCommand.SetValue( "", installationPath + "\\ConvertAll.exe --unpack --format=DIALOGUE \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack All STX
                    RegistryKey UnpackAllSTXItem = HarmonyToolsShell.CreateSubKey( "2_UnpackAllSTX" );
                    UnpackAllSTXItem.SetValue( "MUIVerb", texts[ language ][ "UnpackAllSTXName" ] );
                    UnpackAllSTXItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );
                    
                    RegistryKey UnpackAllSTXCommand = UnpackAllSTXItem.CreateSubKey( "command" );
                    UnpackAllSTXCommand.SetValue( "", installationPath + "\\ConvertAll.exe --unpack --format=STX \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack All DAT
                    RegistryKey UnpackAllDATItem = HarmonyToolsShell.CreateSubKey( "3_UnpackAllDAT" );
                    UnpackAllDATItem.SetValue( "MUIVerb", texts[ language ][ "UnpackAllDATName" ] );
                    UnpackAllDATItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico" );

                    RegistryKey UnpackAllDATCommand = UnpackAllDATItem.CreateSubKey( "command" );
                    UnpackAllDATCommand.SetValue( "", installationPath + "\\ConvertAll.exe --unpack -format=DAT \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack All SPC
                    RegistryKey UnpackAllSPCItem = HarmonyToolsShell.CreateSubKey( "4_UnpackAllSPC" );
                    UnpackAllSPCItem.SetValue( "MUIVerb", texts[ language ][ "UnpackAllSPCName" ] );
                    UnpackAllSPCItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-Icon.ico" );

                    RegistryKey UnpackAllSPCCommand = UnpackAllSPCItem.CreateSubKey( "command" );
                    UnpackAllSPCCommand.SetValue( "", installationPath + "\\ConvertAll.exe --unpack --format=SPC \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Unpack All SRD
                    RegistryKey UnpackAllSRDItem = HarmonyToolsShell.CreateSubKey( "5_UnpackAllSRD" );
                    UnpackAllSRDItem.SetValue( "MUIVerb", texts[ language ][ "UnpackAllSRDName" ] );
                    UnpackAllSRDItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Unpack-Icon.ico" );

                    RegistryKey UnpackAllSRDCommand = UnpackAllSRDItem.CreateSubKey( "command" );
                    UnpackAllSRDCommand.SetValue( "", installationPath + "\\ConvertAll.exe --unpack --format=SRD \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );
                    
                    // ----------

                    /**
                     * PACKING
                     */

                    // Pack All Dialogue
                    RegistryKey PackAllDialogueItem = HarmonyToolsShell.CreateSubKey( "6_PackAllDialogue" );
                    PackAllDialogueItem.SetValue( "MUIVerb", texts[ language ][ "PackAllDialogueName" ] );
                    PackAllDialogueItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );

                    RegistryKey PackAllDialogueCommand = PackAllDialogueItem.CreateSubKey( "command" );
                    PackAllDialogueCommand.SetValue( "", installationPath + "\\ConvertAll.exe --pack --format=DIALOGUE \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack All STX
                    RegistryKey PackAllSTXItem = HarmonyToolsShell.CreateSubKey( "7_PackAllSTX" );
                    PackAllSTXItem.SetValue( "MUIVerb", texts[ language ][ "PackAllSTXName" ] );
                    PackAllSTXItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );

                    RegistryKey PackAllSTXCommand = PackAllSTXItem.CreateSubKey( "command" );
                    PackAllSTXCommand.SetValue( "", installationPath + "\\ConvertAll.exe --pack --format=STX \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack All DAT
                    RegistryKey PackAllDATItem = HarmonyToolsShell.CreateSubKey( "8_PackAllDAT" );
                    PackAllDATItem.SetValue( "MUIVerb", texts[ language ][ "PackAllDATName" ] );
                    PackAllDATItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-File-Icon.ico" );

                    RegistryKey PackAllDATCommand = PackAllDATItem.CreateSubKey( "command" );
                    PackAllDATCommand.SetValue( "", installationPath + "\\ConvertAll.exe --pack --format=DAT \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Pack All SPC
                    RegistryKey PackAllSPCItem = HarmonyToolsShell.CreateSubKey( "9_PackAllSPC" );
                    PackAllSPCItem.SetValue( "MUIVerb", texts[ language ][ "PackAllSPCName" ] );
                    PackAllSPCItem.SetValue( "Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico" );

                    RegistryKey PackAllSPCCommand = PackAllSPCItem.CreateSubKey( "command" );
                    PackAllSPCCommand.SetValue( "", installationPath + "\\ConvertAll.exe --pack --format=SPC \"%1\" --pause-after-error " + ( deleteOriginal ? " --delete-original" : "" ) );
                }
                catch ( System.UnauthorizedAccessException ) {
                    Console.WriteLine( "Error: You don't have permission to register the context menu." );
                    Console.WriteLine( "Tip: Try to run this program as administrator" );
                    Console.WriteLine( "Press <Enter> to close this window" );
                    while ( Console.ReadKey().Key != ConsoleKey.Enter ) {}
                    return;
                }
            }
            else {
                Console.WriteLine( "Warning: Context menu for empty space already exists" );
            }
        }

        static void UnregisterContextMenus () {
            if ( doesKeyExists( @"*\shell\HarmonyTools" ) ) {
                Registry.ClassesRoot.DeleteSubKeyTree( @"*\shell\HarmonyTools" );
            }

            if ( doesKeyExists( @"Directory\shell\HarmonyTools" ) ) {
                Registry.ClassesRoot.DeleteSubKeyTree( @"Directory\shell\HarmonyTools" );
            }

            if ( doesKeyExists( @"Directory\Background\shell\HarmonyTools" ) ) {
                Registry.ClassesRoot.DeleteSubKeyTree( @"Directory\Background\shell\HarmonyTools" );
            }
        }
    }
}