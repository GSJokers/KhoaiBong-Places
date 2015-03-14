using LeagueSharp.Common;

namespace Yasuo
{
    public class YasuoMenu
    {
        /// <summary>
        ///     Yasuo Menu Constructor
        /// </summary>
        public YasuoMenu()
        {
            // => Menu Initialize
            _menu = new Menu(RootDisplayName, RootName, true);

            // => Orbwalker Initialize
            _orbwalker = new Orbwalking.Orbwalker(AddSubMenu(OrbwalkerDisplayName, OrbwalkerName));

            // => TargetSelector Initialize
            TargetSelector.AddToMenu(AddSubMenu(TargetSelectorDisplayName, TargetSelectorName));

            #region Combo Initialize

            var combo = AddSubMenu(ComboDisplayName, ComboName); // => Combo Menu
            AddItem(combo, ComboQDisplayName, ComboQName).SetValue(true); // => Use Q
            AddItem(combo, Combo3QDisplayName, Combo3QName).SetValue(true); // => Use 3rd Q
            AddItem(combo, ComboEDisplayName, ComboEName).SetValue(true); // => Use E
            AddItem(combo, ComboRDisplayName, ComboRName).SetValue(true); // => Use R
            AddSpacer(combo); // => Spacer
            AddItem(combo, ComboRModeDisplayName, ComboRModeName)
                .SetValue(new StringList(new[] { "Multi-target", "Single-target", "Both" }, 1)); // => R Mode
            AddItem(combo, ComboRPercentDisplayName, ComboRPercentName).SetValue(new Slider(40)); // => Min. Enemies Health %
            AddItem(combo, ComboRPercent2DisplayName, ComboRPercent2Name).SetValue(new Slider(40)); // => Min. Enemies Health %
            AddItem(combo, ComboRSelfDisplayName, ComboRSelfName).SetValue(false); // => Self Knockedup Enemies
            AddItem(combo, ComboRMinDisplayName, ComboRMinName).SetValue(new Slider(3, 1, 5)); // => Min. Enemies
            AddItem(combo, ComboRAirTimeDisplayName, ComboRAirTimeName).SetValue(new Slider(500, 250, 1000)); // => Min. Airtime
            AddSpacer(combo); // => Spacer
            AddItem(combo, ComboGapcloserModeDisplayName, ComboGapcloserModeName) // => Gapcloser Mode
                .SetValue(new StringList(new[] { "Follow Mouse", "Follow Enemy" })); // => Gapcloser Mode
            AddItem(combo, ComboGapcloserFModeDisplayName, ComboGapcloserFModeName).SetValue(false); // => Gapcloser Follow even in attack range
            AddItem(combo, ComboERangeDisplayName, ComboERangeName).SetValue(new Slider(475, 0, 475));
            var items = AddSubMenu(combo, ComboItemsDisplayName, ComboItemsName); // => Items Menu
            AddItem(items, ComboItemsTiamatDisplayName, ComboItemsTiamatName).SetValue(true); // => Tiamat
            AddItem(items, ComboItemsHydraDisplayName, ComboItemsHydraName).SetValue(true); // => Ravenous Hydra
            AddItem(items, ComboItemsBilgewaterDisplayName, ComboItemsBilgewaterName).SetValue(true); // => Bilgewater Cutlass
            AddItem(items, ComboItemsBotRkDisplayName, ComboItemsBotRkName).SetValue(true); // => BladeoftheRuinedKing


            #endregion

            #region Flee Initialize

            var flee = AddSubMenu(FleeDisplayName, FleeName);
            AddItem(flee, FleeEnableDisplayName, FleeEnableName).SetValue(true);
            AddItem(flee, FleeKeyDisplayName, FleeKeyName).SetValue(new KeyBind('Z', KeyBindType.Press));
            AddItem(flee, FleeFleeIntoTowersDisplayName, FleeFleeIntoTowersName).SetValue(true);

            #endregion

            #region Farming Initialize

            var farming = AddSubMenu(FarmingDisplayName, FarmingName);
            AddItem(farming, FarmingLastHitQDisplayName, FarmingLastHitQName).SetValue(true);
            AddItem(farming, FarmingLastHit3QDisplayName, FarmingLastHit3QName).SetValue(true);
            AddItem(farming, FarmingLastHitEDisplayName, FarmingLastHitEName).SetValue(true);
            AddItem(farming, FarmingLastHitQaaDisplayName, FarmingLastHitQaaName).SetValue(true);
            AddItem(farming, FarmingLastHitEaaDisplayName, FarmingLastHitEaaName).SetValue(true);
            AddItem(farming, FarmingLastHitTurretDisplayName, FarmingLastHitTurretName).SetValue(false);
            AddSpacer(farming);
            AddItem(farming, FarmingLaneClearQDisplayName, FarmingLaneClearQName).SetValue(true);
            AddItem(farming, FarmingLaneClear3QDisplayName, FarmingLaneClear3QName).SetValue(true);
            AddItem(farming, FarmingLaneClearEDisplayName, FarmingLaneClearEName).SetValue(true);
            AddItem(farming, FarmingLaneClearQaaDisplayName, FarmingLaneClearQaaName).SetValue(true);
            AddItem(farming, FarmingLaneClearEaaDisplayName, FarmingLaneClearEaaName).SetValue(true);
            AddItem(farming, FarmingLaneClearTurretDisplayName, FarmingLaneClearTurretName).SetValue(false);

            #endregion

            #region Killsteal Initialize

            //
            var ks = AddSubMenu(KillstealDisplayName, KillstealName);
            AddItem(ks, KillstealEnabledDisplayName, KillstealEnabledName).SetValue(true);
            AddSpacer(ks);
            AddItem(ks, KillstealQDisplayName, KillstealQName).SetValue(true);
            AddItem(ks, Killsteal3QDisplayName, Killsteal3QName).SetValue(true);
            AddItem(ks, KillstealEDisplayName, KillstealEName).SetValue(true);
            AddItem(ks, KillstealEIntoTowerDisplayName, KillstealEIntoTowerName).SetValue(true);

            #endregion

            // Auto Windwall
            var aww = AutoWindMenu = AddSubMenu(AutoWindWall, AutoWindWallLoc); // => Auto Windwall
            AddItem(aww, AutoWindWallUse, AutoWindWallUseLoc).SetValue(true); // => Use
            AddItem(aww, AutoWindWallDelay, AutoWindWallDelayLoc).SetValue(new Slider(500, 150, 2000)); // => Windwall Delay
            AddSpacer(aww); // => SPACER

            // Evade
            var evade = EvadeMenu = AddSubMenu(Evade, EvadeLoc); // => Evade
            AddItem(evade, EvadeUse, EvadeUseLoc).SetValue(true); // => Use
            AddSpacer(evade); // => SPACER

            #region Misc Initialize

            var misc = AddSubMenu(MiscDisplayName, MiscName); // => Misc Menu
            AddItem(misc, MiscPacketsDisplayName, MiscPacketsName).SetValue(true); // => Packets

            #endregion

            // => Menu Footer
            AddSpacer(_menu); // => Spacer
            AddItem(_menu, RootDisplayName, RootDescriptionName); // => Description

            // => Menu Finalize
            _menu.AddToMainMenu();
        }

        /// <summary>
        ///     Add a sub menu towards the main menu.
        /// </summary>
        /// <param name="displayName">Sub Menu Display Name</param>
        /// <param name="localisationName">Sub Menu Directory</param>
        /// <returns></returns>
        public Menu AddSubMenu(string displayName, string localisationName)
        {
            return _menu.AddSubMenu(new Menu(displayName, localisationName));
        }

        /// <summary>
        ///     Add a sub menu towards a selected menu.
        /// </summary>
        /// <param name="menu">Parent Menu</param>
        /// <param name="displayName">Sub Menu Display Name</param>
        /// <param name="localisationName">Sub Menu Directory</param>
        /// <returns></returns>
        public Menu AddSubMenu(Menu menu, string displayName, string localisationName)
        {
            return menu.AddSubMenu(new Menu(displayName, RootName + localisationName));
        }

        /// <summary>
        ///     Add an item towards a selected menu.
        /// </summary>
        /// <param name="menu">Parent Menu</param>
        /// <param name="displayName">Item Display Name</param>
        /// <param name="localisationName">Item Directory</param>
        /// <returns></returns>
        public MenuItem AddItem(Menu menu, string displayName, string localisationName)
        {
            return menu.AddItem(new MenuItem(RootName + localisationName, displayName));
        }

        /// <summary>
        ///     Add a spacer towards a selected menu.
        /// </summary>
        /// <param name="menu">Parent Menu</param>
        /// <param name="localisationName">Spacer Directory</param>
        public void AddSpacer(Menu menu, string localisationName = "")
        {
            _spacer++;
            menu.AddItem(new MenuItem(RootName + localisationName + ".spacer_" + _spacer, ""));
        }

        /// <summary>
        ///     Orbwalker instance
        /// </summary>
        /// <returns>Returns the orbwalker instance</returns>
        public Orbwalking.Orbwalker GetOrbwalker()
        {
            return _orbwalker;
        }

        /// <summary>
        ///     Menu instance
        /// </summary>
        /// <returns>Returns the menu instance</returns>
        public Menu GetMenu()
        {
            return _menu;
        }

        /// <summary>
        ///     Retrives a value from a MenuItem
        /// </summary>
        /// <typeparam name="T">Value Type</typeparam>
        /// <param name="localisationName">Item Directory</param>
        /// <returns>MenuItem value</returns>
        public T GetValue<T>(string localisationName)
        {
            return _menu.Item(RootName + localisationName).GetValue<T>();
        }

        /// <summary>
        ///     Quick reference to fetch the item
        /// </summary>
        /// <param name="str">Item Location</param>
        /// <returns>Item</returns>
        public MenuItem GetItem(string str)
        {
            return _menu.Item(RootName + str);
        }

        #region Fields

        private readonly Menu _menu; // => The Menu
        private readonly Orbwalking.Orbwalker _orbwalker; // => The Orbwalker

        private int _spacer = -1; // => Spacer Counter

        private const string RootDisplayName = "Yasuo the Unforgiven"; // => Menu Display Name
        private const string RootName = "l33t.yasuo"; // => Menu Name
        private const string RootDescriptionName = ".desc";

        private const string OrbwalkerDisplayName = "Orbwalker"; // => Orbwalker Display Name
        private const string OrbwalkerName = ".orbwalker"; // => Orbwalker Name

        private const string TargetSelectorDisplayName = "Target Selector"; // => TargetSelector Display Name
        private const string TargetSelectorName = ".targetselector"; // => TargetSelector Name

        #region Combo

        private const string ComboDisplayName = "Combo Settings"; // => Combo Display Name
        private const string ComboName = ".combo"; // => Combo Name

        private const string ComboQDisplayName = "Use Steel Tempest (Q)"; // => Combo Q Display Name
        public const string ComboQName = ComboName + ".useq"; // => Combo Q Name
        private const string Combo3QDisplayName = "Use Steel Tempest - Whirlwind (3rd Q)"; // => Combo Q Display Name
        public const string Combo3QName = ComboName + ".use3q"; // => Combo Q Name
        private const string ComboEDisplayName = "Use Sweeping Blade (E)"; // => Combo E Display Name
        public const string ComboEName = ComboName + ".usee"; // => Combo E Name
        private const string ComboRDisplayName = "Use Last Breath (R)"; // => Combo R Display Name
        public const string ComboRName = ComboName + ".user"; // => Combo R Name

        private const string ComboRModeDisplayName = "Last Breath (R) Mode"; // => Combo R Mode Display Name
        public const string ComboRModeName = ComboName + ".rmode"; // => Combo R Mode Name
        private const string ComboRMinDisplayName = "[Last Breath] Min. Enemies to Use R"; // => R Min Enemies to use R
        public const string ComboRMinName = ComboName + ".rmin";

        private const string ComboRPercentDisplayName = "[Last Breath] Min. Enemies Health %"; // => Combo R Min. Enemies Health % Display Name
        public const string ComboRPercentName = ComboName + ".renemieshealthper"; // => Combo R Min. Enemies Health % Name
        private const string ComboRPercent2DisplayName = "[Last Breath] Min. Target Health %"; // => Combo R Min. Target Health % Display Name
        public const string ComboRPercent2Name = ComboName + ".renemyhealthper"; // => Combo R Min. Enemies Health % Name
        private const string ComboRSelfDisplayName = "[Last Breath] Only self knockedup enemies"; // => R only self knockedup enemies Display Name
        public const string ComboRSelfName = ComboName + ".ronlyself"; // => R only self knockedup enemies Name
        private const string ComboRAirTimeDisplayName = "[Last Breath] Keep in the air till (milliseconds)"; // => R Keep in the air before casting Display Name
        public const string ComboRAirTimeName = ComboName + ".rairtime"; // => R Keep in the air before casting Name
        private const string ComboGapcloserModeDisplayName = "Gapcloser Mode"; // => Gapcloser Mode Display name
        public const string ComboGapcloserModeName = ComboName + ".gapclosermode"; // => Gapcloser Mode Display name
        private const string ComboGapcloserFModeDisplayName = "[Gapcloser] Follow even if in attack range"; // => Gapcloser Follow Mode Display name
        public const string ComboGapcloserFModeName = ComboName + ".gapcloserfollowmode"; // => Gapcloser Follow Mode Display name

        private const string ComboERangeDisplayName = "Sweeping Blade (E) Casting Range";
        public const string ComboERangeName = ComboName + ".usecastingrange";

        #region Items

        private const string ComboItemsDisplayName = "Item Settings"; // => Items Display Name
        private const string ComboItemsName = ComboName + ".items"; // => Items Name

        private const string ComboItemsTiamatDisplayName = "Use Tiamat"; // => Tiamat Display Name
        public const string ComboItemsTiamatName = ComboItemsName + ".usetiamat"; // => Tiamat Name
        private const string ComboItemsHydraDisplayName = "Use Ravenous Hydra"; // => Ravenous Hydra Display Name
        public const string ComboItemsHydraName = ComboItemsName + ".usehydra"; // => Ravenous Hydra Name
        private const string ComboItemsBilgewaterDisplayName = "Use Bilgewater Cutlass"; // => Bilgewater Cutlass Display Name
        public const string ComboItemsBilgewaterName = ComboItemsName + ".usebilgewater"; // => Bilgewater Cutlass Name
        private const string ComboItemsBotRkDisplayName = "Use Blade of the Ruined King"; // => Blade of the Ruined King Display Name
        public const string ComboItemsBotRkName = ComboItemsName + ".usebotrk"; // => Blade of the Ruined King Name

        #endregion

        #endregion

        #region Flee

        private const string FleeDisplayName = "Flee Settings"; // => Flee Display Name
        private const string FleeName = ".flee"; // => Flee Name

        private const string FleeEnableDisplayName = "Enable Flee Mode"; // => Flee Enabled Display Name
        public const string FleeEnableName = FleeName + ".useflee"; // => Flee Enabled Name
        private const string FleeKeyDisplayName = "Flee"; // => Flee Key Display Name
        public const string FleeKeyName = FleeName + ".usefleekey"; // => Flee Key Name
        private const string FleeFleeIntoTowersDisplayName = "Flee into enemy towers"; // => Flee into towers Display Name
        public const string FleeFleeIntoTowersName = FleeName + ".usefleetowers"; // => Flee into towers

        #endregion

        #region Farming

        private const string FarmingDisplayName = "Farming Settings"; // => Farming Display Name
        private const string FarmingName = ".farming"; // => Farming Name

        private const string FarmingLastHitQDisplayName = "[LastHit] Use Steel Tempest (Q)"; // => Last Hit Q Display Name
        public const string FarmingLastHitQName = FarmingName + ".lhuseq"; // => Last Hit Q Name
        private const string FarmingLastHit3QDisplayName = "[LastHit] Use Steel Tempest (Q) - Whirlwind"; // => Last Hit 3rd Q Display Name
        public const string FarmingLastHit3QName = FarmingName + ".lhuse3q"; // => Last Hit 3rd Q Name
        private const string FarmingLastHitEDisplayName = "[LastHit] Use Sweeping Blade (E)"; // => Last Hit E Display Name
        public const string FarmingLastHitEName = FarmingName + ".lhusee"; // => Last Hit E Name
        private const string FarmingLastHitEaaDisplayName = "[LastHit] Prioritize E over Basic Attack"; // => Last Hit Prioritize E over AA Display Name
        public const string FarmingLastHitEaaName = FarmingName + "lheoveraa"; // => Last Hit Prioritize E over AA Name
        private const string FarmingLastHitQaaDisplayName = "[LastHit] Prioritize Q over Basic Attack"; // => Last Hit Prioritize Q over AA Display Name
        public const string FarmingLastHitQaaName = FarmingName + "lhqoveraa"; // => Last Hit Prioritize Q over AA Name
        private const string FarmingLastHitTurretDisplayName = "[LastHit] Use Sweeping Blade (E) towards under turret"; // => Last Hit Use E to towers Display Name
        public const string FarmingLastHitTurretName = FarmingName + "lhuseut"; // => Last Hit Use E to towers Name

        private const string FarmingLaneClearQDisplayName = "[LaneClear] Use Steel Tempest (Q)"; // => Lane Clear Q Display Name
        public const string FarmingLaneClearQName = FarmingName + ".lcuseq"; // => Lane Clear Q Name
        private const string FarmingLaneClear3QDisplayName = "[LaneClear] Use Steel Tempest (Q) - Whirlwind"; // => Lane Clear 3rd Q Display Name
        public const string FarmingLaneClear3QName = FarmingName + ".lcuse3q"; // => Lane Clear 3rd Q Name
        private const string FarmingLaneClearEDisplayName = "[LaneClear] Use Sweeping Blade (E)"; // => Lane Clear E Display Name
        public const string FarmingLaneClearEName = FarmingName + ".lcusee"; // => Lane Clae E Name
        private const string FarmingLaneClearEaaDisplayName = "[LaneClear] Prioritize E over Basic Attack"; // => Lane Clear Prioritize E over AA Display Name
        public const string FarmingLaneClearEaaName = FarmingName + "lceoveraa"; // => Lane Clear Prioritize E over AA Name
        private const string FarmingLaneClearQaaDisplayName = "[LaneClear] Prioritize Q over Basic Attack"; // => Lane Clear Prioritize Q over AA Display Name
        public const string FarmingLaneClearQaaName = FarmingName + "lcqoveraa"; // => Lane Clear Prioritize Q over AA Name
        private const string FarmingLaneClearTurretDisplayName = "[LaneClear] Use Sweeping Blade (E) towards under turret"; // => Lane Clear Use E to towers Display Name
        public const string FarmingLaneClearTurretName = FarmingName + "lcuseut"; // => Lane Clear Use E to towers Name

        #endregion

        #region Killsteal

        private const string KillstealDisplayName = "Killsteal Settings"; // => Killsteal Display Name
        private const string KillstealName = ".ks"; // => Killsteal Name
        private const string KillstealEnabledDisplayName = "Killsteal Enabled"; // => Killsteal Enabled Display Name
        public const string KillstealEnabledName = KillstealName + ".kse"; // => Killsteal Enabled Name

        private const string KillstealQDisplayName = "Use Steel Tempest (Q)"; // => Killsteal Q Display Name
        public const string KillstealQName = KillstealName + ".useq"; // => Killsteal Q Name
        private const string Killsteal3QDisplayName = "Use Steel Tempest - Whirlwind (3rd Q)"; // => Killsteal 3Q Display Name
        public const string Killsteal3QName = KillstealName + ".use3q"; // => Killsteal 3Q Name
        private const string KillstealEDisplayName = "Use Sweeping Blade (E)"; // => Killsteal E Display Name
        public const string KillstealEName = KillstealName + ".usee"; // => Killsteal E Name
        private const string KillstealEIntoTowerDisplayName = "Use Sweeping Blade (E) towards under turret"; // => Killsteal E Into Towers Display Name
        public const string KillstealEIntoTowerName = KillstealName + ".useeintotower"; // => Killsteal E Into Towers Name


        #endregion

        #region Windwall/Evade

        /* AUTO WINDWALL */
        public static Menu AutoWindMenu;
        private const string AutoWindWall = "Auto Windwall Settings";
        public const string AutoWindWallLoc = ".autoww";
        private const string AutoWindWallUse = "Use Auto Windwall";
        public const string AutoWindWallUseLoc = AutoWindWallLoc + ".usew";
        private const string AutoWindWallDelay = "Windwall Delay";
        public const string AutoWindWallDelayLoc = AutoWindWallLoc + ".delay";

        /* EVADE */
        public static Menu EvadeMenu;
        private const string Evade = "Evade Settings";
        public const string EvadeLoc = ".evade";
        private const string EvadeUse = "Use Sweeping Blade (E) to Evade";
        public const string EvadeUseLoc = EvadeLoc + ".use";

        #endregion

        #region Misc

        private const string MiscDisplayName = "Misc Settings"; // => Misc Display Name
        private const string MiscName = ".misc"; // => Misc Name

        private const string MiscPacketsDisplayName = "Packets"; // => Packets Display Name
        public const string MiscPacketsName = MiscName + ".packets"; // => Packets Display Name

        #endregion

        #endregion
    }
}