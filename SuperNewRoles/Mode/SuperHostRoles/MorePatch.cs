using HarmonyLib;
using SuperNewRoles.CustomRPC;
using SuperNewRoles.Roles;

namespace SuperNewRoles.Mode.SuperHostRoles
{
    class MorePatch
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
        class PlayerControlCompleteTaskPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                FixedUpdate.SetRoleName(__instance);
            }
        }

        public static bool RepairSystem(
                SystemTypes systemType,
                PlayerControl player)
        {
            if (systemType == SystemTypes.Sabotage && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
            {
                if ((player.isRole(RoleId.Jackal) && !RoleClass.Jackal.IsUseSabo) || player.isRole(RoleId.Demon, RoleId.Arsonist, RoleId.RemoteSheriff, RoleId.Sheriff,
                    RoleId.truelover, RoleId.FalseCharges, RoleId.MadMaker, RoleId.ToiletFan)
                    || (!RoleClass.Minimalist.UseSabo && player.isRole(RoleId.Minimalist))
                    || (!RoleClass.Samurai.UseSabo && player.isRole(RoleId.Samurai))
                    || (!RoleClass.Egoist.UseSabo && player.isRole(RoleId.Egoist))) return false;
            }
            if (PlayerControl.LocalPlayer.IsUseVent() && RoleHelpers.IsComms())
            {
                if (BattleRoyal.Main.VentData.ContainsKey(CachedPlayer.LocalPlayer.PlayerId))
                {
                    var data = BattleRoyal.Main.VentData[CachedPlayer.LocalPlayer.PlayerId];
                    if (data != null)
                    {
                        PlayerControl.LocalPlayer.MyPhysics.RpcExitVent((int)data);
                    }
                }
            }
            return true;
        }
        public static void StartMeeting()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            FixedUpdate.SetRoleNames(true);
            RoleClass.IsMeeting = true;
            new LateTask(() =>
            {
                FixedUpdate.SetDefaultNames();
            }, 5f, "SetMeetingName");
        }
        public static void MeetingEnd()
        {
        }
    }
}