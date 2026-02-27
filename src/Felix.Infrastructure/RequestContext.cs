namespace Felix.Infrastructure;

public interface IRequestContext
{
    LocationInfo? Location { get; }
    void SetLocation(double latitude, double longitude);
}

public class RequestContext : IRequestContext
{
    public LocationInfo? Location { get; private set; }

    public void SetLocation(double latitude, double longitude)
    {
        Location = new LocationInfo(latitude, longitude);
    }
}

public record LocationInfo(double Latitude, double Longitude);
