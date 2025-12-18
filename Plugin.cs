using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTPC
{ // will be replaced by assemblyName if desired
    [BepInPlugin("com.andme.rtpc", "RTPC", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string TargetSceneName = "LevelEditor2";

        internal bool IsModEnabled;
        internal LEV_LevelEditorCentral LevelEditorCentral;

        internal List<BlockProperties> blockList;

        internal static ManualLogSource MyLogger;
        private Harmony harmony;

        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            MyLogger = Logger;

            harmony = new Harmony("com.andme.rtpc");
            harmony.PatchAll();

            MyLogger.LogInfo("Plugin com.andme.rtpc is loaded!");

            ModConfig.Initialize(Config);

            FilterCache.Refresh();

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                try
                {
                    OnSceneLoaded(scene, mode);
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"OnSceneLoaded exception: {ex}");
                }
            };
        }

        private void Update()
        {
            if (!IsModEnabled) return;

            if (!LevelEditorCentral) return;

            if (Input.GetKeyDown(ModConfig.Randomize.Value) && !LevelEditorCentral.input.inputLocked)
            {
                if (LevelEditorCentral.gizmos.isGrabbing && !LevelEditorCentral.gizmos.isDragging && LevelEditorCentral.selection != null)
                {
                    if (LevelEditorCentral.selection.list.Count > 0)
                    {
                        MyLogger.LogInfo("Grabbing a block/selection, placing it before randomizing");
                        PlaceSelection();
                    }
                }

                RandomizeBlock();
            }

        }

        private void PlaceSelection()
        {
            LEV_GizmoHandler gizmos = LevelEditorCentral.gizmos;
            gizmos.creatingNewBlockLock = false;
            if (gizmos.central.manager != null)
            {
                gizmos.central.manager.steamAchiever.AddIntToStat("stat_blocks_placed", gizmos.central.selection.list.Count);
            }
            List<string> after3 = gizmos.central.undoRedo.ConvertBlockListToJSONList(gizmos.central.selection.list);
            List<string> afterSelection3 = gizmos.central.undoRedo.ConvertSelectionToStringList(gizmos.central.selection.list);
            gizmos.GoOutOfGMode();
            gizmos.central.validation.BreakLock(gizmos.central.undoRedo.ConvertBeforeAndAfterListToCollection(gizmos.gModeBeforeList, after3, gizmos.central.selection.list, gizmos.gModeBefore_selectionUIDs, afterSelection3), "RTPC_Mod");
        }

        private void RandomizeBlock()
        {
            MyLogger.LogInfo($"Randomizing Block!");

            if (blockList == null || blockList.Count == 0)
            {
                MyLogger.LogError("Block list is empty or null.");
                return;
            }

            int index = UnityEngine.Random.Range(0, blockList.Count);

            BlockProperties selectedBlock = blockList[index];

            MyLogger.LogInfo($"Selected Block Index: {index}");
            MyLogger.LogInfo($"Selected Block ID: {selectedBlock.blockID}");

            LevelEditorCentral.gizmos.CreateNewBlock(selectedBlock.blockID);

        }



        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == TargetSceneName)
            {
                LevelEditorCentral = FindObjectOfType<LEV_LevelEditorCentral>();

                if (!LevelEditorCentral)
                {
                    MyLogger.LogError("LEV_LevelEditorCentral not found in the Level Editor scene.");
                    return;
                }

                if (!LevelEditorCentral.inspector || !LevelEditorCentral.inspector.globalBlockList)
                {
                    MyLogger.LogError("Global Block List not found in LevelEditorCentral inspector.");
                    return;
                }

                blockList = BuildFilteredBlockList(LevelEditorCentral.inspector.globalBlockList.globalBlocksFolder);
                MyLogger.LogInfo($"Collected {blockList.Count} blocks for randomization.");

                IsModEnabled = true;
            }
            else
            {
                IsModEnabled = false;
            }
        }

        private IEnumerable<BlocksFolder> FindIncludedRoots(BlocksFolder globalRoot, HashSet<string> includedIds)
        {
            var result = new List<BlocksFolder>();

            void Traverse(BlocksFolder folder)
            {
                if (folder == null)
                    return;

                if (includedIds.Contains(folder.folderID))
                    result.Add(folder);

                foreach (var sub in folder.folders)
                    Traverse(sub);
            }

            Traverse(globalRoot);
            return result;
        }

        void CollectBlocksFromFolder(BlocksFolder folder, List<BlockProperties> result, int depth = 0)
        {
            if (folder == null)
                return;

            // Exclude folder after inclusion
            if (FilterCache.ExcludedFolders.Contains(folder.folderID))
                return;

            string indent = new string(' ', depth * 2);
            LogDebug($"{indent}📁 Folder: {folder.folderID}");

            foreach (var block in folder.blocks)
            {
                if (block == null)
                    continue;

                if (FilterCache.ExcludedBlocks.Contains(block.blockID))
                    continue;

                result.Add(block);

                LogDebug($"{indent}  🧱 Block: {block.name}");
            }

            foreach (var sub in folder.folders)
            {
                CollectBlocksFromFolder(sub, result, depth + 1);
            }
        }
        private void CollectIncludedBlocks(BlocksFolder folder,List<BlockProperties> result,HashSet<int> alreadyAdded)
        {
            if (folder == null)
                return;

            foreach (var block in folder.blocks)
            {
                if (block == null)
                    continue;

                if (!FilterCache.IncludedBlocks.Contains(block.blockID))
                    continue;

                if (alreadyAdded.Contains(block.blockID))
                    continue;

                result.Add(block);
                alreadyAdded.Add(block.blockID);

                LogDebug($"⭐ Forced Include Block: {block.name} ({block.blockID})");
            }

            foreach (var sub in folder.folders)
            {
                CollectIncludedBlocks(sub, result, alreadyAdded);
            }
        }


        private List<BlockProperties> BuildFilteredBlockList(BlocksFolder globalRoot)
        {
            var result = new List<BlockProperties>();
            var addedBlockIds = new HashSet<int>();

            var includedRoots = FindIncludedRoots(
                globalRoot,
                FilterCache.IncludedFolders
            );

            foreach (var root in includedRoots)
            {
                CollectBlocksFromFolder(root, result);
            }

            foreach (var block in result)
            {
                addedBlockIds.Add(block.blockID);
            }

            CollectIncludedBlocks(globalRoot, result, addedBlockIds);

            return result;
        }

        internal static void UpdateBlockList()
        {
            if (Instance == null || !Instance.LevelEditorCentral || !Instance.LevelEditorCentral.inspector || !Instance.LevelEditorCentral.inspector.globalBlockList)
                return;
            Instance.blockList = Instance.BuildFilteredBlockList(
                Instance.LevelEditorCentral.inspector.globalBlockList.globalBlocksFolder
            );
            MyLogger.LogInfo($"Block list updated. Total blocks available for randomization: {Instance.blockList.Count}");
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
        internal static void LogDebug(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                MyLogger.LogInfo(message);
            }
        }


    }
    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<KeyCode> Randomize;
        public static ConfigEntry<String> IncludedFoldersRaw;
        public static ConfigEntry<String> ExcludedFoldersRaw;
        public static ConfigEntry<String> ExcludedBlocksRaw;
        public static ConfigEntry<String> IncludedBlocksRaw;
        public static ConfigEntry<bool> DebugLogging;



        public static List<string> IncludedFolders => ParseStringList(IncludedFoldersRaw.Value);
        public static List<string> ExcludedFolders => ParseStringList(ExcludedFoldersRaw.Value);
        public static List<int> ExcludedBlocks => ParseIntList(ExcludedBlocksRaw.Value);
        public static List<int> IncludedBlocks => ParseIntList(IncludedBlocksRaw.Value);


        // Constructor that takes a ConfigFile instance from the main class
        public static void Initialize(ConfigFile config)
        {
            Randomize = config.Bind("1. Keybinds", "1.1 Randomize", KeyCode.None,
                "Key to generate a random piece");
            IncludedFoldersRaw = config.Bind("2. Filter", "2.1 Included Folders", "100;200;300",
                "List of folder IDs to include in randomization");
            ExcludedFoldersRaw = config.Bind("2. Filter", "2.2 Excluded Folders", "102;107;108",
                "List of folder IDs to exclude from randomization");
            ExcludedBlocksRaw = config.Bind("2. Filter", "2.3 Excluded Blocks", "",
                "List of block IDs to exclude from randomization");
            IncludedBlocksRaw = config.Bind("2. Filter", "2.4 Included Blocks", "",
                "List of block IDs to include in randomization (overrides folder filters)");
            DebugLogging = config.Bind("3. Debug", "3.1 Enable Debug Logging", false,
                "Enable detailed debug logging to help with troubleshooting");

            IncludedFoldersRaw.SettingChanged += onFilterChanged;
            ExcludedFoldersRaw.SettingChanged += onFilterChanged;
            ExcludedBlocksRaw.SettingChanged += onFilterChanged;
            IncludedBlocksRaw.SettingChanged += onFilterChanged;
        }


        private static void onFilterChanged(object sender, System.EventArgs e)
        {
            FilterCache.Refresh();
            Plugin.UpdateBlockList();
        }

        private static List<int> ParseIntList(string value)
        {
            return value
                .Split(';')
                .Select(x => x.Trim())
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();
        }

        private static List<string> ParseStringList(string value)
        {
            return value
                .Split(';')
                .Select(x => x.Trim())
                .Distinct()
                .ToList();
        }
        private void OnDestroy()
        {
            IncludedFoldersRaw.SettingChanged -= onFilterChanged;
            ExcludedFoldersRaw.SettingChanged -= onFilterChanged;
            ExcludedBlocksRaw.SettingChanged -= onFilterChanged;
            IncludedBlocksRaw.SettingChanged -= onFilterChanged;
        }

    }

    static class FilterCache
    {
        public static HashSet<string> IncludedFolders;
        public static HashSet<string> ExcludedFolders;
        public static HashSet<int> ExcludedBlocks;
        public static HashSet<int> IncludedBlocks;

        public static void Refresh()
        {
            IncludedFolders = new HashSet<string>(ModConfig.IncludedFolders);
            ExcludedFolders = new HashSet<string>(ModConfig.ExcludedFolders);
            ExcludedBlocks = new HashSet<int>(ModConfig.ExcludedBlocks);
            IncludedBlocks = new HashSet<int>(ModConfig.IncludedBlocks);
        }
    }

}
