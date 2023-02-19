using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyTools.Exceptions;
using HarmonyTools.Extensions;
using HarmonyTools.Formats;
using Microsoft.Win32;

namespace HarmonyTools.Drivers
{
    public class ContextMenuDriver : IDriver
    {
        public static Command GetCommand()
        {
            var driverInstance = new ContextMenuDriver();

            var command = new Command("context-menu", "Manages custom context menu");

            var registerCommand = new Command("register", "Registers context menu");
            var unregisterCommand = new Command("unregister", "Unregisters context menu");

            registerCommand.SetHandler(() => driverInstance.Register());
            unregisterCommand.SetHandler(() => driverInstance.Unregister());

            command.AddCommand(registerCommand);
            command.AddCommand(unregisterCommand);

            return command;
        }

        protected readonly string binaryPath;
        protected readonly string installationPath;

        public ContextMenuDriver()
        {
            binaryPath = Assembly.GetExecutingAssembly().Location;
            installationPath = Path.GetDirectoryName(binaryPath)!;
        }

        protected void Register()
        {
            // csharpier-ignore-start
            Console.WriteLine("WARNING: Note that you should not delete or move HarmonyTools binary file.");
            Console.WriteLine("         Otherwise, context menu will not work properly.");
            Console.WriteLine();
            Console.WriteLine("         If you really need to move it somewhere else, you should unregister context menu first.");
            Console.WriteLine("         Then, move the binary file and register context menu again.");
            // csharpier-ignore-end

            if (
                DoesKeyExists(@"*\shell\HarmonyTools")
                || DoesKeyExists(@"Directory\shell\HarmonyTools")
                || DoesKeyExists(@"Directory\Background\shell\HarmonyTools")
            )
            {
                throw new ContextMenuException("HarmonyTools context menu is already registered.");
            }

            var driverContextMenuDeclarations = new List<IEnumerable<ContextMenuEntry>>()
            {
                DialogueDriver.SetupContextMenu(),
                StxDriver.SetupContextMenu(),
                SpcDriver.SetupContextMenu(),
                SrdDriver.SetupContextMenu(),
                DatDriver.SetupContextMenu(),
                FontDriver.SetupContextMenu(),
                WrdDriver.SetupContextMenu(),
            };

            try
            {
                var htFileRoot = Registry.ClassesRoot.CreateSubKey(@"*\shell\HarmonyTools");
                var htDirRoot = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\HarmonyTools");
                var htDirBgRoot = Registry.ClassesRoot.CreateSubKey(
                    @"Directory\Background\shell\HarmonyTools"
                );

                htFileRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htFileRoot.SetValue("MUIVerb", "Harmony Tools");
                htFileRoot.SetValue("Position", "Top");
                htFileRoot.SetValue("subcommands", "");

                htDirRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htDirRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirRoot.SetValue("Position", "Top");
                htDirRoot.SetValue("subcommands", "");

                htDirBgRoot.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                htDirBgRoot.SetValue("MUIVerb", "Harmony Tools");
                htDirBgRoot.SetValue("Position", "Top");
                htDirBgRoot.SetValue("subcommands", "");

                var htFileShell = htFileRoot.CreateSubKey("shell");
                var htDirShell = htDirRoot.CreateSubKey("shell");
                var htDirBgShell = htDirBgRoot.CreateSubKey("shell");

                foreach (var driverContextMenuDeclaration in driverContextMenuDeclarations)
                {
                    var driverDeclarationCount = driverContextMenuDeclaration.Count();

                    for (int index = 0; index < driverDeclarationCount; index++)
                    {
                        var declaration = driverContextMenuDeclaration.ElementAt(index);
                        var htShell = declaration.ApplyTo.IsDirectory ? htDirShell : htFileShell;
            
                        htShell.RegisterHTCommand(
                            declaration.Command,
                            declaration.Name,
                            Path.Combine(installationPath, declaration.Icon),
                            $"{binaryPath} {declaration.Command} \"%1\"",
                            hasSeparatorBelow: index == driverDeclarationCount - 1
                        );
                    }
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                throw new ContextMenuException(
                    "You do not have permission to register the context menu."
                );
            }

            /*
            try
            {
                RegistryKey HarmonyTools = Registry.ClassesRoot.CreateSubKey(
                    @"Directory\Background\shell\HarmonyTools"
                );

                HarmonyTools.SetValue("Icon", installationPath + @"\Harmony-Tools-Icon.ico");
                HarmonyTools.SetValue("MUIVerb", "Harmony Tools");
                HarmonyTools.SetValue("Position", "Top");
                HarmonyTools.SetValue("subcommands", "");

                RegistryKey HarmonyToolsShell = HarmonyTools.CreateSubKey("shell");

                /**
                 * UNPACKING
                 *\/

                // Unpack All Dialogue
                RegistryKey UnpackAllDialogueItem = HarmonyToolsShell.CreateSubKey(
                    "1_UnpackAllDialogue"
                );
                UnpackAllDialogueItem.SetValue("MUIVerb", texts[language]["UnpackAllDialogueName"]);
                UnpackAllDialogueItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllDialogueCommand = UnpackAllDialogueItem.CreateSubKey(
                    "command"
                );
                UnpackAllDialogueCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=DIALOGUE --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All STX
                RegistryKey UnpackAllSTXItem = HarmonyToolsShell.CreateSubKey("2_UnpackAllSTX");
                UnpackAllSTXItem.SetValue("MUIVerb", texts[language]["UnpackAllSTXName"]);
                UnpackAllSTXItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllSTXCommand = UnpackAllSTXItem.CreateSubKey("command");
                UnpackAllSTXCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=STX --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All DAT
                RegistryKey UnpackAllDATItem = HarmonyToolsShell.CreateSubKey("3_UnpackAllDAT");
                UnpackAllDATItem.SetValue("MUIVerb", texts[language]["UnpackAllDATName"]);
                UnpackAllDATItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-File-Icon.ico"
                );

                RegistryKey UnpackAllDATCommand = UnpackAllDATItem.CreateSubKey("command");
                UnpackAllDATCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack -format=DAT --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All SPC
                RegistryKey UnpackAllSPCItem = HarmonyToolsShell.CreateSubKey("4_UnpackAllSPC");
                UnpackAllSPCItem.SetValue("MUIVerb", texts[language]["UnpackAllSPCName"]);
                UnpackAllSPCItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-Icon.ico"
                );

                RegistryKey UnpackAllSPCCommand = UnpackAllSPCItem.CreateSubKey("command");
                UnpackAllSPCCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=SPC --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Unpack All SRD
                RegistryKey UnpackAllSRDItem = HarmonyToolsShell.CreateSubKey("5_UnpackAllSRD");
                UnpackAllSRDItem.SetValue("MUIVerb", texts[language]["UnpackAllSRDName"]);
                UnpackAllSRDItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Unpack-Icon.ico"
                );
                UnpackAllSRDItem.SetValue("CommandFlags", (uint)0x40, RegistryValueKind.DWord);

                RegistryKey UnpackAllSRDCommand = UnpackAllSRDItem.CreateSubKey("command");
                UnpackAllSRDCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --unpack --format=SRD --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // ----------

                /**
                 * PACKING
                 *\/

                // Pack All Dialogue
                RegistryKey PackAllDialogueItem = HarmonyToolsShell.CreateSubKey(
                    "6_PackAllDialogue"
                );
                PackAllDialogueItem.SetValue("MUIVerb", texts[language]["PackAllDialogueName"]);
                PackAllDialogueItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllDialogueCommand = PackAllDialogueItem.CreateSubKey("command");
                PackAllDialogueCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=DIALOGUE --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All STX
                RegistryKey PackAllSTXItem = HarmonyToolsShell.CreateSubKey("7_PackAllSTX");
                PackAllSTXItem.SetValue("MUIVerb", texts[language]["PackAllSTXName"]);
                PackAllSTXItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllSTXCommand = PackAllSTXItem.CreateSubKey("command");
                PackAllSTXCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=STX --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All DAT
                RegistryKey PackAllDATItem = HarmonyToolsShell.CreateSubKey("8_PackAllDAT");
                PackAllDATItem.SetValue("MUIVerb", texts[language]["PackAllDATName"]);
                PackAllDATItem.SetValue(
                    "Icon",
                    installationPath + @"\Harmony-Tools-Pack-File-Icon.ico"
                );

                RegistryKey PackAllDATCommand = PackAllDATItem.CreateSubKey("command");
                PackAllDATCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=DAT --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );

                // Pack All SPC
                RegistryKey PackAllSPCItem = HarmonyToolsShell.CreateSubKey("9_PackAllSPC");
                PackAllSPCItem.SetValue("MUIVerb", texts[language]["PackAllSPCName"]);
                PackAllSPCItem.SetValue("Icon", installationPath + @"\Harmony-Tools-Pack-Icon.ico");

                RegistryKey PackAllSPCCommand = PackAllSPCItem.CreateSubKey("command");
                PackAllSPCCommand.SetValue(
                    "",
                    installationPath
                        + "\\HTConvertAll.exe --pack --format=SPC --pause-after-error "
                        + (deleteOriginal ? " --delete-original" : "")
                );
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Error: You don't have permission to register the context menu.");
                Console.WriteLine("Tip: Try running this command prompt as administrator");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                return;
            }
            */
        }

        protected void Unregister()
        {
            if (DoesKeyExists(@"*\shell\HarmonyTools"))
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\HarmonyTools");
            }

            if (DoesKeyExists(@"Directory\shell\HarmonyTools"))
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\HarmonyTools");
            }

            if (DoesKeyExists(@"Directory\Background\shell\HarmonyTools"))
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\Background\shell\HarmonyTools");
            }
        }

        protected bool DoesKeyExists(string keyName)
        {
            var key = Registry.ClassesRoot.OpenSubKey(keyName, false);
            return key != null;
        }
    }
}
