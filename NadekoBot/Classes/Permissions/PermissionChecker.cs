﻿using Discord.Commands.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections.Concurrent;

namespace NadekoBot.Classes.Permissions {
    class PermissionChecker : IPermissionChecker {
        public static readonly PermissionChecker _instance = new PermissionChecker();
        public static PermissionChecker Instance => _instance;

        private ConcurrentDictionary<User, DateTime> timeBlackList { get; } = new ConcurrentDictionary<User, DateTime>();

        static PermissionChecker() { }
        public PermissionChecker() {
            Task.Run(async () => {
                while (true) {
                    //blacklist is cleared every 1.3 seconds. That is the most time anyone will be blocked for ever
                    await Task.Delay(1300);
                    timeBlackList.Clear();
                }
            });
        }

        public bool CanRun(Command command, User user, Channel channel, out string error) {
            error = String.Empty;

            if (timeBlackList.ContainsKey(user))
                return false;

            if (channel.IsPrivate)
                return false;

            timeBlackList.TryAdd(user, DateTime.Now);

            try {
                //is it a permission command?
                // if it is, check if the user has the correct role
                // if yes return true, if no return false
                if (command.Category == "Permissions") {
                    Discord.Role role = null;
                    try {
                        role = PermissionHelper.ValidateRole(user.Server, PermissionsHandler.GetServerPermissionsRoleName(user.Server));
                    } catch { }
                    if (user.Server.Owner.Id == user.Id || (role != null && user.HasRole(role)))
                        return true;
                    throw new Exception($"You don't have the necessary role (**{PermissionsHandler._permissionsDict[user.Server].PermissionsControllerRole}**) to change permissions.");
                }

                var permissionType = PermissionsHandler.GetPermissionBanType(command, user, channel);

                string msg;

                switch (permissionType) {
                    case PermissionsHandler.PermissionBanType.None:
                        return true;
                    case PermissionsHandler.PermissionBanType.ServerBanCommand:
                        msg = $"**{command.Text}** command has been banned from use on this **server**.";
                        break;
                    case PermissionsHandler.PermissionBanType.ServerBanModule:
                        msg = $"**{command.Category}** module has been banned from use on this **server**.";
                        break;
                    case PermissionsHandler.PermissionBanType.ChannelBanCommand:
                        msg = $"**{command.Text}** command has been banned from use on this **channel**.";
                        break;
                    case PermissionsHandler.PermissionBanType.ChannelBanModule:
                        msg = $"**{command.Category}** module has been banned from use on this **channel**.";
                        break;
                    case PermissionsHandler.PermissionBanType.RoleBanCommand:
                        msg = $"You do not have a **role** which permits you the usage of **{command.Text}** command.";
                        break;
                    case PermissionsHandler.PermissionBanType.RoleBanModule:
                        msg = $"You do not have a **role** which permits you the usage of **{command.Category}** module.";
                        break;
                    case PermissionsHandler.PermissionBanType.UserBanCommand:
                        msg = $"{user.Mention}, You have been banned from using **{command.Text}** command.";
                        break;
                    case PermissionsHandler.PermissionBanType.UserBanModule:
                        msg = $"{user.Mention}, You have been banned from using **{command.Category}** module.";
                        break;
                    default:
                        return true;
                }
                if (PermissionsHandler._permissionsDict[user.Server].Verbose) //if verbose - print errors
                    error = msg;
                return false;
            } catch (Exception ex) {
                if (PermissionsHandler._permissionsDict[user.Server].Verbose) //if verbose - print errors
                    error = ex.Message;
                return false;
            }
        }
    }
}
