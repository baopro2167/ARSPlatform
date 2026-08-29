using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaperController : ControllerBase
    {
        private readonly IPaperService _paperService;

        public PaperController(
            IPaperService paperService)
        {
            _paperService =
                paperService;
        }

        /// <summary>
        /// Lấy danh sách bài báo nghiên cứu phân trang
        /// </summary>
        /// <param name="paginationParams">
        /// Tham số phân trang (PageNumber, PageSize)
        /// </param>
        /// <returns>Danh sách bài báo</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResult<PaperResponse>>> GetPapers(
            [FromQuery] PaginationParams paginationParams)
        {
            var result =
                await _paperService
                    .GetPapersAsync(
                        paginationParams);

            return Ok(result);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER ,
        /// TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST
        /// LÊN DANH SÁCH CÓ PHÂN TRANG
        /// </summary>
        /// <param name="paginationParams">
        /// Tham số phân trang (PageNumber, PageSize)
        /// </param>
        /// <returns>
        /// Danh sách bài báo có phân trang
        /// </returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PaperResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams)
        {
            var result =
                await _paperService
                    .GetPapersAsync(
                        paginationParams);

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết bài báo nghiên cứu theo ID
        /// </summary>
        /// <param name="id">ID bài báo</param>
        /// <returns>Chi tiết bài báo</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PaperResponse>> GetPaperById(
            int id)
        {
            var paper =
                await _paperService
                    .GetPaperByIdAsync(id);

            if (paper == null)
                return NotFound();

            return Ok(paper);
        }

        /// <summary>
        /// Tải lên / Tạo mới một bài báo nghiên cứu khoa học
        /// </summary>
        /// <param name="request">
        /// Thông tin bài báo
        /// </param>
        /// <returns>Bài báo vừa tạo</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PaperResponse>> CreatePaper(
            [FromBody] PaperCreateRequest request)
        {
            var currentUserIdStr =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)
                    ?.Value;

            if (string.IsNullOrEmpty(
                    currentUserIdStr))
            {
                return Unauthorized();
            }

            try
            {
                var authorId =
                    int.Parse(
                        currentUserIdStr);

                var createdPaper =
                    await _paperService
                        .CreatePaperAsync(
                            request,
                            authorId);

                return CreatedAtAction(
                    nameof(GetPaperById),
                    new
                    {
                        id = createdPaper.Id
                    },
                    createdPaper);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        Message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Xác minh tác giả Paper bằng OpenAlex Work ID
        /// và ORCID đã được xác minh của Creator.
        /// </summary>
        /// <param name="id">Paper ID</param>
        /// <param name="request">
        /// OpenAlex Work ID dùng để kiểm tra authorship
        /// </param>
        /// <returns>
        /// Kết quả xác minh authorship
        /// </returns>
        [HttpPost("{id:int}/verify-authorship")]
        [Authorize]
        public async Task<ActionResult<PaperAuthorshipVerificationResponse>>
            VerifyAuthorship(
                int id,
                [FromBody] PaperAuthorshipVerifyRequest request)
        {
            /*
                Read Paper first to enforce ownership
                before triggering OpenAlex verification.
            */
            var paper =
                await _paperService
                    .GetPaperByIdAsync(id);

            if (paper == null)
            {
                return NotFound(
                    new
                    {
                        Message =
                            "Paper not found."
                    });
            }

            var currentUserIdStr =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)
                    ?.Value;

            if (string.IsNullOrWhiteSpace(
                    currentUserIdStr))
            {
                return Unauthorized();
            }

            var currentUserRole =
                User.FindFirst(
                    ClaimTypes.Role)
                    ?.Value;

            /*
                Preserve existing Paper ownership model:

                - Admin may operate on any Paper.
                - Normal user may operate only on own Paper.

                Actual verification always uses the
                Paper Creator's ORCID, not caller ORCID.
            */
            if (currentUserRole != "Admin" &&
                paper.AuthorId?.ToString() !=
                    currentUserIdStr)
            {
                return Forbid();
            }

            try
            {
                var result =
                    await _paperService
                        .VerifyAuthorshipAsync(
                            id,
                            request);

                if (result == null)
                {
                    return NotFound(
                        new
                        {
                            Message =
                                "Paper not found."
                        });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new
                    {
                        Message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        Message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Cập nhật thông tin bài báo nghiên cứu
        /// </summary>
        /// <param name="id">
        /// ID bài báo cần cập nhật
        /// </param>
        /// <param name="request">
        /// Dữ liệu cập nhật
        /// </param>
        /// <returns>
        /// Bài báo sau khi cập nhật
        /// </returns>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<PaperResponse>> UpdatePaper(
            int id,
            [FromBody] PaperUpdateRequest request)
        {
            var paper =
                await _paperService
                    .GetPaperByIdAsync(id);

            if (paper == null)
                return NotFound();

            var currentUserIdStr =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)
                    ?.Value;

            var currentUserRole =
                User.FindFirst(
                    ClaimTypes.Role)
                    ?.Value;

            if (currentUserRole != "Admin" &&
                paper.AuthorId?.ToString() !=
                    currentUserIdStr)
            {
                return Forbid();
            }

            try
            {
                /*
                    Existing Admin status-management behavior
                    is preserved.

                    Normal owner cannot send Status=Approved
                    and bypass OpenAlex verification.
                */
                var updatedPaper =
                    await _paperService
                        .UpdatePaperAsync(
                            id,
                            request,
                            allowStatusUpdate:
                                currentUserRole == "Admin");

                return Ok(updatedPaper);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        Message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Xóa bài báo nghiên cứu
        /// </summary>
        /// <param name="id">
        /// ID bài báo cần xóa
        /// </param>
        /// <returns>Thông báo kết quả</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePaper(
            int id)
        {
            var paper =
                await _paperService
                    .GetPaperByIdAsync(id);

            if (paper == null)
                return NotFound();

            var currentUserIdStr =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)
                    ?.Value;

            var currentUserRole =
                User.FindFirst(
                    ClaimTypes.Role)
                    ?.Value;

            if (currentUserRole != "Admin" &&
                paper.AuthorId?.ToString() !=
                    currentUserIdStr)
            {
                return Forbid();
            }

            await _paperService
                .DeletePaperAsync(id);

            return Ok(
                new
                {
                    Message =
                        "Paper deleted successfully."
                });
        }
    }
}