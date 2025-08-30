namespace MyFinances.Logic.Configuration;
public class FmpConfig
{
    public required string BaseAddress { get; set; }
    public required string ApiKey { get; set; }
}

public class AppConfig
{
    public required FmpConfig Fmp { get; set; }
}
