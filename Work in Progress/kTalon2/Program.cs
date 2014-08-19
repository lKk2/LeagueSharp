#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp.Common;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace kTalon2
{
    internal class Program
    {
        private const string Champion = "Talon";
        private static readonly List<Spell> Spellist = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;
        private static Items.Item _dfg, _tmt, _rah;
        private static SpellSlot _igniteSlot;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != Champion)
                return;

            _igniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            _dfg = new Items.Item(3128, 750f); // dfg orly?
            _tmt = new Items.Item(3077, 400f); // tiamat
            _rah = new Items.Item(3074, 400f); // hydra

            _q = new Spell(SpellSlot.Q, 250f);
            _w = new Spell(SpellSlot.W, 600f);
            _e = new Spell(SpellSlot.E, 700f);
            _r = new Spell(SpellSlot.R, 500f);
            

            // fine tune of spells~


            _w.SetSkillshot(50f, 0f, 0f, false, Prediction.SkillshotType.SkillshotCone); // not ready yet
            Spellist.AddRange(new[] { _q, _w, _e, _r });



            // Menu 
            _config = new Menu(Player.ChampionName, Player.ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
        }
    }
}
