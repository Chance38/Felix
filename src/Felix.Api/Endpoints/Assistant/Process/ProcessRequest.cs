namespace Felix.Api.Endpoints.Assistant.Process;

public class ProcessRequest
{
    public string? Message { get; set; }
    public LocationRequest? Location { get; set; }
}

public class LocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
