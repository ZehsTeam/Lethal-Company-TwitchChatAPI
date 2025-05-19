# v2.0.0

- Changed root namespace from `com.github.zehsteam.TwitchChatAPI` to `TwitchChatAPI`.
- Changed plugin guid from `com.github.zehsteam.TwitchChatAPI` to `TwitchChatAPI`.
- Removed all references to Lethal Company to make this mod work for other games.
- Renamed `SubType` to `Type` in `TwitchSubEvent`.
- Removed `IsPrime` bool from `TwitchSubEvent` and added `Prime` option to the `SubTier` enum.
- Added `Channel` string to the API.
- Added `OnConnectionStateChanged` event to the API.
- Added `Connect`, `Connect(string channel)`, and `Disconnect()` methods to the API.
- Added extension methods to `TwitchUser` for getting the DisplayName with color as a rich text string.

## v1.1.0

- Updated [`TwitchSubEvent`](https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchEvents.cs#L14) class.
  - Changed `Tier` from an int to an enum.
  - Renamed `Months` to `CumulativeMonths`.
- Updated [`TwitchUser`](https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchUser.cs) class.
  - Added `Username` property.
- Updated [`API`](https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/API.cs).
  - Added `public static IReadOnlyCollection<TwitchUser> Users { get; }` property.
  - Added `public static bool TryGetUserByUsername(string username, out TwitchUser twitchUser);` method.
  - Added `public static bool TryGetUserByUserId(string userId, out TwitchUser twitchUser);` method.
  - Added `public static TwitchUser[] GetUsersSeenWithin(TimeSpan timeSpan);` method.

## v1.0.1

- API events are now invoked on the main thread.
- Bug fixes and improvements.

## v1.0.0

- Initial release.