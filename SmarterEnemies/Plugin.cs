using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;

namespace SmarterEnemies {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class SmarterEnemies : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pseudopulse";
        public const string PluginName = "SmarterEnemies";
        public const string PluginVersion = "1.0.0";

        // config
        public static float InteractableAIChance;
        public static float TeleporterAIChance;
        public static float TeleporterTargetingDelay;
        public static bool LetPlayerAlliesInteract;
        public static bool DropOnDeath;

        public static BepInEx.Logging.ManualLogSource ModLogger;

        public void Awake() {
            // set logger
            ModLogger = Logger;

            Tweaks.Interactables.Hook();

            InteractableAIChance = Config.Bind<float>("Interactables:", "Interact AI Chance", 35f, "The chance for an enemy to be able to go after interactables").Value;
            TeleporterAIChance = Config.Bind<float>("Interactables:", "Teleporter Target Chance", 50, "The chance for an enemy to be able to target the teleporter after a period of time").Value;
            TeleporterTargetingDelay = Config.Bind<float>("Interactables:", "Teleporter Delay", 5 * 60f, "The time in seconds before enemies will attempt to target the teleporter").Value;
            LetPlayerAlliesInteract = Config.Bind<bool>("Interactables:", "Player Allies", false, "Let player allies (such as Engineer Turrets) use interactables").Value;
            // DropOnDeath = Config.Bind<bool>("Interactables:", "Drop On Death", true, "Should enemies that open interactables drop their items upon death.").Value;
        }
    }
}