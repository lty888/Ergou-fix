#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace Vi
{
    internal class Program
    {
        public const string ChampionName = "Vi";
        private static readonly Obj_AI_Hero vPlayer = ObjectManager.Player;
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell E2;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;
        private static SpellSlot FlashSlot;

        public static float FlashRange = 450f;
        public static float SmiteRange = 700f;
        public static int DelayTick = 0;

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        private static Menu MenuTargetedItems;
        private static Menu MenuNonTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (vPlayer.BaseSkinName != "Vi") return;
            if (vPlayer.IsDead) return;

            Q = new Spell(SpellSlot.Q, 860f);
            E = new Spell(SpellSlot.E, 235f);
            E2 = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 800f);

            Q.SetSkillshot(0.5f, 75f, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetTargetted(0.15f, 1500f);

            Q.SetCharged("ViQ", "ViQ", 100, 860, 1f);

            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");
            SmiteSlot = vPlayer.GetSpellSlot("SummonerSmite");
            FlashSlot = vPlayer.GetSpellSlot("SummonerFlash");

            //Create the menu
            Config = new Menu("xQx | 蔚", "Vi", true);

            var targetSelectorMenu = new Menu("目标 选择", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("走砍", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("连招", "Combo"));

            /* [ Don't Use Ult ] */
            Config.SubMenu("Combo").AddSubMenu(new Menu("禁用 R", "DontUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != vPlayer.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            /* [ Find Him in Team Fight ] */
            Config.SubMenu("Combo").AddSubMenu(new Menu("团战锁定目标", "FindHim"));
            Config.SubMenu("Combo").SubMenu("FindHim")
                .AddItem(new MenuItem("ForceFocusctive", "启动强制锁定目标").SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != vPlayer.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("FindHim")
                    .AddItem(new MenuItem("FindHim" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            Menu comboUseQ = new Menu("Q 设置", "comboUseQ");
            Config.SubMenu("Combo").AddSubMenu(comboUseQ);
            comboUseQ.AddItem(new MenuItem("UseQCombo", "使用 Q").SetValue(true));
            comboUseQ.AddItem(new MenuItem("UseQComboDontUnderTurret", "塔下禁用 Q").SetValue(true));

            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "使用 E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "使用 R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboFlashQActive", "连招 闪现+Q!").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "连招!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("骚扰", "Harass"));
            Menu harassUseQ = new Menu("Q 设置", "harassUseQ");
            Config.SubMenu("Harass").AddSubMenu(harassUseQ);
            harassUseQ.AddItem(new MenuItem("UseQHarass", "使用 Q").SetValue(true));
            harassUseQ.AddItem(new MenuItem("UseQHarassDontUnderTurret", "塔下禁止 Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "使用 E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "骚扰最低蓝量: ")
                .SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "骚扰").SetValue(new KeyBind("C".ToCharArray()[0],
                        KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("清线", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "使用 Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "使用 E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "清线最低蓝量: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "清线").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("清野", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "使用 Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "使用 E").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "自动 惩戒")
                .SetValue<KeyBind>(new KeyBind('N', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "清野最低蓝量: ")
                .SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "清野").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            // Extras
            //Config.AddSubMenu(new Menu("Extras", "Extras"));
            //Config.SubMenu("Extras").AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            // Extras -> Use Items 
            MenuExtras = new Menu("额外", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "中断法术").SetValue(true));

            Menu menuUseItems = new Menu("使用 物品", "menuUseItems");
            Config.SubMenu("Extras").AddSubMenu(menuUseItems);
            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("攻击 物品", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            
            MenuTargetedItems.AddItem(new MenuItem("item3153", "破败").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3143", "兰盾").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "比尔吉沃特弯刀").SetValue(true));

            MenuTargetedItems.AddItem(new MenuItem("item3146", "科技枪").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "冰锤 ").SetValue(true));

            // Extras -> Use Items -> AOE Items
            MenuNonTargetedItems = new Menu("AOE 物品", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "奥丁面纱").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "神圣之剑").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "贪欲九头蛇").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3077", "提亚马特").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "幽梦之灵").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("范围", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q 范围").SetValue(new Circle(true,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E 范围").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R 范围").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("SmiteRange", "自动 惩戒").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("FQRange", "闪现+Q 范围").SetValue(new Circle(false,
                System.Drawing.Color.FromArgb(0xFF, 0xCC, 0x00))));

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>",
                    ChampionName));

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color);
            }

            var drawFqCombo = Config.Item("FQRange").GetValue<Circle>();
            if (drawFqCombo.Active)
            {
                Utility.DrawCircle(vPlayer.Position, Q.Range + FlashRange, drawFqCombo.Color);
            }

            var drawSmite = Config.Item("SmiteRange").GetValue<Circle>();
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active && drawSmite.Active)
            {
                Utility.DrawCircle(vPlayer.Position, SmiteRange, drawSmite.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100)) return;

            if (DelayTick - Environment.TickCount <= 250)
            {
                UseSummoners();
                DelayTick = Environment.TickCount;
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("ComboFlashQActive").GetValue<KeyBind>().Active)
            {
                ComboFlashQ();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
                if (vPlayer.Mana >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (vPlayer.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = vPlayer.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (vPlayer.Mana >= existsMana)
                    JungleFarm();
            }
        }

        private static void Combo()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            var e2Target = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.Physical);

            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var comboDamage = rTarget != null ? GetComboDamage(rTarget) : 0;
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            if (qTarget != null && Q.IsReady() && useQ)
            {
                if (Q.IsCharging)
                {
                    if (useQDontUnderTurret)
                    {
                        if (!Utility.UnderTurret(qTarget))
                            Q.Cast(qTarget);
                    }
                    else
                        Q.Cast(qTarget);
                }
                else
                {
                    Q.StartCharging();
                }
            }

            if (eTarget != null)
                UseItems(eTarget);

            if (rTarget != null && comboDamage > rTarget.Health &&
                IgniteSlot != SpellSlot.Unknown &&
                vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                vPlayer.Spellbook.CastSpell(IgniteSlot, rTarget);
            }

            if (E.IsReady() && useE)
            {
                UseItems(e2Target);
                if (eTarget != null)
                    E.Cast();
                else
                    if (e2Target != null && EMinion != null)
                        E.Cast();
            }

            if (rTarget != null && R.IsReady())
            {
                useR = (Config.Item("DontUlt" + rTarget.BaseSkinName) != null &&
                        Config.Item("DontUlt" + rTarget.BaseSkinName).GetValue<bool>() == false) && useR;

                var rDamage = vPlayer.GetSpellDamage(rTarget, SpellSlot.R);
                var qDamage = vPlayer.GetSpellDamage(rTarget, SpellSlot.Q);
                var eDamage = vPlayer.GetSpellDamage(rTarget, SpellSlot.E); //* E.get .Instance.Ammo;

                if (qTarget != null && qTarget.IsValidTarget(Q.Range) && qTarget.Health < qDamage)
                    return;

                if (eTarget != null && (eTarget.IsValidTarget(E.Range) && eTarget.Health < eDamage))
                    return;

                if (qTarget != null && Q.IsReady() && E.IsReady() && qTarget.Health < qDamage + eDamage)
                    return;

                if (useR && rTarget != null && rTarget.Health > rDamage)
                {
                    if (Q.IsReady() && E.IsReady() && rTarget.Health < rDamage + qDamage + eDamage)
                        R.CastOnUnit(rTarget);
                    if (E.IsReady() && rTarget.Health < rDamage + eDamage)
                        R.CastOnUnit(rTarget);
                    if (Q.IsReady() && rTarget.Health < rDamage + qDamage)
                        R.CastOnUnit(rTarget);
                }
            }
        }
        private static void ComboFlashQ()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
			
            var fqTarget = TargetSelector.GetTarget(Q.Range + FlashRange, TargetSelector.DamageType.Physical);
            
            if (vPlayer.Distance(fqTarget) > Q.Range && fqTarget != null)
            {
                if (FlashSlot != SpellSlot.Unknown &&
                    vPlayer.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
                {
                    if (Q.IsCharging && Q.Range >= Q.ChargedMaxRange)
                    {
                        vPlayer.Spellbook.CastSpell(FlashSlot, fqTarget.ServerPosition);
                        Q.Cast(fqTarget);
                    }
                    else
                    {
                        Q.StartCharging();
                    }
                }
            }
        }
        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            var useQDontUnderTurret = Config.Item("UseQHarassDontUnderTurret").GetValue<bool>();
            if (qTarget != null && Q.IsReady() && useQ)
            {
                if (useQDontUnderTurret)
                {
                    if (!Utility.UnderTurret(qTarget))
                        Q.Cast(qTarget);
                }
                else
                    Q.Cast(qTarget);
            }

            if (eTarget != null && E.IsReady() && useE)
            {
                E.Cast();
            }
        }

        private static void JungleFarm()
        {
            if (!Config.Item("JungleFarmActive").GetValue<KeyBind>().Active) return;

            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E2.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];
            if (useE && E.IsReady())
            {
                E.Cast();
            }
            else if (useQ && Q.IsReady())
            {
                if (!Q.IsCharging)
                    Q.StartCharging();
                else
                    Q.Cast(mob);
            }
        }

        private static void LaneClear()
        {
            if (!Config.Item("LaneClearActive").GetValue<KeyBind>().Active) return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.ChargedMaxRange);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E2.Range);

            if (useQ && Q.IsReady())
            {
                if (Q.IsCharging)
                {
                    var locQ = Q.GetLineFarmLocation(allMinionsQ);
                    if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < Q.Range) &&
                        locQ.MinionsHit > 2 && locQ.Position.IsValid()) 
                        Q.Cast(locQ.Position);
                }
                else if (allMinionsQ.Count > 2)
                    Q.StartCharging();
            }

            if (useE && E.IsReady())
            {
                var locE = E.GetLineFarmLocation(allMinionsE);
                if (allMinionsQ.Count == allMinionsQ.Count(m => vPlayer.Distance(m) < E2.Range) && locE.MinionsHit > 2 &&
                    locE.Position.IsValid()) 
                    E.Cast();
            }
        }

        public static bool Intersection(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {
            var p3 = new Vector2(pC.X + radius, pC.Y + radius);
            var m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            var constant = (m * p1.X) - p1.Y;
            var b = -(2f * ((m * constant) + p3.X + (m * p3.Y)));
            var a = (1 + (m * m));
            var c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * constant * p3.Y) + (constant * constant));
            var d = ((b * b) - (4f * a * c));

            return d > 0;
        }

        public static Obj_AI_Base EMinion
        {
            get
            {
                var vTarget = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.Physical);
                var vMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.None);

                return (from vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(E.Range))
                        let endPoint =
                            vMinion.ServerPosition.To2D()
                                .Extend(ObjectManager.Player.ServerPosition.To2D(), -E.Range)
                                .To3D()
                        where
                            Intersection(
                                ObjectManager.Player.ServerPosition.To2D(), endPoint.To2D(), vTarget.ServerPosition.To2D(),
                                vTarget.BoundingRadius + E.Width / 2)
                        select vMinion).FirstOrDefault();
            }
        }
        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.Q);

            if (E.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.R);

            if (Items.CanUseItem(3128))
                fComboDamage += vPlayer.GetItemDamage(vTarget, Damage.DamageItems.Botrk); 

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += vPlayer.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells) return;

            if (vPlayer.Distance(vTarget) < Q.Range)
            {
                Q.Cast(vTarget);
            }
            else if (vPlayer.Distance(vTarget) < R.Range)
            {
                R.Cast(vTarget);
            }
        }

        private static InventorySlot GetInventorySlot(int id)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(
                item => (item.Id == (ItemId)id && item.Stacks >= 1) || (item.Id == (ItemId)id && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null) return;

            foreach (var itemID in from menuItem in MenuTargetedItems.Items
                                   let useItem =
                                        MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                                       into itemId
                                       where Items.HasItem(itemId) &&
                                             Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                                       select itemId)
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items
                                   let useItem =
                                        MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                                       into itemId
                                       where Items.HasItem(itemId) &&
                                             Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                                       select itemId)
            {
                Items.UseItem(itemID);
            }
        }
        private static void UseSummoners()
        {
            if (SmiteSlot == SpellSlot.Unknown)
                return;
            if (!Config.Item("AutoSmite").GetValue<KeyBind>().Active) return;

            float[] smiteDmg = { 20 * vPlayer.Level + 370, 30 * vPlayer.Level + 330, 40 * vPlayer.Level + 240, 50 * vPlayer.Level + 100 };

            string[] monsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
            var firstOrDefault = vPlayer.Spellbook.Spells.FirstOrDefault(
                spell => spell.Name.Contains("mite"));
            if (firstOrDefault == null) return;

            var vMinions = MinionManager.GetMinions(vPlayer.ServerPosition, firstOrDefault.SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
            foreach (
                var vMinion in
                    vMinions.Where(
                        vMinion =>
                            vMinion != null && !vMinion.IsDead && !vPlayer.IsDead && !vPlayer.IsStunned &&
                            SmiteSlot != SpellSlot.Unknown &&
                            vPlayer.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                        .Where(
                            vMinion =>
                                (vMinion.Health < smiteDmg.Max()) &&
                                (monsterNames.Any(name => vMinion.BaseSkinName.StartsWith(name))))) 
            {
                vPlayer.Spellbook.CastSpell(SmiteSlot, vMinion);
            }
        }
    }
}
