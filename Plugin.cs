using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using BepInEx.Configuration;

namespace FirestoneAutoSquelch
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private ConfigEntry<string> downloadLink;
        private ConfigEntry<string> guid;
        private ConfigEntry<string> name;
        private ConfigEntry<string> version;
        private ConfigEntry<string> description;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Config.Bind("General", "Name", MyPluginInfo.PLUGIN_NAME);
            Config.Bind("General", "Guid", MyPluginInfo.PLUGIN_GUID);
            Config.Bind("General", "Version", MyPluginInfo.PLUGIN_VERSION);
            Config.Bind("General", "DownloadLink", "https://github.com/Zero-to-Heroes/firestone-bepinex-auto-squelch");
            Config.Bind("General", "Description", "Automatically hide all emotes sent by your opponents");

            var harmony = new Harmony("com.firestoneapp.FirestoneAutoSquelch");
            harmony.PatchAll();
            Logger.LogInfo($"Patched harmony");
        }
    }

    [HarmonyPatch(typeof(EnemyEmoteHandler), "IsSquelched", new Type[] { typeof(int) })]
    public static class SquelchPatcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            Plugin.Logger.LogInfo($"Patching IsSquelched: {__result}");
            __result = true;
            Plugin.Logger.LogInfo($"Returning instead: {__result}");
            return false;
        }
    }
}