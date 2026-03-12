using Microsoft.AspNetCore.Mvc;
using WordCounter.Api.Interfaces;
using WordCounter.Api.Models;
using WordCounter.Domain.Enums;

namespace WordCounter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController(IWordCountingService service) : ControllerBase
{
    [HttpPost("html")]
    public async Task<ActionResult<AnalysisResponse>> AnalyzeHtml(AnalysisRequest request, CancellationToken ct)
    {
        var results = await service.Analyze(request.Urls, ContentType.Html, ct);
        return Ok(new AnalysisResponse 
        { 
            Results = results.Select(r => new AnalysisResultDto(r.Url, r.WordCount, r.WordFrequencies, r.Status, r.Duration)) 
        });
    }

    [HttpPost("json")]
    public async Task<ActionResult<AnalysisResponse>> AnalyzeJson(AnalysisRequest request, CancellationToken ct)
    {
        var results = await service.Analyze(request.Urls, ContentType.Json, ct);
        return Ok(new AnalysisResponse 
        { 
            Results = results.Select(r => new AnalysisResultDto(r.Url, r.WordCount, r.WordFrequencies, r.Status, r.Duration)) 
        });
    }

    [HttpPost("xml")]
    public async Task<ActionResult<AnalysisResponse>> AnalyzeXml(AnalysisRequest request, CancellationToken ct)
    {
        var results = await service.Analyze(request.Urls, ContentType.Xml, ct);
        return Ok(new AnalysisResponse 
        { 
            Results = results.Select(r => new AnalysisResultDto(r.Url, r.WordCount, r.WordFrequencies, r.Status, r.Duration)) 
        });
    }
}
