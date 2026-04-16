namespace MyFinances.Logic.Configuration;

public class FmpConfig
{
    public required string BaseAddress { get; set; }
    public required string ApiKey { get; set; }
}

public class MarketHoursConfig
{
    public string OpenTimeUtc { get; set; } = "14:30";
    public string CloseTimeUtc { get; set; } = "21:00";
    public string TimeZone { get; set; } = "UTC";
    public string? Comment { get; set; }

    public TimeSpan GetOpenTime()
    {
        return TimeSpan.Parse(OpenTimeUtc);
    }

    public TimeSpan GetCloseTime()
    {
        return TimeSpan.Parse(CloseTimeUtc);
    }

    /// <summary>
    /// Determina si el mercado está abierto en el momento actual (UTC)
    /// </summary>
    public bool IsMarketOpen()
    {
        return IsMarketOpenAt(DateTime.UtcNow);
    }

    /// <summary>
    /// Determina si el mercado está abierto en una fecha/hora específica (UTC)
    /// </summary>
    public bool IsMarketOpenAt(DateTime dateTimeUtc)
    {
        // Convertir a solo hora
        var currentTime = dateTimeUtc.TimeOfDay;
        var openTime = GetOpenTime();
        var closeTime = GetCloseTime();

        // Verificar que no sea fin de semana
        if (dateTimeUtc.DayOfWeek == DayOfWeek.Saturday || dateTimeUtc.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        // Verificar si está dentro del horario de mercado
        return currentTime >= openTime && currentTime < closeTime;
    }

    /// <summary>
    /// Obtiene la fecha efectiva para consultar datos históricos.
    /// Si el mercado está abierto, retorna el día hábil anterior.
    /// Si el mercado está cerrado, retorna el día actual.
    /// </summary>
    public DateTime GetEffectiveDataDate()
    {
        return GetEffectiveDataDateAt(DateTime.UtcNow);
    }

    /// <summary>
    /// Obtiene la fecha efectiva para consultar datos históricos en una fecha específica.
    /// </summary>
    public DateTime GetEffectiveDataDateAt(DateTime dateTimeUtc)
    {
        var effectiveDate = dateTimeUtc.Date;

        // Si el mercado está abierto, usar el día hábil anterior
        if (IsMarketOpenAt(dateTimeUtc))
        {
            effectiveDate = GetPreviousBusinessDay(effectiveDate);
        }

        // Si es fin de semana, retroceder al viernes
        while (effectiveDate.DayOfWeek == DayOfWeek.Saturday || effectiveDate.DayOfWeek == DayOfWeek.Sunday)
        {
            effectiveDate = effectiveDate.AddDays(-1);
        }

        return effectiveDate;
    }

    /// <summary>
    /// Obtiene el día hábil anterior (excluye fines de semana)
    /// </summary>
    private DateTime GetPreviousBusinessDay(DateTime date)
    {
        var previousDay = date.AddDays(-1);

        // Si es domingo, retroceder al viernes
        if (previousDay.DayOfWeek == DayOfWeek.Sunday)
        {
            return previousDay.AddDays(-2);
        }

        // Si es sábado, retroceder al viernes
        if (previousDay.DayOfWeek == DayOfWeek.Saturday)
        {
            return previousDay.AddDays(-1);
        }

        return previousDay;
    }
}

public class AppConfig
{
    public required FmpConfig Fmp { get; set; }
    public MarketHoursConfig MarketHours { get; set; } = new MarketHoursConfig();
}
