using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeminarController : ControllerBase
    {
        private readonly IAudioSummaryService _audioSummaryService;

        public SeminarController(IAudioSummaryService audioSummaryService)
        {
            _audioSummaryService = audioSummaryService;
        }

        [HttpPost("{id:int}/summarize-audio")]
        [RequestSizeLimit(524_288_000)] // Cho phép file tới 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        [ProducesResponseType(typeof(SeminarAudioSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SummarizeAudio(int id, [FromForm] SeminarAudioSummaryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _audioSummaryService.SummarizeSeminarAudioAsync(id, request, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}