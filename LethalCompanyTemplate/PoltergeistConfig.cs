﻿using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Poltergeist
{
    public class PoltergeistConfig : SyncedConfig2<PoltergeistConfig>
    {
        //Non-synced entries
        public ConfigEntry<bool> DefaultToVanilla { get; private set; }
        public ConfigEntry<float> LightIntensity { get; private set; }
        public ConfigEntry<bool> ShowDebugLogs { get; private set; }
        public ConfigEntry<float> GhostVolume { get; private set; }

        //Synced entries
        [field: SyncedEntryField] public SyncedEntry<float> MaxPower { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> PowerRegen { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<int> AliveForMax { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> TimeForAggro { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<int> HitsForAggro { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> AudioTime { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<string> PesterBlacklist { get; private set; }

        //Cost-related entries
        [field: SyncedEntryField] public SyncedEntry<float> DoorCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> BigDoorCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> NoisyItemCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> ValveCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> ShipDoorCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> CompanyBellCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> PesterCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> ManifestCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> BarkCost { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<float> MiscCost { get; private set; }

        //Feature toggle entries
        [field: SyncedEntryField] public SyncedEntry<bool> EnableDoor { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableBigDoor { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableNoisyItem { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableValve { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableShipDoor { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableCompanyBell { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnablePester { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableManifest { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableAudio { get; private set; }
        [field: SyncedEntryField] public SyncedEntry<bool> EnableMisc { get; private set; }

        /**
         * Make an instance of the config
         */
        public PoltergeistConfig(ConfigFile cfg) : base(Poltergeist.MOD_GUID)
        {
            //Bind the non-synced stuff
            DefaultToVanilla = cfg.Bind(
                new ConfigDefinition("Client-Side", "DefaultToVanilla"),
                false,
                new ConfigDescription(
                    "If true, you will be placed into the default spectate mode when you die."
                    )
                );
            LightIntensity = cfg.Bind(
                new ConfigDefinition("Client-Side", "LightIntensity"),
                8f,
                new ConfigDescription(
                    "The intensity of the ghost light.\n",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            ShowDebugLogs = cfg.Bind(
                new ConfigDefinition("Client-Side", "ShowDebugLogs"),
                false,
                new ConfigDescription(
                    "If true, you will see debug logs."
                    )
                );
            GhostVolume = cfg.Bind(
                new ConfigDefinition("Client-Side", "Ghost Volume"),
                1f,
                new ConfigDescription(
                    "Volume of the audio ghosts make",
                    new AcceptableValueRange<float>(0, 1)
                    )
                );

            //Bind the regular synced stuff
            MaxPower = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Max power"),
                100f,
                new ConfigDescription(
                    "The maximum amount of power that will be available to the ghosts.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            PowerRegen = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Power regen"),
                5f,
                new ConfigDescription(
                    "How much power the ghosts regenerate per second.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            AliveForMax = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Alive for max power"),
                1,
                new ConfigDescription(
                    "The maximum number of players that can be alive for the ghosts to have max power.\n" + 
                    "(As soon as this number or fewer players are left alive, ghosts will be at max power.)",
                    new AcceptableValueRange<int>(0, int.MaxValue)
                    )
                );
            TimeForAggro = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Pester aggro timespan"),
                3f,
                new ConfigDescription(
                    "How many seconds can be between pesterings for an enemy to get mad at a nearby player.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            HitsForAggro = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Aggro hit requirement"),
                2,
                new ConfigDescription(
                    "How many times an enemy has to be pestered in the timespan in order to get mad at a nearby player.",
                    new AcceptableValueRange<int>(1, int.MaxValue)
                    )
                );
            AudioTime = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Audio play time"),
                5f,
                new ConfigDescription(
                    "The maximum time (in seconds) that ghost audio can play before stopping.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            PesterBlacklist = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced", "Pester blacklist"),
                "",
                new ConfigDescription(
                    "Comma separated list of enemy script type names that cannot be pestered.",
                    null
                    )
                );
            Poltergeist.DebugLog($"Loaded pester blacklist: '{PesterBlacklist.Value}'");

            //Bind the cost-related configs
            DoorCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Door cost"),
                10f,
                new ConfigDescription(
                    "The power required to open/close regular doors.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            BigDoorCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Big door cost"),
                50f,
                new ConfigDescription(
                    "The power required to open/close larger doors and mess with the mineshaft elevator.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            NoisyItemCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Noisy item cost"),
                5f,
                new ConfigDescription(
                    "The power required to use noisy items.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            ValveCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Valve cost"),
                20f,
                new ConfigDescription(
                    "The power required to turn valves.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            ShipDoorCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Ship door cost"),
                30f,
                new ConfigDescription(
                    "The power required to use the ship doors.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            CompanyBellCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Company bell cost"),
                15f,
                new ConfigDescription(
                    "The power required to ring the bell at the company building.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            PesterCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Pester cost"),
                20f,
                new ConfigDescription(
                    "The power required to pester enemies.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            ManifestCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Manifest cost"),
                60f,
                new ConfigDescription(
                    "The power required to manifest in the realm of the living.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            BarkCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Audio playing cost"),
                40f,
                new ConfigDescription(
                    "The power required to play audio that the living can hear.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );
            MiscCost = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Costs", "Misc cost"),
                10f,
                new ConfigDescription(
                    "The power required to do any interactions not covered by another section.",
                    new AcceptableValueRange<float>(0, float.MaxValue)
                    )
                );

            //Bind the feature toggles
            EnableDoor = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable door"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot open or close regular doors.",
                    null
                    )
                );
            EnableBigDoor = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable big door"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot interact with large doors or the mineshaft elevator.",
                    null
                    )
                );
            EnableNoisyItem = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable noisy item"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot use noisy items or whoopie cushions.",
                    null
                    )
                );
            EnableValve = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable valve"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot turn steam valves.",
                    null
                    )
                );
            EnableShipDoor = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable ship door"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot use ship doors.",
                    null
                    )
                );
            EnableCompanyBell = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable company bell"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot ring the company bell.",
                    null
                    )
                );
            EnablePester = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable pester"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot pester enemies.",
                    null
                    )
                );
            EnableManifest = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable manifest"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot manifest.",
                    null
                    )
                );
            EnableAudio = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable audio"),
                true,
                new ConfigDescription(
                    "If false, ghosts cannot play audio.",
                    null
                    )
                );
            EnableMisc = cfg.BindSyncedEntry(
                new ConfigDefinition("Synced: Features", "Enable misc"),
                true,
                new ConfigDescription(
                    "If false, miscellaneous ghost interactions are disabled.",
                    null
                    )
                );

            //Register the config
            ConfigManager.Register(this);
        }

        public bool IsEnemyPesterBlocked(string typeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PesterBlacklist.Value))
                    return false;

                string[] blocked = PesterBlacklist.Value.Split(',');
                foreach (string s in blocked)
                {
                    string trimmed = s.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    bool match = typeName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                                 typeName.EndsWith(trimmed, StringComparison.OrdinalIgnoreCase);

                    if (match)
                    {
                        Poltergeist.DebugLog($"Blacklisted enemy type matched: {typeName} matches {trimmed}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Poltergeist.LogError($"Error checking pester blacklist for {typeName}: {ex}");
            }
            return false;
        }
    }
}
