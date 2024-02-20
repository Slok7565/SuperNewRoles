using System;
using HarmonyLib;
using Hazel;
using SuperNewRoles.Buttons;
using SuperNewRoles.Mode;
using UnityEngine;
using SuperNewRoles.Roles.Role;
using SuperNewRoles.Roles.RoleBases;
using SuperNewRoles.Roles.RoleBases.Interfaces;
using System.Collections.Generic;
using TMPro;

namespace SuperNewRoles.Roles;

public class InvisibleRoleBase : RoleBase, IRpcHandler, IMeetingHandler, IHandleChangeRole, IDeathHandler, IHandleDisconnect
{
    /// <summary>
    /// 透明化が発動しているか
    /// </summary>
    /// <value>
    /// true : 透明化の対象が存在し, 且つ透明化が有効である,
    /// false : 透明化が発動していない
    /// </value>
    public bool IsActive => InvisiblePlayer != null ? _isActive : false;

    /// <summary>
    /// InvisibleCrew の 透明化が有効か
    /// </summary>
    private bool _isActive { get; set; }

    /// <summary>
    /// 透明化の対象となっているプレイヤー
    /// </summary>
    public PlayerControl InvisiblePlayer { get; private set; }

    public InvisibleRoleBase(PlayerControl player, RoleInfo Roleinfo, OptionInfo Optioninfo, IntroInfo Introinfo) : base(player, Roleinfo, Optioninfo, Introinfo)
    {
        this._isActive = false;
        this.InvisiblePlayer = null;
    }

    /// <summary>
    /// 透明化しているプレイヤーが存在するか
    /// FixedUpdateでPlayerControl.AllPlayerControlのforeachを常に回して取得する事を防ぐ為に, 透明化しているプレイヤーが存在するかを静的な変数でも管理している。
    /// </summary>
    public static PlayerData<bool> IsExistsInvisiblePlayer { get; private set; }
    public static void ClearAndReload() => IsExistsInvisiblePlayer = new();

    public enum RpcType
    {
        Start,
        End
    }

    /// <summary>
    /// 透明化 有効処理の実行及び送信
    /// </summary>
    /// <param name="target">透明化する対象</param>
    /// <param name="isRpcSend">RPCを送るか</param>
    public void EnableInvisible(PlayerControl target, bool isRpcSend = false)
    {
        if (target != null)
        {
            IsExistsInvisiblePlayer[target.PlayerId] = true;

            this._isActive = true;
            this.InvisiblePlayer = target;

            if (isRpcSend) RpcSend(RpcType.Start, target);
        }
        else
        {
            DisableInvisible(isRpcSend);
            Logger.Error($"透明化の過程で異常なリクエストが行われた為, 初期化します。 Role : {this.Player.GetRole()}", "InvisibleRoleBase");
        }
    }
    /// <summary>
    /// 透明化 解除処理の実行及び送信
    /// </summary>
    /// <param name="isRpcSend">RPCを送るか</param>
    public void DisableInvisible(bool isRpcSend = false)
    {
        if (!this.IsActive) return;
        PlayerControl releaseTarget = this.InvisiblePlayer;

        bool isItStillInvisible = false;

        foreach (var processingPlayer in PlayerControl.AllPlayerControls)
        {
            if (processingPlayer.TryGetRoleBase<InvisibleRoleBase>(out var invisibleRoleBase))
            {
                if (invisibleRoleBase.Player == this.Player) { continue; } // 本人なら検索続行
                else if (invisibleRoleBase.InvisiblePlayer == null || invisibleRoleBase.InvisiblePlayer != releaseTarget) { continue; } // 対象が存在しない又は別のプレイヤーを透明化しているなら検索続行
                else // 別のプレイヤーが対象を透明化しているなら
                {
                    isItStillInvisible = true;
                    break; // 1人でも見つかった時点で ループから抜け出す。
                }
            }
            else break;
        }

        IsExistsInvisiblePlayer[releaseTarget.PlayerId] = isItStillInvisible;

        this._isActive = false;
        this.InvisiblePlayer = null;

        ReleaseOfInvisible(releaseTarget);

        if (isRpcSend) RpcSend(RpcType.End, null);
    }

    /// <summary>
    /// 透明状態の解除
    /// </summary>
    /// <param name="releaseTarget">透明化を解除する対象</param>
    private static void ReleaseOfInvisible(PlayerControl releaseTarget) => InvisibleRole.SetOpacity(releaseTarget, 1.5f, true);

    public void RpcSend(RpcType type, PlayerControl target) // Start() と SetScientistEnd() を合わせた物
    {
        MessageWriter writer = RpcWriter;

        writer.Write((byte)type);
        writer.Write(target != null ? target.PlayerId : 255);

        SendRpc(writer);
    }

    public void RpcReader(MessageReader reader) // RPCProcedure.SetScientistRPC (RPC 読み取り & 反映) に当たる部分
    {
        (RpcType type, PlayerControl target) = ((RpcType)reader.ReadByte(), ModHelpers.PlayerById(reader.ReadByte()));

        Logger.Info($"RpcType : {type}, 透明化処理を呼び出したプレイヤー : {this.Player.name} ({this.Player.GetRole()}), 透明化の対象 : {(target != null ? target.name : "null")}", "RpcSetInvisible");

        switch (type)
        {
            case RpcType.Start:
                EnableInvisible(target);
                break;
            case RpcType.End:
                DisableInvisible();
                break;
            default:
                Logger.Error($"RpcTypeが異常なリクエストが行われた為, 初期化します。", "TransparentRoleBase");
                DisableInvisible();
                break;
        }
    }


    /// <summary>
    /// 対象に, 透明化状態を反映する事ができるか。
    /// </summary>
    /// <param name="invisibleTarget">透明化効果が発生している対象</param>
    /// <returns>true : 反映可能 , false : 反映不可 (対象に透明化効果が発生していても通常通り表示する)</returns>
    public virtual bool CanTransparencyStateReflected(PlayerControl invisibleTarget)
    {
        return true;
    }

    /// <summary>
    /// 対象を, 半透明状態で見る事ができるか。
    /// </summary>
    /// <param name="invisibleTarget">透明化効果が発生している対象</param>
    /// <returns></returns>
    public virtual bool CanSeeTranslucentState(PlayerControl invisibleTarget)
    {
        return false;
    }

    // interfaceを利用した, 特定タイミングでの透明化解除処理

    public void StartMeeting() { }
    public void CloseMeeting() => DisableInvisible(); // 会議終了時
    public void OnChangeRole() => DisableInvisible(true); // 透明化役職が役職変更を受けた時
    public void OnMurderPlayer(DeathInfo info) { if (info.DeathPlayer == InvisiblePlayer) DisableInvisible(); } // 透明化しているプレイヤーの死亡時
    public void OnDisconnect() => DisableInvisible(); // 透明化役職の切断時
}

[HarmonyPatch]
public class InvisibleRole
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate)), HarmonyPostfix]
    public static void PlayerPhysics_Postfix(PlayerPhysics __instance)
    {
        if (AmongUsClient.Instance.GameState != AmongUsClient.GameStates.Started || !GameData.Instance) return;
        if (!ModeHandler.IsMode(ModeId.Default)) return;
        if (__instance.myPlayer == null || __instance.myPlayer.IsDead()) return;

        if (!InvisibleRoleBase.IsExistsInvisiblePlayer[__instance.myPlayer.PlayerId]) return; // __instance.myPlayerの透明化が有効になっていない, 又は管理されてないなら 実行しない。

        foreach (PlayerControl processingPlayer in PlayerControl.AllPlayerControls)
        {
            // 透明化を発動可能なプレイヤーを取得する。
            if (!processingPlayer.TryGetRoleBase<InvisibleRoleBase>(out var invisibleRoleBase)) continue;

            // __instance.myPlayer を processingPlayer が 透明化している時に 以降の処理を実行
            if (!(invisibleRoleBase.IsActive && invisibleRoleBase.InvisiblePlayer == __instance.myPlayer)) continue;

            var invisibleTarget = __instance.myPlayer;
            var isHide = invisibleRoleBase.CanTransparencyStateReflected(invisibleTarget); // 透明化を自視点で反映可能か
            bool canSeeTranslucentState = invisibleRoleBase.CanSeeTranslucentState(invisibleTarget) && isHide; // 自視点で半透明で表示するか

            // 自身が死んでいるなら, 透明化している者を無条件で半透明で見る事ができる。
            if (PlayerControl.LocalPlayer.IsDead()) { isHide = true; canSeeTranslucentState = true; }

            var opacity = canSeeTranslucentState ? 0.1f : 0.0f;
            if (isHide) // 透明化が反映される時
            {
                opacity = Math.Max(opacity, 0);
                invisibleTarget.MyRend().material.SetFloat("_Outline", 0f);
            }
            else // 透明化が反映できない時
            {
                opacity = 1.5f;
            }
            SetOpacity(invisibleTarget, opacity, canSeeTranslucentState);
        }
    }
    public static void SetOpacity(PlayerControl player, float opacity, bool cansee)
    {
        // Sometimes it just doesn't work?
        var color = Color.Lerp(Palette.ClearWhite, Palette.White, opacity);
        try
        {
            if (player.MyRend() != null)
                player.MyRend().color = color;

            if (player.GetSkin().layer != null)
                player.GetSkin().layer.color = color;

            if (player.cosmetics.hat != null)
                player.cosmetics.hat.SpriteColor = color;

            if (player.GetPet() != null)
                player.GetPet().ForEachRenderer(true, (Il2CppSystem.Action<SpriteRenderer>)((render) => render.color = color));

            if (player.VisorSlot() != null)
                player.VisorSlot().Image.color = color;

            if (player.cosmetics.colorBlindText != null && opacity < 0.1f) // 完全に透明化している場合のみ, 色覚補助テキストを非表示にする。
                player.cosmetics.colorBlindText.color = Palette.ClearWhite;
            else if (player.cosmetics.colorBlindText != null)
                player.cosmetics.colorBlindText.color = Palette.White;

            if (player.cosmetics.nameTextContainer != null && opacity < 0.1f)
                player.cosmetics.nameTextContainer.GetComponent<TextMeshPro>().enabled = false; // 完全に透明化している場合は, プレイヤー名を非表示にする。
            else
                player.cosmetics.nameTextContainer.GetComponent<TextMeshPro>().enabled = true; // 少しでも姿を見られるなら, プレイヤー名を表示する。
        }
        catch { }
    }
}