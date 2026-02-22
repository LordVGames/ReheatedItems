using R2API;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
namespace ReheatedItems;


internal static class ModLanguage
{
    internal static List<string> LangFilesToLoad = [];

    internal static void AddNewLangTokens()
    {
        string rootLangFolderLocation = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.PluginInfo.Location), "Language");
        foreach (string itemName in LangFilesToLoad)
        {
            string itemLangFileLocation = System.IO.Path.Combine(rootLangFolderLocation, $"{itemName}.json");
            LanguageAPI.AddOverlayPath(itemLangFileLocation);
        }
    }
}