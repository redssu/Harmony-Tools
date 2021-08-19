using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ExplorerExtension {
    class Program {
        public static Dictionary<string, Dictionary<string, string>> texts = new Dictionary<string, Dictionary<string, string>>() {
            { 
                "EN", new Dictionary<string, string>() {  
                    { "UnpackSTXName", "Unpack STX file" },
                    { "UnpackSPCName", "Unpack SPC archive" },
                    { "UnpackSRDName", "Unpack SRD archive (only textures)" },
                    { "UnpackFontsName", "Unpack font file" },

                    { "PackSTXName", "Pack this file to STX file" },
                    { "PackSPCName", "Pack this directory to SPC Archive" },
                    { "PackSRDName", "Pack this directory to SRD Archive" },
                    { "PackFontsName", "Pack this directory to font file" }
                }
            },
            {
                "PL", new Dictionary<string, string>() {
                    { "UnpackSTXName", "Rozpakuj plik STX" },
                    { "UnpackSPCName", "Rozpakuj archiwum SPC" },
                    { "UnpackSRDName", "Rozpakuj archiwum SRD (tylko tekstury)" },
                    { "UnpackFontsName", "Rozpakuj plik czcionki" },

                    { "PackSTXName", "Spakuj ten plik jako plik STX" },
                    { "PackSPCName", "Spakuj ten katalog jako archiwum SPC" },
                    { "PackSRDName", "Spakuj ten katalog jako archiwum SRD" },
                    { "PackFontsName", "Spakuj ten katalog jako plik czcionki" }
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
                    HarmonyTools.SetValue( "subcommands", "" );

                    RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey( "shell" );

                    // Context submenu items
                    // STX
                    RegistryKey UnpackSTXItem = HarmonyToolsShell.CreateSubKey( "UnpackSTX" );
                    UnpackSTXItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSTXName" ] );

                    RegistryKey UnpackSTXCommand = UnpackSTXItem.CreateSubKey( "command" );
                    UnpackSTXCommand.SetValue( "", installationPath + "\\HTStx.exe --unpack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );

                    // SPC
                    RegistryKey UnpackSPCItem = HarmonyToolsShell.CreateSubKey( "UnpackSPC" );
                    UnpackSPCItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSPCName" ] );

                    RegistryKey UnpackSPCCommand = UnpackSPCItem.CreateSubKey( "command" );
                    UnpackSPCCommand.SetValue( "", installationPath + "\\HTSpc.exe --unpack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );

                    // SRD
                    RegistryKey UnpackSRDItem = HarmonyToolsShell.CreateSubKey( "UnpackSRD" );
                    UnpackSRDItem.SetValue( "MUIVerb", texts[ language ][ "UnpackSRDName" ] );

                    RegistryKey UnpackSRDCommand = UnpackSRDItem.CreateSubKey( "command" );
                    UnpackSRDCommand.SetValue( "", installationPath + "\\HTSrd.exe --unpack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Fonts
                    RegistryKey UnpackFontsItem = HarmonyToolsShell.CreateSubKey( "UnpackFonts" );
                    UnpackFontsItem.SetValue( "MUIVerb", texts[ language ][ "UnpackFontsName" ] );

                    RegistryKey UnpackFontsCommand = UnpackFontsItem.CreateSubKey( "command" );
                    UnpackFontsCommand.SetValue( "", installationPath + "\\HTFont.exe --unpack \"%1\"" );

                    // Pack TXT file
                    RegistryKey PackSTXItem = HarmonyToolsShell.CreateSubKey( "PackSTX" );
                    PackSTXItem.SetValue( "MUIVerb", texts[ language ][ "PackSTXName" ] );

                    RegistryKey PackSTXCommand = PackSTXItem.CreateSubKey( "command" );
                    PackSTXCommand.SetValue( "", installationPath + "\\HTStx.exe --pack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );
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
                    HarmonyTools.SetValue( "subcommands", "" );

                    RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey( "shell" );

                    // Context submenu items
                    // SPC
                    RegistryKey PackSPCItem = HarmonyToolsShell.CreateSubKey( "PackSPC" );
                    PackSPCItem.SetValue( "MUIVerb", texts[ language ][ "PackSPCName" ] );

                    RegistryKey PackSPCCommand = PackSPCItem.CreateSubKey( "command" );
                    PackSPCCommand.SetValue( "", installationPath + "\\HTSpc.exe --pack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );

                    // SRD
                    RegistryKey PackSRDItem = HarmonyToolsShell.CreateSubKey( "PackSRD" );
                    PackSRDItem.SetValue( "MUIVerb", texts[ language ][ "PackSRDName" ] );

                    RegistryKey PackSRDCommand = PackSRDItem.CreateSubKey( "command" );
                    PackSRDCommand.SetValue( "", installationPath + "\\HTSrd.exe --pack \"%1\"" + ( deleteOriginal ? " --delete-original" : "" ) );

                    // Fonts
                    RegistryKey PackFontsItem = HarmonyToolsShell.CreateSubKey( "PackFonts" );
                    PackFontsItem.SetValue( "MUIVerb", texts[ language ][ "PackFontsName" ] );

                    RegistryKey PackFontsCommand = PackFontsItem.CreateSubKey( "command" );
                    PackFontsCommand.SetValue( "", installationPath + "\\HTFont.exe --pack \"%1\"" );
                }
                catch ( System.UnauthorizedAccessException ) {
                    Console.WriteLine( "Error: You don't have permission to register the context menu." );
                    Console.WriteLine( "Tip: Try to run this program as administrator" );
                    return;
                }
            }
            else {
                Console.WriteLine( "Warning: Context menu for directories already exists" );
            }
        }

        static void UnregisterContextMenus () {
            if ( doesKeyExists( @"*\shell\HarmonyTools" ) ) {
                Registry.ClassesRoot.DeleteSubKeyTree( @"*\shell\HarmonyTools" );
            }

            if ( doesKeyExists( @"Directory\shell\HarmonyTools" ) ) {
                Registry.ClassesRoot.DeleteSubKeyTree( @"Directory\shell\HarmonyTools" );
            }
        }
    }
}