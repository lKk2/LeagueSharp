using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace kShaco2
{
    internal class Program
    {
        // global declarations~ 
        public const string CharName = "Shaco";
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static List<Vector3> BoxPlaces = new List<Vector3>();
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != CharName)
            {
                return;
            }
            Q = new Spell(SpellSlot.Q, 400f);
            W = new Spell(SpellSlot.W, 425f);
            E = new Spell(SpellSlot.E, 625f);
            R = new Spell(SpellSlot.R); // no range needed only mana
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //menu openning
            Config = new Menu(CharName, CharName, true);
            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Passive", "Passive"));
            Config.SubMenu("Passive").AddItem(new MenuItem("put_box", "Place Box Automatic").SetValue(true));


            Config.AddToMainMenu();
            //end menu

            Drawing.OnDraw += OnDraw;
            Game.OnGameUpdate += OnGameUpdate;
            Game.PrintChat("kShaco2# Loaded");
            BoxPlace();
            var templist = (from pos in BoxPlaces
                            let x = pos.X
                            let y = pos.Y
                            let z = pos.Z
                            select new Vector3(x, z, y)).ToList();
            BoxPlaces = templist;
        }

        //ondraw
        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, Color.Aquamarine);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            

            switch (Orbwalker.ActiveMode)
            {
                    case Orbwalking.OrbwalkingMode.Combo:
                        Combo(target);
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass(target);
                        break;
            }

            if (Config.SubMenu("Passive").Item("put_box").GetValue<bool>())
            {
                if (!W.IsReady())
                    return;
                    foreach (var place in BoxPlaces.Where(pos => pos.Distance(ObjectManager.Player.Position) <= W.Range))
                    {
                        W.Cast(place);
                    }
            }
        }

        private static void BoxPlace()
        {
            // Blue Team Places~
            BoxPlaces.Add(new Vector3(3529.24f, 54.65f, 7700.50f));
            BoxPlaces.Add(new Vector3(6397.00f, 51.67f, 5065.00f));
            BoxPlaces.Add(new Vector3(3388.47f, 55.61f, 6168.49f));
            BoxPlaces.Add(new Vector3(7586.97f, 57.00f, 3828.58f));
            BoxPlaces.Add(new Vector3(7445.00f, 55.60f, 3365.00f));
            BoxPlaces.Add(new Vector3(8055.41f, 54.28f, 2671.30f));
            
            // Purple Team Places~
            BoxPlaces.Add(new Vector3(10520.72f, 54.87f, 6927.20f));
            BoxPlaces.Add(new Vector3(7645.00f, 55.20f, 9413.00f));
            BoxPlaces.Add(new Vector3(10580.53f, 65.54f, 7958.30f));
            BoxPlaces.Add(new Vector3(6431.00f, 54.63f, 10535.00f));
            BoxPlaces.Add(new Vector3(6597.55f, 54.63f, 11117.78f));
            BoxPlaces.Add(new Vector3(6143.00f, 39.55f, 11777.00f));
           
        }

        private static void Combo(Obj_AI_Base target)
        {
            

        }

        private static void Harass(Obj_AI_Base target)
        {
            Console.WriteLine("[" + Game.Time + "]Target acquired");
            if (E.IsReady())
            {
                E.CastOnUnit(target);
            }
            if (Q.IsReady())
            {
                Q.Cast(new Vector3(Game.CursorPos.X, Game.CursorPos.Y, Game.CursorPos.Z));
            }
        }
    }
}
