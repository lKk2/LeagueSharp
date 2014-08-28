#region dependecies
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
#endregion

namespace kKayle2
{
   internal class Program
    {
        private const string Champion = "Kayle";
        private static readonly List<Spell> Spellist = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;

       private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

       private static void Game_OnGameLoad(EventArgs args)
       {
           if (Player.ChampionName != Champion)
               return;

           _q = new Spell(SpellSlot.Q, 650f);
           _w = new Spell(SpellSlot.W, 900f);
           _e = new Spell(SpellSlot.E, 0f);
           _r = new Spell(SpellSlot.R, 900f);
           Spellist.AddRange(new[] { _q, _w, _e, _r });

           //Menu
           _config = new Menu(Player.ChampionName, Player.ChampionName, true);
           var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
           SimpleTs.AddToMenu(targetSelectorMenu);
           _config.AddSubMenu(targetSelectorMenu);

           _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
           _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

           //Combo
           _config.AddSubMenu(new Menu("Combo", "combo"));
           _config.SubMenu("combo").AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
           _config.SubMenu("combo").AddItem(new MenuItem("useW", "Use W").SetValue(true));
           _config.SubMenu("combo").AddItem(new MenuItem("useE", "Use E").SetValue(true));
           _config.SubMenu("combo").AddItem(new MenuItem("useR", "Use R").SetValue(true));
           _config.SubMenu("combo").AddItem(new MenuItem("useRpct", "Life to Use ULT").SetValue(new Slider(30, 0, 100)));

           //Healing
           _config.AddSubMenu(new Menu("Healing", "healing"));
           _config.SubMenu("healing").AddItem(new MenuItem("selfW", "Self Heal Percent").SetValue(true));
           _config.SubMenu("healing")
               .AddItem(new MenuItem("hpWpercent", "Percentage of Self Heal").SetValue(new Slider(60, 0, 100)));

           //Ks
           _config.AddSubMenu(new Menu("KS", "ks"));
           _config.SubMenu("ks").AddItem(new MenuItem("ksQ", "Use Q on Killable Targets").SetValue(true));


           // Drawning
           _config.AddSubMenu(new Menu("Drawning", "drawning"));
           _config.SubMenu("drawning").AddItem(new MenuItem("DrawQ", "Draw Q Range").SetValue(true));


           _config.AddToMainMenu(); // add everything

           Game.OnGameUpdate += Game_OnGameUpdate;
           Drawing.OnDraw += Drawing_OnDraw;
           Game.PrintChat("<font color=\"#6699ff\"><b>kKayle Loaded</b></font>");
       }

       private static void Drawing_OnDraw(EventArgs args)
       {
           if (_config.SubMenu("drawning").Item("DrawQ").GetValue<bool>())
           {
               Utility.DrawCircle(Player.Position, _q.Range, Color.Blue);
           }
       }

       private static void Game_OnGameUpdate(EventArgs args)
       {
           if (Player.IsDead) return;
           AutoBots();
           if (_orbwalker.ActiveMode.ToString() == "Combo")
           {
              Combo();
           }
           if (_orbwalker.ActiveMode.ToString() == "Mixed")
           {
              // Mixed();
           }

           if (_orbwalker.ActiveMode.ToString() == "LaneClear")
           {
              Clear();
           }
           if (_orbwalker.ActiveMode.ToString() == "LastHit")
           {
              // LastHit(); 
           }

       }

       private static void Clear()
       {
           if (MinionManager.GetMinions(Player.ServerPosition, 650f).Count > 0 && _e.IsReady())
           {
               _e.Cast();
           }
       }

       private static void Combo()
       {
           var target = SimpleTs.GetTarget(_q.Range, SimpleTs.DamageType.Magical);
           if (_config.SubMenu("combo").Item("useQ").GetValue<bool>() && _q.IsReady())
           {
               _q.Cast(target);
           }
           if (_config.SubMenu("combo").Item("useE").GetValue<bool>() && _e.IsReady())
           {
               _e.Cast();
           }
           if (_config.SubMenu("combo").Item("useW").GetValue<bool>() && (Player.Health / Player.MaxHealth) * 100 >=
               _config.SubMenu("healing").Item("hpWpercent").GetValue<Slider>().Value && _w.IsReady())
           {
               _w.Cast(Player);
           }
           if (_config.SubMenu("combo").Item("useR").GetValue<bool>() && (Player.Health / Player.MaxHealth) * 100 <= _config.SubMenu("combo").Item("useRpct").GetValue<Slider>().Value && _r.IsReady() && Utility.CountEnemysInRange(650) > 0)
           {
               _r.Cast(Player);
           }
       }

       private static void AutoBots()
       {
           if (_config.SubMenu("healing").Item("selfW").GetValue<bool>() && _w.IsReady() &&
               (Player.Health / Player.MaxHealth) * 100 <=
               _config.SubMenu("healing").Item("hpWpercent").GetValue<Slider>().Value && ObjectManager.Player.Spellbook.IsCastingSpell == false)
               _w.Cast(Player);
           if (_config.SubMenu("ks").Item("ksQ").GetValue<bool>())
           {
               foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(_q.Range)))
               {
                   if (_q.IsReady() && hero.Distance(Player) <= _q.Range &&
                       DamageLib.getDmg(hero, DamageLib.SpellType.Q) >= hero.Health)
                   {
                       _q.Cast(hero);
                   }
               }
           }
       }
    }
}
