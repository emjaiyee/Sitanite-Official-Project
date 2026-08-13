public static class GatewayDirectionExtensions
{
    public static GatewayDirection Opposite(
        this GatewayDirection direction)
    {
        switch (direction)
        {
            case GatewayDirection.NorthWest:
                return GatewayDirection.SouthEast;

            case GatewayDirection.NorthEast:
                return GatewayDirection.SouthWest;

            case GatewayDirection.SouthWest:
                return GatewayDirection.NorthEast;

            case GatewayDirection.SouthEast:
                return GatewayDirection.NorthWest;

            default:
                throw new System.ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    "Unknown GatewayDirection."
                );
        }
    }
}
