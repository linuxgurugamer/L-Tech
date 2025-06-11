/**
 * Based on the InstallChecker from the Kethane mod for Kerbal Space Program.
 * https://github.com/Majiir/Kethane/blob/b93b1171ec42b4be6c44b257ad31c7efd7ea1702/Plugin/InstallChecker.cs
 * 
 * Original is (C) Copyright Majiir.
 * CC0 Public Domain (http://creativecommons.org/publicdomain/zero/1.0/)
 * http://forum.kerbalspaceprogram.com/threads/65395-CompatibilityChecker-Discussion-Thread?p=899895&viewfull=1#post899895
 * 
 * This file has been modified extensively and is released under the same license.
 */
using KSP.Localization;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LtScience
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    internal class Startup : MonoBehaviour
    {
        private void Start()
        {
            string v = Localizer.Format("#LOC_lTech_36");
            AssemblyTitleAttribute attributes = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false);
            string title = attributes?.Title;
            if (title == null)
            {
                title = Localizer.Format("#LOC_lTech_37");
            }
            v = Assembly.GetExecutingAssembly().FullName;
            if (v == null)
            {
                v = Localizer.Format("#LOC_lTech_38");
            }
            Debug.Log(Localizer.Format("#LOC_lTech_39") + title + Localizer.Format("#LOC_lTech_40") + v);
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class InstallChecker : MonoBehaviour
    {
        private const string MODNAME = "L-Tech Science";
        private const string FOLDERNAME = "LTech";
        private const string EXPECTEDPATH = FOLDERNAME + "/Plugins";

        protected void Start()
        {
            // Search for this mod's DLL existing in the wrong location. This will also detect duplicate copies because only one can be in the right place.
            var assemblies = AssemblyLoader.loadedAssemblies.Where(a => a.assembly.GetName().Name == Assembly.GetExecutingAssembly().GetName().Name).Where(a => a.url != EXPECTEDPATH);
            if (assemblies.Any())
            {
                var badPaths = assemblies.Select(a => a.path).Select(p => Uri.UnescapeDataString(new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath)).MakeRelativeUri(new Uri(p)).ToString().Replace('/', Path.DirectorySeparatorChar)));
                PopupDialog.SpawnPopupDialog
                (
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Localizer.Format("#LOC_lTech_41"),
                    Localizer.Format("#LOC_lTech_42") + MODNAME + Localizer.Format("#LOC_lTech_43"),
                    MODNAME + Localizer.Format("#LOC_lTech_44") + FOLDERNAME + Localizer.Format("#LOC_lTech_45") + String.Join(Localizer.Format("#LOC_lTech_46"), badPaths.ToArray()),
                    Localizer.Format("#LOC_lTech_47"),
                    false,
                    HighLogic.UISkin
                );
                Debug.Log(Localizer.Format("#LOC_lTech_48") + MODNAME + Localizer.Format("#LOC_lTech_49") + MODNAME + Localizer.Format("#LOC_lTech_50") + EXPECTEDPATH + Localizer.Format("#LOC_lTech_51") + String.Join(Localizer.Format("#LOC_lTech_52"), badPaths.ToArray())

                     );

            }

            //// Check for Module Manager
            //if (!AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name.StartsWith("ModuleManager") && a.url == ""))
            //{
            //    PopupDialog.SpawnPopupDialog("Missing Module Manager",
            //        modName + " requires the Module Manager mod in order to function properly.\n\nPlease download from http://forum.kerbalspaceprogram.com/threads/55219 and copy to the KSP/GameData/ directory.",
            //        "OK", false, HighLogic.Skin);
            //}

            CleanupOldVersions();
        }

        /*
         * Tries to fix the install if it was installed over the top of a previous version
         */
        void CleanupOldVersions()
        {
            try
            {
            }
            catch (Exception ex)
            {
                Debug.LogError(Localizer.Format("#LOC_lTech_53") + this.GetType().FullName + Localizer.Format("#LOC_lTech_54") + this.GetInstanceID().ToString(Localizer.Format("#LOC_lTech_55")) + Localizer.Format("#LOC_lTech_56") + Time.time.ToString(Localizer.Format("#LOC_lTech_57")) + Localizer.Format("#LOC_lTech_58") +
                   Localizer.Format("#LOC_lTech_59") + ex.Message + Localizer.Format("#LOC_lTech_60") + ex.StackTrace );

            }
        }
    }
}

