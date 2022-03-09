﻿using BepInEx.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SuperNewRoles.Mode.SuperHostRoles { 
    public static class NotBlackOut
    {

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
        class CheckForEndVotingPatch
        {
            public static void Prefix(MeetingHud __instance)
            {
                if (Mode.ModeHandler.isMode(Mode.ModeId.SuperHostRoles))
                {
                    EndMeetingPatch();
                }
            }

        }
        public static void EndMeetingPatch()
        {
            //霊界用暗転バグ対処
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.isRole(CustomRPC.RoleId.Egoist) && pc.Data.IsDead) pc.ResetPlayerCam(12.5f);
        }
		public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
		{
			if ((UnityEngine.Object)(object)pc == (UnityEngine.Object)null || !AmongUsClient.Instance.AmHost || pc.AmOwner)
			{
				return;
			}
			int clientId = pc.getClientId();
			byte reactorId = 3;
			if (PlayerControl.GameOptions.MapId == 2)
			{
				reactorId = 21;
			}
			AmongUsClient.Instance.StartCoroutine(ReactorDesync());
			AmongUsClient.Instance.StartCoroutine(MurderToResetCam());
			AmongUsClient.Instance.StartCoroutine(FixDesyncReactor());
			IEnumerator ReactorDesync()
			{
				yield return new WaitForSeconds(delay);
				MessageWriter val4 = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, 28, (SendOption)1, clientId);
				val4.Write(reactorId);
				val4.WriteNetObject(pc);
				val4.Write((byte)128);
				AmongUsClient.Instance.FinishRpcImmediately(val4);
			}
			IEnumerator MurderToResetCam()
			{
				yield return new WaitForSeconds(0.2f+delay);
				MessageWriter val3 = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, 12, (SendOption)1, clientId);
				val3.WriteNetObject(pc);
				AmongUsClient.Instance.FinishRpcImmediately(val3);
			}
			IEnumerator FixDesyncReactor()
			{
				yield return new WaitForSeconds(0.4f + delay);
				MessageWriter val2 = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, 28, (SendOption)1, clientId);
				val2.Write(reactorId);
				val2.WriteNetObject(pc);
				val2.Write((byte)16);
				AmongUsClient.Instance.FinishRpcImmediately(val2);
			};
			if (PlayerControl.GameOptions.MapId == 4)
			{
				AmongUsClient.Instance.StartCoroutine(FixDesyncReactor2());
				IEnumerator FixDesyncReactor2()
				{
					yield return new WaitForSeconds(0.4f + delay);
					MessageWriter val = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, 28, (SendOption)1, clientId);
					val.Write(reactorId);
					val.WriteNetObject(pc);
					val.Write((byte)17);
					AmongUsClient.Instance.FinishRpcImmediately(val);
				}
			}
		}
	}
}
