using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchChatAPI.Enums;
using TwitchChatAPI.Objects;

namespace TwitchChatAPI.Helpers;

internal static class MessageHelper
{
    public static void ProcessMessage(string message)
    {
        try
        {
            if (message.StartsWith("@") && message.Contains("PRIVMSG"))
            {
                ProcessMessage_PRIVMSG(message);
            }
            else if (message.StartsWith("@") && message.Contains("USERNOTICE"))
            {
                ProcessMessage_USERNOTICE(message);
            }
            else if (message.StartsWith("@") && message.Contains("ROOMSTATE"))
            {
                ProcessMessage_ROOMSTATE(message);
            }
            else
            {
                Logger.LogInfo($"Unhandled RAW message: {message}", extended: true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_PRIVMSG(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag[(tag.IndexOf('=') + 1)..] : string.Empty
            );

            string contentSection = message.Split("PRIVMSG")[1];
            string channel = contentSection.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

            UserHelper.UpdateUser(twitchUser);

            if (tags.ContainsKey("bits"))
            {
                ProcessMessage_PRIVMSG_Cheer(message, channel, twitchUser, chatMessage, tags);
                return;
            }

            var twitchMessage = new TwitchMessage
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags // Retain raw tags for extensibility
            };

            //Plugin.Instance.LogInfoExtended($"\n{JsonConvert.SerializeObject(twitchMessage, Formatting.Indented)}");

            API.InvokeOnMessage(twitchMessage);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_PRIVMSG_Cheer(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            var cheerTypes = new string[]
            {
                "Cheer", "cheerwhal", "Corgo", "uni", "ShowLove", "Party", "SeemsGood", "Pride",
                "Kappa", "FrankerZ", "HeyGuys", "DansGame", "EleGiggle", "TriHard", "Kreygasm",
                "4Head", "SwiftRage", "NotLikeThis", "FailFish", "VoHiYo", "PJSalt", "MrDestructoid",
                "bday", "RIPCheer", "Shamrock"
            };

            string pattern = @"\b(" + string.Join("|", cheerTypes) + @")\d+\b";

            chatMessage = Regex.Replace(chatMessage, pattern, string.Empty, RegexOptions.IgnoreCase).Trim();

            var cheerEvent = new TwitchCheerEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                CheerAmount = int.Parse(tags.GetValueOrDefault("bits", "0"))
            };

            Logger.LogInfo($"RAW cheer message: {message}", extended: true);
            Logger.LogInfo($"[!] Cheer event: {cheerEvent.User.DisplayName} cheered {cheerEvent.CheerAmount} bits!\n{JsonConvert.SerializeObject(cheerEvent, Formatting.Indented)}", extended: true);

            API.InvokeOnCheer(cheerEvent);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag[(tag.IndexOf('=') + 1)..] : string.Empty
            );

            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}");
                return;
            }

            string contentSection = message.Split("USERNOTICE")[1];
            string channel = contentSection.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

            UserHelper.UpdateUser(twitchUser);

            if (msgId == "sub" || msgId == "resub" || msgId == "subgift" || msgId == "submysterygift")
            {
                ProcessMessage_USERNOTICE_Sub(message, channel, twitchUser, chatMessage, tags);
            }
            else if (msgId == "raid")
            {
                ProcessMessage_USERNOTICE_Raid(message, channel, twitchUser, tags);
            }
            else
            {
                Logger.LogInfo($"Unhandled USERNOTICE message: {message}", extended: true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Sub(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Logger.LogError($"Failed to process USERNOTICE message: {message}");
                return;
            }

            SubType subType = SubType.Sub;

            switch (msgId)
            {
                case "resub":
                    subType = SubType.Resub;
                    break;
                case "subgift":
                    subType = SubType.SubGift;
                    break;
                case "submysterygift":
                    subType = SubType.SubMysteryGift;
                    break;
            }

            if (subType == SubType.SubGift && tags.ContainsKey("msg-param-community-gift-id"))
            {
                Logger.LogInfo($"Skipping subgift since it originates from a submysterygift. Message: {message}", extended: true);
                return;
            }

            SubTier tier = SubTier.One;

            if (tags.TryGetValue("msg-param-sub-plan", out string subPlan) && !string.IsNullOrEmpty(subPlan))
            {
                if (subPlan == "Prime")
                {
                    tier = SubTier.Prime;
                }
                else if (subPlan == "1000")
                {
                    tier = SubTier.One;
                }
                else if (subPlan == "2000")
                {
                    tier = SubTier.Two;
                }
                else if (subPlan == "3000")
                {
                    tier = SubTier.Three;
                }
            }

            var subEvent = new TwitchSubEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                Type = subType,
                Tier = tier,
                CumulativeMonths = int.Parse(tags.GetValueOrDefault("msg-param-cumulative-months", defaultValue: "0")),
                RecipientUser = tags.GetValueOrDefault("msg-param-recipient-display-name", defaultValue: string.Empty),
                GiftCount = int.Parse(tags.GetValueOrDefault("msg-param-mass-gift-count", defaultValue: "0"))
            };

            Logger.LogInfo($"RAW subscription message: {message}", extended: true);
            Logger.LogInfo($"[!] Subscription event: \n{JsonConvert.SerializeObject(subEvent, Formatting.Indented)}", extended: true);

            API.InvokeOnSub(subEvent);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Raid(string message, string channel, TwitchUser twitchUser, Dictionary<string, string> tags)
    {
        try
        {
            var raidEvent = new TwitchRaidEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = string.Empty,
                Tags = tags,
                ViewerCount = int.Parse(tags.GetValueOrDefault("msg-param-viewerCount", "0"))
            };

            Logger.LogInfo($"RAW raid message: {message}", extended: true);
            Logger.LogInfo($"[!] Raid detected: {raidEvent.User.DisplayName} is raiding with {raidEvent.ViewerCount} viewers!\n{JsonConvert.SerializeObject(raidEvent, Formatting.Indented)}", extended: true);

            API.InvokeOnRaid(raidEvent);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_ROOMSTATE(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            string channel = message.Split("ROOMSTATE")[1].Trim().TrimStart('#');

            var roomState = new TwitchRoomState
            {
                Channel = channel,
                IsEmoteOnly = tags.ContainsKey("emote-only") && tags["emote-only"] == "1",
                IsFollowersOnly = tags.ContainsKey("followers-only") && tags["followers-only"] != "-1",
                IsR9K = tags.ContainsKey("r9k") && tags["r9k"] == "1",
                IsSlowMode = tags.ContainsKey("slow") && tags["slow"] != "0",
                IsSubsOnly = tags.ContainsKey("subs-only") && tags["subs-only"] == "1",
                Tags = tags
            };

            Logger.LogInfo($"RAW roomstate message: {message}", extended: true);
            Logger.LogInfo($"[!] Room state change detected: \n{JsonConvert.SerializeObject(roomState, Formatting.Indented)}", extended: true);

            API.InvokeOnRoomStateUpdate(roomState);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process ROOMSTATE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static TwitchUser GetTwitchUser(string channel, Dictionary<string, string> tags)
    {
        try
        {
            string displayName = tags.GetValueOrDefault("display-name", "Anonymous");

            string color = tags.GetValueOrDefault("color", "#FFFFFF");

            if (string.IsNullOrEmpty(color))
            {
                color = "#FFFFFF";
            }

            return new TwitchUser
            {
                UserId = tags.GetValueOrDefault("user-id", defaultValue: "0"),
                Username = displayName.ToLower(),
                DisplayName = displayName,
                Color = color,
                IsVIP = tags.TryGetValue("vip", out var vip) && vip == "1",
                IsSubscriber = tags.TryGetValue("subscriber", out var sub) && sub == "1",
                IsModerator = tags.TryGetValue("mod", out var mod) && mod == "1",
                IsBroadcaster = displayName.Equals(channel, StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to get TwitchUser: {ex}");
            return default;
        }
    }
}
