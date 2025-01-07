using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.github.zehsteam.TwitchChatAPI.Helpers;

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
                Plugin.Instance.LogInfoExtended($"Unhandled RAW message: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process message:\n\n{message}\n\nError: {ex}");
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
            string channel = contentSection.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

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
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
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

            Plugin.Instance.LogInfoExtended($"RAW cheer message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Cheer event: {cheerEvent.User.DisplayName} cheered {cheerEvent.CheerAmount} bits!\n{JsonConvert.SerializeObject(cheerEvent, Formatting.Indented)}");

            API.InvokeOnCheer(cheerEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
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
                Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}");
                return;
            }

            string contentSection = message.Split("USERNOTICE")[1];
            string channel = contentSection.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

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
                Plugin.Instance.LogInfoExtended($"Unhandled USERNOTICE message: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Sub(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Plugin.Logger.LogError($"Failed to process USERNOTICE message: {message}");
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
                Plugin.Instance.LogInfoExtended($"Skipping subgift since it originates from a submysterygift. Message: {message}");
                return;
            }

            bool isPrime = false;
            int tier = 1;

            if (tags.TryGetValue("msg-param-sub-plan", out string subPlan) && !string.IsNullOrEmpty(subPlan))
            {
                if (subPlan == "Prime")
                {
                    isPrime = true;
                }
                else if (subPlan == "2000")
                {
                    tier = 2;
                }
                else if (subPlan == "3000")
                {
                    tier = 3;
                }
            }

            var subEvent = new TwitchSubEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                SubType = subType,
                IsPrime = isPrime,
                Tier = tier,
                Months = int.Parse(tags.GetValueOrDefault("msg-param-cumulative-months", defaultValue: "0")),
                RecipientUser = tags.GetValueOrDefault("msg-param-recipient-display-name", defaultValue: string.Empty),
                GiftCount = int.Parse(tags.GetValueOrDefault("msg-param-mass-gift-count", defaultValue: "0"))
            };

            Plugin.Instance.LogInfoExtended($"RAW subscription message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Subscription event: \n{JsonConvert.SerializeObject(subEvent, Formatting.Indented)}");

            API.InvokeOnSub(subEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
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

            Plugin.Instance.LogInfoExtended($"RAW raid message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Raid detected: {raidEvent.User.DisplayName} is raiding with {raidEvent.ViewerCount} viewers!\n{JsonConvert.SerializeObject(raidEvent, Formatting.Indented)}");

            API.InvokeOnRaid(raidEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
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

            Plugin.Instance.LogInfoExtended($"RAW roomstate message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Room state change detected: \n{JsonConvert.SerializeObject(roomState, Formatting.Indented)}");

            API.InvokeOnRoomStateUpdate(roomState);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process ROOMSTATE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static TwitchUser GetTwitchUser(string channel, Dictionary<string, string> tags)
    {
        try
        {
            string displayName = tags.GetValueOrDefault("display-name", "Anonymous");

            return new TwitchUser
            {
                UserId = tags.GetValueOrDefault("user-id", defaultValue: "0"),
                DisplayName = displayName,
                Color = tags.GetValueOrDefault("color", "#FFFFFF"),
                IsVIP = tags.TryGetValue("vip", out var vip) && vip == "1",
                IsSubscriber = tags.TryGetValue("subscriber", out var sub) && sub == "1",
                IsModerator = tags.TryGetValue("mod", out var mod) && mod == "1",
                IsBroadcaster = displayName.Equals(channel, System.StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to get TwitchUser: {ex}");
            return default;
        }
    }
}
