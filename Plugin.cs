using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    [HarmonyPatch(typeof(EnemyEmoteHandler))]
    public static class SquelchPatcher
    {
        private static readonly FieldInfo SquelchedField = AccessTools.Field(typeof(EnemyEmoteHandler), "m_squelched");

        private static EnemyEmoteHandler appliedTo;

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void AwakePostfix(EnemyEmoteHandler __instance)
        {
            TryApply(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSquelched", typeof(int))]
        public static void IsSquelchedPrefix(EnemyEmoteHandler __instance)
        {
            TryApply(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("ShowEmotes")]
        public static void ShowEmotesPrefix(EnemyEmoteHandler __instance)
        {
            TryApply(__instance);
        }

        private static void TryApply(EnemyEmoteHandler handler)
        {
            if (handler == null || appliedTo == handler)
            {
                return;
            }

            if (SquelchedField == null)
            {
                Plugin.Logger.LogError("EnemyEmoteHandler.m_squelched was not found; auto-squelch was not applied");
                return;
            }

            object squelched = SquelchedField.GetValue(handler);
            if (squelched == null)
            {
                return;
            }

            GameState gameState = GameState.Get();
            GameMgr gameMgr = GameMgr.Get();
            if (gameState == null || gameMgr == null)
            {
                return;
            }

            if (gameMgr.IsBattlegrounds())
            {
                int localPlayerId = gameState.GetFriendlyPlayerId();
                if (localPlayerId == 0)
                {
                    return;
                }

                foreach (int playerId in SnapshotKeys(squelched))
                {
                    if (playerId != localPlayerId)
                    {
                        SetSquelched(squelched, playerId, true);
                    }
                }
            }
            else
            {
                int opposingPlayerId = gameState.GetOpposingPlayerId();
                if (opposingPlayerId == 0 || !ContainsPlayer(squelched, opposingPlayerId))
                {
                    return;
                }

                SetSquelched(squelched, opposingPlayerId, true);
            }

            appliedTo = handler;
            Plugin.Logger.LogInfo("Auto-squelched opponents for this game");
        }

        private static List<int> SnapshotKeys(object squelched)
        {
            var keys = new List<int>();
            var keyCollection = squelched.GetType().GetProperty("Keys")?.GetValue(squelched) as IEnumerable;
            if (keyCollection == null)
            {
                return keys;
            }

            foreach (object key in keyCollection)
            {
                keys.Add((int)key);
            }

            return keys;
        }

        private static bool ContainsPlayer(object squelched, int playerId)
        {
            return (bool)squelched.GetType().GetMethod("ContainsKey").Invoke(squelched, new object[] { playerId });
        }

        private static void SetSquelched(object squelched, int playerId, bool value)
        {
            squelched.GetType().GetProperty("Item").SetValue(squelched, value, new object[] { playerId });
        }
    }
}
