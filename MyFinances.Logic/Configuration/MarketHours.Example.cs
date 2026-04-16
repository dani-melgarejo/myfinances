using Microsoft.Extensions.Options;

namespace MyFinances.Logic.Configuration;

/// <summary>
/// Ejemplos de uso de la configuración de horarios de mercado
/// </summary>
public class MarketHoursExamples
{
    private readonly MarketHoursConfig _marketHours;

    public MarketHoursExamples(IOptions<AppConfig> appConfig)
    {
        _marketHours = appConfig.Value.MarketHours;
    }

    /// <summary>
    /// Ejemplo 1: Verificar si el mercado está abierto ahora
    /// </summary>
    public void Example1_CheckIfMarketIsOpen()
    {
        bool isOpen = _marketHours.IsMarketOpen();
        
        if (isOpen)
        {
            Console.WriteLine("🟢 El mercado está ABIERTO");
            Console.WriteLine("Los datos del día actual pueden no estar completos");
        }
        else
        {
            Console.WriteLine("🔴 El mercado está CERRADO");
            Console.WriteLine("Puedes consultar datos hasta el día de hoy");
        }
    }

    /// <summary>
    /// Ejemplo 2: Obtener la fecha efectiva para consultar datos
    /// </summary>
    public void Example2_GetEffectiveDate()
    {
        DateTime effectiveDate = _marketHours.GetEffectiveDataDate();
        
        Console.WriteLine($"📅 Fecha efectiva para consultar datos: {effectiveDate:yyyy-MM-dd}");
        
        // Esta fecha considera:
        // - Si el mercado está abierto -> día hábil anterior
        // - Si el mercado está cerrado -> día actual
        // - Excluye fines de semana automáticamente
    }

    /// <summary>
    /// Ejemplo 3: Verificar estado del mercado en una fecha/hora específica
    /// </summary>
    public void Example3_CheckMarketAtSpecificTime()
    {
        // Verificar si el mercado estaba abierto el lunes a las 16:00 UTC
        var testDateTime = new DateTime(2026, 4, 14, 16, 0, 0, DateTimeKind.Utc);
        bool wasOpen = _marketHours.IsMarketOpenAt(testDateTime);
        
        Console.WriteLine($"Fecha de prueba: {testDateTime:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"Mercado estaba: {(wasOpen ? "ABIERTO ✓" : "CERRADO ✗")}");
    }

    /// <summary>
    /// Ejemplo 4: Obtener fecha efectiva para una fecha específica
    /// </summary>
    public void Example4_GetEffectiveDateForSpecificTime()
    {
        // Escenario 1: Lunes a las 16:00 UTC (mercado abierto)
        var scenario1 = new DateTime(2026, 4, 14, 16, 0, 0, DateTimeKind.Utc);
        var effective1 = _marketHours.GetEffectiveDataDateAt(scenario1);
        
        Console.WriteLine("Escenario 1 - Mercado ABIERTO:");
        Console.WriteLine($"  Fecha consulta: {scenario1:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"  Fecha efectiva: {effective1:yyyy-MM-dd}");
        Console.WriteLine($"  (Retrocede al viernes: {effective1.DayOfWeek})");
        Console.WriteLine();

        // Escenario 2: Lunes a las 22:00 UTC (mercado cerrado)
        var scenario2 = new DateTime(2026, 4, 14, 22, 0, 0, DateTimeKind.Utc);
        var effective2 = _marketHours.GetEffectiveDataDateAt(scenario2);
        
        Console.WriteLine("Escenario 2 - Mercado CERRADO:");
        Console.WriteLine($"  Fecha consulta: {scenario2:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"  Fecha efectiva: {effective2:yyyy-MM-dd}");
        Console.WriteLine($"  (Usa día actual: {effective2.DayOfWeek})");
    }

    /// <summary>
    /// Ejemplo 5: Uso en un servicio de actualización de datos
    /// </summary>
    public async Task Example5_UpdateHistoricalData(int assetId)
    {
        // Determinar desde cuándo obtener datos
        DateTime fromDate = DateTime.Today.AddMonths(-1);
        
        // Obtener la fecha hasta la cual traer datos
        DateTime toDate = _marketHours.GetEffectiveDataDate();
        
        bool isMarketOpen = _marketHours.IsMarketOpen();
        
        Console.WriteLine($"📊 Actualizando datos históricos:");
        Console.WriteLine($"   Asset ID: {assetId}");
        Console.WriteLine($"   Desde: {fromDate:yyyy-MM-dd}");
        Console.WriteLine($"   Hasta: {toDate:yyyy-MM-dd}");
        Console.WriteLine($"   Estado mercado: {(isMarketOpen ? "ABIERTO 🟢" : "CERRADO 🔴")}");
        
        if (isMarketOpen)
        {
            Console.WriteLine($"   ⚠️ Mercado abierto - usando día hábil anterior");
        }
        
        // Aquí iría la lógica de actualización de datos
        // await _historicService.GetPostsAsync(assetId, fromDate, toDate);
    }

    /// <summary>
    /// Ejemplo 6: Logging detallado del estado del mercado
    /// </summary>
    public void Example6_LogMarketStatus()
    {
        var now = DateTime.UtcNow;
        var openTime = _marketHours.GetOpenTime();
        var closeTime = _marketHours.GetCloseTime();
        var isOpen = _marketHours.IsMarketOpen();
        var effectiveDate = _marketHours.GetEffectiveDataDate();
        
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("       ESTADO DEL MERCADO - NYSE/NASDAQ");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine($"Hora actual UTC:     {now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Horario apertura:    {openTime}");
        Console.WriteLine($"Horario cierre:      {closeTime}");
        Console.WriteLine($"Estado:              {(isOpen ? "🟢 ABIERTO" : "🔴 CERRADO")}");
        Console.WriteLine($"Día de la semana:    {now.DayOfWeek}");
        Console.WriteLine($"Fecha efectiva:      {effectiveDate:yyyy-MM-dd}");
        Console.WriteLine("═══════════════════════════════════════════");
    }

    /// <summary>
    /// Ejemplo 7: Validar antes de permitir operaciones
    /// </summary>
    public bool Example7_CanPerformTrade()
    {
        bool isMarketOpen = _marketHours.IsMarketOpen();
        
        if (!isMarketOpen)
        {
            Console.WriteLine("❌ No se pueden realizar operaciones - Mercado cerrado");
            return false;
        }
        
        var now = DateTime.UtcNow;
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
        {
            Console.WriteLine("❌ No se pueden realizar operaciones - Fin de semana");
            return false;
        }
        
        Console.WriteLine("✅ Operaciones permitidas - Mercado abierto");
        return true;
    }

    /// <summary>
    /// Ejemplo 8: Calcular próxima apertura del mercado
    /// </summary>
    public DateTime Example8_GetNextMarketOpen()
    {
        var now = DateTime.UtcNow;
        var openTime = _marketHours.GetOpenTime();
        
        // Si hoy el mercado aún no abrió
        if (now.TimeOfDay < openTime && 
            now.DayOfWeek != DayOfWeek.Saturday && 
            now.DayOfWeek != DayOfWeek.Sunday)
        {
            var nextOpen = now.Date.Add(openTime);
            Console.WriteLine($"⏰ Próxima apertura: HOY {nextOpen:yyyy-MM-dd HH:mm} UTC");
            return nextOpen;
        }
        
        // Buscar el próximo día hábil
        var nextDay = now.Date.AddDays(1);
        while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }
        
        var nextOpenDateTime = nextDay.Add(openTime);
        Console.WriteLine($"⏰ Próxima apertura: {nextOpenDateTime:yyyy-MM-dd HH:mm} UTC ({nextDay.DayOfWeek})");
        
        return nextOpenDateTime;
    }

    /// <summary>
    /// Ejemplo 9: Determinar si necesita actualización de datos
    /// </summary>
    public bool Example9_NeedsDataUpdate(DateTime lastDataDate)
    {
        var effectiveDate = _marketHours.GetEffectiveDataDate();
        
        if (lastDataDate >= effectiveDate)
        {
            Console.WriteLine($"✓ Datos actualizados (última fecha: {lastDataDate:yyyy-MM-dd})");
            return false;
        }
        
        Console.WriteLine($"⚠️ Datos desactualizados");
        Console.WriteLine($"   Última fecha en DB: {lastDataDate:yyyy-MM-dd}");
        Console.WriteLine($"   Fecha efectiva:     {effectiveDate:yyyy-MM-dd}");
        Console.WriteLine($"   Días de diferencia: {(effectiveDate - lastDataDate).Days}");
        
        return true;
    }

    /// <summary>
    /// Ejemplo 10: Uso completo en un controlador o servicio
    /// </summary>
    public async Task<string> Example10_CompleteUsageExample(int assetId, DateTime? lastDataDate)
    {
        // 1. Verificar estado del mercado
        bool isMarketOpen = _marketHours.IsMarketOpen();
        DateTime effectiveDate = _marketHours.GetEffectiveDataDate();
        
        // 2. Determinar si necesita actualización
        if (lastDataDate.HasValue && lastDataDate.Value >= effectiveDate)
        {
            return $"Asset {assetId} ya está actualizado hasta {lastDataDate.Value:yyyy-MM-dd}";
        }
        
        // 3. Calcular rango de fechas
        DateTime fromDate = lastDataDate?.AddDays(1) ?? DateTime.Today.AddYears(-1);
        DateTime toDate = effectiveDate;
        
        // 4. Log de la operación
        string status = isMarketOpen ? "ABIERTO" : "CERRADO";
        string logMessage = $@"
╔════════════════════════════════════════════════════════╗
║  ACTUALIZACIÓN DE DATOS HISTÓRICOS                     ║
╠════════════════════════════════════════════════════════╣
║  Asset ID:         {assetId,-35} ║
║  Estado mercado:   {status,-35} ║
║  Fecha desde:      {fromDate,-35:yyyy-MM-dd} ║
║  Fecha hasta:      {toDate,-35:yyyy-MM-dd} ║
║  Hora actual UTC:  {DateTime.UtcNow,-35:HH:mm:ss} ║
╚════════════════════════════════════════════════════════╝
";
        
        Console.WriteLine(logMessage);
        
        // 5. Realizar la actualización
        // await _historicService.GetPostsAsync(assetId, fromDate, toDate);
        
        return $"Datos actualizados para asset {assetId} desde {fromDate:yyyy-MM-dd} hasta {toDate:yyyy-MM-dd}";
    }
}
