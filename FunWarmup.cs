using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Commands;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;

namespace FunWarmup;
public class WarmupConfig : BasePluginConfig
{
    [JsonPropertyName("EnableFunWarmup")]
    public bool EnableFunWarmup{get; set;} = true;
}
public class FunWarmupPlugin : BasePlugin, IPluginConfig<WarmupConfig>
{
    public override string ModuleName => "Fun Warmup";
    public override string ModuleAuthor => "Phoenix";
    public override string ModuleVersion => "1.0";
    public WarmupConfig Config {get; set;} = new WarmupConfig ();
    private bool isNeedUpdate = true;
    public void OnConfigParsed(WarmupConfig config)
    {
        Config = config;
    }
    bool knifeRound = false, ssgRound = false, hERound = false, speedy = false, canTouch = false, invis = false, hasbik = false, bigBoy = false, secondVandam = false, strongAwp = false;
    private bool isWarmupActive = false;
    public override void Load(bool hotReload)
    {
        Config.Reload();

        AddCommandListener("buy", PlayerCantUse);
        AddCommandListener("autobuy", PlayerCantUse);
        AddCommandListener("rebuy", PlayerCantUse);
        AddCommandListener("mp_warmup_end", OnWarmupEndFromCommand);
        RegisterEventHandler<EventItemPickup>(PlayerCantTake);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventRoundAnnounceWarmup>(OnWarmupStart);
        RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        RegisterEventHandler<EventPlayerSpawn>(ManupilationPlayer);
        RegisterEventHandler<EventPlayerHurt>(PlayerDamage);
        RegisterEventHandler<EventRoundAnnounceMatchStart>(GiveNormalPlayerFirstRound);
        RegisterEventHandler<EventPlayerTeam>(TakeSkill);
    }
    private HookResult TakeSkill (EventPlayerTeam @event, GameEventInfo info)
    {
        if (!isWarmupActive) return HookResult.Continue;

        var player = @event.Userid;
        var whatTeam = @event.Team;

        if (player == null || !player.IsValid) return HookResult.Continue;

        if (whatTeam == 1 || whatTeam == 0)
        {
            ResetsAllSkill(player);
        }

        return HookResult.Continue;
    }
    private HookResult GiveNormalPlayerFirstRound (EventRoundAnnounceMatchStart @event, GameEventInfo info)
    {
        if (!isNeedUpdate) return HookResult.Continue;

        foreach (var player in Utilities.GetPlayers())
        {
            ResetsAllSkill(player);
        }
        var scopeStatic = ConVar.Find("weapon_accuracy_nospread");
        scopeStatic?.SetValue(false);
        isNeedUpdate = false;

        return HookResult.Continue;
    }
    private HookResult PlayerDamage (EventPlayerHurt @event, GameEventInfo info)
    {
        if (Config.EnableFunWarmup == false || !isWarmupActive)
        {
            return HookResult.Continue;
        }
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid || !victim.PawnIsAlive) return HookResult.Continue;
        var pawnVictim = victim.PlayerPawn.Value;
        if (pawnVictim == null || !pawnVictim.IsValid) return HookResult.Continue;
        if (speedy || hasbik)
        {
            AddTimer(2.0f, () =>
            {
                if (victim == null || !victim.IsValid || pawnVictim == null || !pawnVictim.IsValid || !victim.PawnIsAlive)
                {
                    return;
                }
                newSpeed(pawnVictim, 1.4f);
            });
        }
        else if (bigBoy)
        {
            newSpeed(pawnVictim, 0.8f);
        }
        else if (canTouch)
        {
            if (pawnVictim == null) return HookResult.Continue;

            if (@event.Weapon == "deagle")
            {
                if ((pawnVictim.Health + @event.DmgHealth) > 20)
                {
                    pawnVictim.Health = pawnVictim.Health + @event.DmgHealth - 20;
                    Utilities.SetStateChanged(pawnVictim, "CBaseEntity", "m_iHealth");
                    slapPlayer(pawnVictim);
                }
            }
        }
        return HookResult.Continue;
    }
    public void applyFeatures (CCSPlayerController player)
    {    
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || !player.PawnIsAlive) return;

            AddTimer(0.3f, () =>
            {
                if (player == null || !player.IsValid || !pawn.IsValid || !player.PawnIsAlive) return; 

                if (speedy || invis || hasbik || bigBoy)
                {
                    if (speedy)
                    {
                        newSpeed(pawn, 1.4f);
                    } else if (invis) {
                        invisPlayer(pawn);
                    } else if (hasbik)
                    {
                        SetScale(pawn, 0.5f);
                        newSpeed(pawn, 1.4f);
                    } else if (bigBoy)
                    {
                        SetScale(pawn, 1.5f);
                        newSpeed(pawn, 0.8f);
                    }
                    return;
                }

                player.RemoveWeapons();

                if (knifeRound)
                {
                    player.GiveNamedItem("weapon_knife");
                    newHp(pawn, 35);
                } else if (ssgRound)
                {
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem("weapon_ssg08");
                    newGravity(pawn, 0.3f);
                    var air = ConVar.Find("sv_airaccelerate");
                    air?.SetValue(20000.0f);
                    var scopeStatic = ConVar.Find("weapon_accuracy_nospread");
                    scopeStatic?.SetValue(true);
                } else if (hERound)
                {
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem("weapon_hegrenade");
                } else if (canTouch)
                {
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem("weapon_revolver");
                } else if (secondVandam)
                {
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem("weapon_sawedoff");
                    newHp(pawn, 650);
                } else if (strongAwp)
                {
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem("weapon_awp");
                }
            });
            return;   
    }
    private HookResult OnWarmupStart (EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        if (Config.EnableFunWarmup == false)
        {
            return HookResult.Continue;
        }
        ResetTypeWarmup();
        SetTypeWarmup();
        isWarmupActive = true;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) continue;
            applyFeatures(player);
        }

        return HookResult.Continue;
    }
    private HookResult OnWarmupEndFromCommand (CCSPlayerController? player, CommandInfo command)
    {
        WarmupEnd();
        return HookResult.Continue;
    }
    private HookResult OnWarmupEnd (EventWarmupEnd @event, GameEventInfo info)
    {
        WarmupEnd();
        return HookResult.Continue;
    }
    private HookResult PlayerCantUse (CCSPlayerController? player, CommandInfo command)
    {
        if (isWarmupActive == false) return HookResult.Continue;
        if (knifeRound || ssgRound || hERound || canTouch || secondVandam || strongAwp)
        {
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }
    private HookResult PlayerCantTake (EventItemPickup @event, GameEventInfo info)
    {
        if (isWarmupActive == false) return HookResult.Continue;
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive) return HookResult.Continue;
        var weapon = @event.Item;
        if (weapon == null) return HookResult.Continue;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return HookResult.Continue;
        if ((weapon == "hkp2000" || weapon == "glock") && (knifeRound || ssgRound || hERound || canTouch || secondVandam || strongAwp))
        {
            applyFeatures(player);
        }
        return HookResult.Continue;
    }
    private HookResult OnWeaponFire (EventWeaponFire @event, GameEventInfo info)
    {
        if (!Config.EnableFunWarmup || !isWarmupActive)
        {
            return HookResult.Continue;
        }
        if (!hERound && !strongAwp)
        {
            return HookResult.Continue;
        }
        var player = @event.Userid;
        var weaponPlayer = @event.Weapon;
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PawnIsAlive || weaponPlayer == null)
        {
            return HookResult.Continue;
        }
        if (hERound)
        {
            AddTimer (0.5f, () =>
            {
                if (player == null || !player.IsValid || !player.PawnIsAlive) return;
                if (weaponPlayer == "weapon_hegrenade")
                {
                    player.GiveNamedItem("weapon_hegrenade");
                }
            });
        }
        else if (strongAwp)
        {
            if (weaponPlayer == "weapon_awp")
            {
                slapPlayer(player.PlayerPawn.Value);
            }
        }
        return HookResult.Continue;
    }
    private HookResult ManupilationPlayer(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!isWarmupActive)
        {
            return HookResult.Continue;
        }

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.TeamNum <= 1) return HookResult.Continue;
        AddTimer (0.5f, () =>
        {
            applyFeatures(player);
        });
        
        return HookResult.Continue;
    }
    public void newHp (CCSPlayerPawn pawn, int newValue)
    {
        pawn.Health = newValue;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
    public void newGravity (CCSPlayerPawn pawn, float newValue)
    {
        pawn.ActualGravityScale = newValue;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flActualGravityScale");
    }
    public void newSpeed (CCSPlayerPawn pawn, float newValue)
    {
        pawn.VelocityModifier = newValue;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }
    public void slapPlayer (CCSPlayerPawn pawn)
    {
        Random rnd = new Random();
        pawn.AbsVelocity.X = rnd.Next(-1000, 1000);
        pawn.AbsVelocity.Y = rnd.Next(-1000, 1000);
        pawn.AbsVelocity.Z = rnd.Next(300, 1000);
    }
    public void invisPlayer (CCSPlayerPawn pawn)
    {
        pawn.Render = Color.FromArgb(130, 0, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");   
    }
    public void SetScale(CCSPlayerPawn pawn, float value)
    {
        if (pawn == null)
        {
            return;
        }
        pawn.CBodyComponent!.SceneNode!.Scale = value;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
    }
    public void SetTypeWarmup ()
    {
        int number = Random.Shared.Next(1, 11);
        switch (number)
        {
            case 1:
                knifeRound = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["KnifeRound"]);
            break;
            case 2:
                ssgRound = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["SSGRound"]);
            break;
            case 3:
                hERound = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["HERound"]);
            break;
            case 4:
                speedy = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["Speedy"]);
            break;
            case 5:
                canTouch = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["CanTouch"]);
            break;
            case 6:
                invis = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["NedoDimki"]);
            break;
            case 7:
                hasbik = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["Hasbiki"]);
            break;
            case 8:
                bigBoy = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["BigBoy"]);
            break;
            case 9:
                secondVandam = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["Vandam"]);
            break;
            case 10:
                strongAwp = true;
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["WarmupType"] + Localizer["StrongAwp"]);
            break;

            default:
                Server.PrintToChatAll(Localizer["Prefix"] + " Произошла ошибка :(");
            break;
        }
    }
    public void ResetTypeWarmup ()
    {
        knifeRound = ssgRound = hERound = speedy = canTouch = invis = hasbik = bigBoy = secondVandam = strongAwp = false;
    }
    [RequiresPermissions("@css/root")]
    [ConsoleCommand("wfupdate", "Обновляет конфигурацию")]
    public void UpdateConfig(CCSPlayerController? player, CommandInfo command)
    {
        Config.Update();
        if (player == null)
        {
            Console.WriteLine("[FunWarmup] Конфиг успешно перезапущен!");
        }
        else
        {
            player.PrintToChat(Localizer["Prefix"] + "Конфиг успешно перезапущен");
        }
        return;
    }
    public void WarmupEnd ()
    {
        if (!isWarmupActive)
        {
            return;
        }
        isWarmupActive = false;
        foreach (var playerok in Utilities.GetPlayers())
        {
            ResetsAllSkill(playerok);
        }
        var scopeStatic = ConVar.Find("weapon_accuracy_nospread");
        scopeStatic?.SetValue(false);
        ResetTypeWarmup();
        return;
    }
    public void ResetsAllSkill (CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;
        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        SetScale(pawn, 1.0f);
        newGravity(pawn, 1.0f);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}