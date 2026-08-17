using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.DTOs.Response;

public class AnalyticsTimeseriesResponse
{
    public string Range { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public List<AnalyticsTimeseriesPointResponse> Points { get; set; }
        = new();
}

public class AnalyticsTimeseriesPointResponse
{
    public DateTime Date { get; set; }

    public decimal Value { get; set; }
}
