using System;
using System.Diagnostics;
using System.IO;

namespace Installer {
    class Program {
        static void Main () {
            string installationPath = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles );
            installationPath = installationPath + Path.DirectorySeparatorChar + @"HarmonyTools";
            
            Console.WriteLine( "Trying to create installation directiory..." );
            
            if ( !Directory.Exists( installationPath ) ) {
                try {
                    Directory.CreateDirectory( installationPath );
                }
                catch ( Exception e ) {
                    Console.WriteLine( "Error: Could not create installation directory: " + e.Message );
                    return;
                }

                Console.SetCursorPosition( 0, Console.CursorTop - 1 );
                Console.WriteLine( "Trying to create installation directiory... OK" );
            }

            Console.WriteLine( "Trying to copy bin files..." );

            if ( !Directory.Exists( @".\bin" ) ) { 
                Console.WriteLine( "Error: Could not copy bin files: bin folder doesn't exist" );
                return;
            }

            string[] filesToCopy = Directory.GetFiles(  @".\bin" );

            bool isCopyingSuccess = true;

            foreach ( string filePath in filesToCopy ) {
                string fileName = Path.GetFileName( filePath );

                try {
                    File.Copy( filePath, installationPath + Path.DirectorySeparatorChar + fileName, true );
                }
                catch ( Exception e ) {
                    Console.WriteLine( "Error: Could not copy file \"" + fileName + "\": " + e.Message );
                    isCopyingSuccess = false;
                }
            }

            if ( !isCopyingSuccess ) { 
                return;
            }

            Console.SetCursorPosition( 0, Console.CursorTop - 1 );
            Console.WriteLine( "Trying to copy bin files... OK" );

            Console.WriteLine( "Trying to add installation directory to Enviroment Path..." );

            string pathValue = Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.Machine );

            if ( !pathValue.Contains( installationPath ) ) {
                pathValue += ";" + installationPath;
                Environment.SetEnvironmentVariable( "PATH", pathValue, EnvironmentVariableTarget.Machine );
            }

            Console.SetCursorPosition( 0, Console.CursorTop - 1 );
            Console.WriteLine( "Trying to add installation directory to Enviroment Path... OK" );

            Console.WriteLine( "Installation successful" );
            Console.WriteLine( "You can now delete this directory" );
        }
    }
}