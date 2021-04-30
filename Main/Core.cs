using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroodyHen.Main
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        public static Core Plugin { get; private set; }

        private CachedValue<bool> ingameUIisVisible;
        private static readonly ConcurrentDictionary<NormalInventoryItem, RectangleF> incubatedItems =
            new ConcurrentDictionary<NormalInventoryItem, RectangleF>();

        private static readonly List<InventoryIndex> slots = new List<InventoryIndex>()
        { 
            InventoryIndex.Amulet,
            InventoryIndex.Belt,
            InventoryIndex.Boots,
            InventoryIndex.Chest,
            InventoryIndex.Gloves,
            InventoryIndex.Helm,
            InventoryIndex.LRing,
            InventoryIndex.RRing,
            InventoryIndex.LWeapon,
            InventoryIndex.RWeapon,
            InventoryIndex.LWeaponSwap,
            InventoryIndex.RWeaponSwap
        };

        public override void OnLoad()
        {
            CanUseMultiThreading = true;
            Name = "Broody Hen";
        }

        public override bool Initialise()
        {
            var _ingameUI = GameController.IngameState.IngameUi;
            ingameUIisVisible = new TimeCache<bool>(() =>
            {
                return _ingameUI.SyndicatePanel.IsVisibleLocal
                       || _ingameUI.TreePanel.IsVisibleLocal
                       || _ingameUI.Atlas.IsVisibleLocal;
            }, 250);

            Plugin = this;
            return true;
        }

        public override Job Tick()
        {
            if (Settings.MultiThreading)
                return GameController.MultiThreadManager.AddJob(TickLogic, Name);

            TickLogic();

            return null;
        }

        public override void Render()
        {
            if (ingameUIisVisible.Value) return;

            foreach (var item in incubatedItems.Keys)
                Graphics.DrawFrame(incubatedItems[item], Color.YellowGreen, 4);
        }

        private void TickLogic()
        {
            var _inventories = GameController.Game.IngameState.IngameUi.InventoryPanel;
            var _incubatedItemsList = new List<NormalInventoryItem>();

            if (_inventories.IsVisibleLocal)
                foreach (var index in slots)
                    _incubatedItemsList.AddRange(_inventories[index].VisibleInventoryItems
                    .Where(i => i?.Item?.GetComponent<Mods>()?.IncubatorName == ""
                                || i?.Item?.GetComponent<Mods>()?.IncubatorName == null)
                    );

            foreach (var key in incubatedItems.Keys.Where(k => !_incubatedItemsList.Contains(k)))
                incubatedItems.TryRemove(key, out _);

            foreach (var item in _incubatedItemsList)
            {
                if (Settings.Debug) LogMessage($"{Name}: {item.Item.Metadata}");

                var _rectangle = item?.GetClientRect();

                if (_rectangle == null) continue;

                incubatedItems.AddOrUpdate(item, (RectangleF)_rectangle, (key, oldLValue) => (RectangleF)_rectangle);
            }
        }
    }
}
