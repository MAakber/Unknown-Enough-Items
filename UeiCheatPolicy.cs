internal enum TogetherNetworkState
{
    NotInstalled,
    Offline,
    Client,
    Server,
    Unknown,
}

internal enum UeiGiveRoute
{
    Unavailable,
    LocalFactory,
    ServerCommand,
    HostRequired,
}

internal static class UeiCheatPolicy
{
    internal static bool HasTogetherAccess(TogetherNetworkState state, bool? adminGranted, bool? serverCheats)
    {
        return state switch
        {
            TogetherNetworkState.NotInstalled or TogetherNetworkState.Offline => true,
            // AllowClientCheatCommands controls local, unsynchronized cheats.
            // The host validates sv_cheats again when processing our admin command.
            TogetherNetworkState.Client => adminGranted == true,
            TogetherNetworkState.Server => serverCheats == true,
            _ => false,
        };
    }

    internal static UeiGiveRoute GiveRoute(TogetherNetworkState state, bool customItem, bool resourceItem)
    {
        if (!customItem && !resourceItem)
            return UeiGiveRoute.Unavailable;

        // Native give uses Resources.Load, not the mod-aware Utils.Create.
        // A mod override sharing a vanilla ID must not receive the wrong item.
        return state switch
        {
            TogetherNetworkState.Client => customItem ? UeiGiveRoute.HostRequired : UeiGiveRoute.ServerCommand,
            TogetherNetworkState.Server => customItem ? UeiGiveRoute.LocalFactory : UeiGiveRoute.ServerCommand,
            TogetherNetworkState.NotInstalled or TogetherNetworkState.Offline => UeiGiveRoute.LocalFactory,
            _ => UeiGiveRoute.Unavailable,
        };
    }
}
