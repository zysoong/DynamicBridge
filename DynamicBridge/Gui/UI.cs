﻿using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using ECommons;
using ECommons.Configuration;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;
using Action = System.Action;

namespace DynamicBridge.Gui;

public unsafe static class UI
{

    public static Profile SelectedProfile = null;
    public static Profile Profile => SelectedProfile ?? Utils.GetProfileByCID(Player.CID);
    public static string RandomNotice = Lang.WillBeRandomlySelectedBetween;
    public static string AnyNotice = Lang.MeetingAnyOfTheFollowingConditionsWillResultInRuleBeingTriggeredN;
    static string PSelFilter = "";
    public static string RequestTab = null;

    public static void DrawMain()
    {
        var resolution = "";
        if (Player.CID == 0) resolution = Lang.NotLoggedIn;
        else if (C.Blacklist.Contains(Player.CID)) resolution = Lang.CharacterBlacklisted;
        else if (Utils.GetProfileByCID(Player.CID) == null) resolution = Lang.NoAssociatedProfile;
        else resolution = Lang.ProfileTitle.Params(Utils.GetProfileByCID(Player.CID).CensoredName);
        if (!C.Enable && Environment.TickCount64 % 2000 > 1000) resolution = Lang.PLUGINDISABLEDBYSETTINGS;
        EzConfigGui.Window.WindowName = $"{DalamudReflector.GetPluginName()} v{P.GetType().Assembly.GetName().Version} [{resolution}]###{DalamudReflector.GetPluginName()}";
        if (ImGui.IsWindowAppearing())
        {
            Utils.ResetCaches();
            foreach (var x in Svc.Data.GetExcelSheet<Weather>()) ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.Icon, false, out _);
            foreach (var x in Svc.Data.GetExcelSheet<Emote>()) ThreadLoadImageHandler.TryGetIconTextureWrap(x.Icon, false, out _);
        }
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("TabsNR2", PatreonBanner.Text, RequestTab, [
            //("Settings", Settings, null, true),
            (C.ShowTutorial?Lang.TabTutorial:null, GuiTutorial.Draw, null, true),
            (Lang.TabDynamicRules, GuiRules.Draw, Colors.TabGreen, true),
            (Lang.TabPresets, GuiPresets.DrawUser, Colors.TabGreen, true),
            (Lang.TabGlobalPresets, GuiPresets.DrawGlobal, Colors.TabYellow, true),
            (Lang.LayeredDesigns, ComplexGlamourer.Draw, Colors.TabPurple, true),
            (Lang.TabHouseRegistration, HouseReg.Draw, Colors.TabPurple, true),
            (Lang.TabProfiles, GuiProfiles.Draw, Colors.TabBlue, true),
            (Lang.TabCharacters, GuiCharacters.Draw, Colors.TabBlue, true),
            (Lang.TabSettings, GuiSettings.Draw, null, true),
            InternalLog.ImGuiTab(),
            (C.Debug?"Debug":null, Debug.Draw, ImGuiColors.DalamudGrey3, true),
            ]);
        RequestTab = null;
    }

    public static void ProfileSelectorCommon(Action before = null, Action after = null)
    {
        if (SelectedProfile != null && !C.ProfilesL.Contains(SelectedProfile)) SelectedProfile = null;
        var currentCharaProfile = Utils.GetProfileByCID(Player.CID);

        before?.Invoke();

        if (SelectedProfile == null)
        {
            if (currentCharaProfile == null)
            {
                if (C.Blacklist.Contains(Player.CID))
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("blisted", Lang.BlacklistedCharacter.Params(Censor.Character(Player.NameWithWorld)), ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                        {
                            C.Blacklist.Remove(Player.CID);
                        }
                        ImGuiEx.Tooltip(Lang.UnblacklistThisCharacter);
                    });
                }
                else if (Player.CID != 0)
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("noprofile", Lang.CharaNoAssociation.Params(Censor.Character(Player.NameWithWorld)), ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.PlusCircle))
                        {
                            var profile = new Profile();
                            C.ProfilesL.Add(profile);
                            profile.Characters = [Player.CID];
                            profile.Name = Lang.AutogeneratedProfileFor.Params(Player.Name);
                        }
                        ImGuiEx.Tooltip(Lang.CreateNewEmptyProfileAndAssignItToCurrentCharacter);
                    });
                }
                else
                {
                    ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("nlg", Lang.YouAreNotLoggedInPleaseSelectProfileToEdit, ProfileSelectable), () =>
                    {
                        after?.Invoke();
                        ImGui.Dummy(Vector2.Zero);
                    });
                }
            }
            else
            {
                UsedByCurrent();
            }
        }
        else
        {
            if (currentCharaProfile == SelectedProfile)
            {
                UsedByCurrent();
            }
            else
            {
                ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", Lang.YouAreEditingProfile.Params(SelectedProfile.CensoredName) + (Player.Available?Lang.ItIsNotUsedBy.Params(Censor.Character(Player.NameWithWorld)) :""), ProfileSelectable, EColor.YellowDark), () =>
                {
                    after?.Invoke();
                    if (!C.Blacklist.Contains(Player.CID))
                    {
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Link))
                        {
                            new TickScheduler(() => SelectedProfile.SetCharacter(Player.CID));
                        }
                        ImGuiEx.Tooltip($"Assign profile $1 to $2".Params(SelectedProfile?.CensoredName, Censor.Character(Player.NameWithWorld)));
                    }
                    else
                    {
                        ImGuiEx.HelpMarker(Lang.YourCurrentCharacterIsBlacklisted, null, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    }
                });
            }
        }

        void UsedByCurrent()
        {
            ImGuiEx.InputWithRightButtonsArea(() => Utils.BannerCombo("EditNotify", Lang.YouAreEditingProfile1WhichIsUsedBy2.Params(currentCharaProfile.CensoredName, Censor.Character(Player.NameWithWorld)), ProfileSelectable, EColor.GreenDark), () =>
            {
                after?.Invoke();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Unlink, enabled:ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => currentCharaProfile.Characters.Remove(Player.CID));
                }
                ImGuiEx.Tooltip(Lang.HoldCTRLKeyAndClickToUnassignProfile1From2.Params(currentCharaProfile?.CensoredName, Censor.Character(Player.NameWithWorld)));
            });
        }

        void ProfileSelectable()
        {
            if(ImGui.Selectable(Lang.CurrentCharacterSelectable, SelectedProfile == null))
            {
                SelectedProfile = null;
            }
            ImGui.Separator();
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGui.InputTextWithHint($"##SearchCombo", Lang.Filter, ref PSelFilter, 50, Utils.CensorFlags);
            foreach(var x in C.ProfilesL)
            {
                if (PSelFilter.Length > 0 && !x.Name.Contains(PSelFilter, StringComparison.OrdinalIgnoreCase)) continue;
                if (SelectedProfile == x && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                if(ImGui.Selectable($"{x.CensoredName}##{x.GUID}", SelectedProfile == x))
                {
                    new TickScheduler(() => SelectedProfile = x);
                }
            }
        }
    }

    public static void ForceUpdateButton()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Tshirt))
        {
            P.ForceUpdate = true;
        }
        ImGuiEx.Tooltip(Lang.ForceUpdateYourCharacterReapplyingAllRulesAndResets);
    }
}
