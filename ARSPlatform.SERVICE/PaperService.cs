using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class PaperService : IPaperService
    {
        private const string VerificationNotChecked =
            "NOT_CHECKED";

        private const string VerificationVerified =
            "VERIFIED";

        private const string VerificationPendingAdminReview =
            "PENDING_ADMIN_REVIEW";

        private const string OpenAlexFound =
            "Found";

        private const string OpenAlexInvalidWorkId =
            "InvalidWorkId";

        private const string OpenAlexNotFound =
            "NotFound";

        private const string OpenAlexRateLimited =
            "RateLimited";

        private const string OpenAlexProviderUnavailable =
            "ProviderUnavailable";

        private const string OpenAlexProviderError =
            "ProviderError";

        private readonly IPaperRepository _paperRepository;
        private readonly IExternalApiService _externalApiService;
        private readonly IOpenAlexService _openAlexService;
        private readonly IMapper _mapper;

        public PaperService(
            IPaperRepository paperRepository,
            IExternalApiService externalApiService,
            IOpenAlexService openAlexService,
            IMapper mapper)
        {
            _paperRepository = paperRepository;
            _externalApiService = externalApiService;
            _openAlexService = openAlexService;
            _mapper = mapper;
        }

        public async Task<PagedResult<PaperResponse>> GetPapersAsync(
            PaginationParams paginationParams)
        {
            var query = _paperRepository
                .GetQueryable()
                .Include(p => p.Creator)
                .AsNoTracking();

            var totalCount =
                await query.CountAsync();

            var items =
                await query
                    .Skip(
                        (paginationParams.PageNumber - 1) *
                        paginationParams.PageSize)
                    .Take(
                        paginationParams.PageSize)
                    .ToListAsync();

            var dtos =
                _mapper.Map<List<PaperResponse>>(items);

            return new PagedResult<PaperResponse>(
                dtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PagedResult<PaperResponse>> GetByAuthorIdAsync(
            int authorId,
            int pageNumber,
            int pageSize)
        {
            var paged =
                await _paperRepository
                    .GetByAuthorIdPagedAsync(
                        authorId,
                        pageNumber,
                        pageSize);

            var dtos =
                _mapper.Map<List<PaperResponse>>(
                    paged.Items);

            return new PagedResult<PaperResponse>(
                dtos,
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
        }

        public async Task<PagedResult<PaperResponse>> GetBySubFieldIdAsync(
            int subFieldId,
            int pageNumber,
            int pageSize)
        {
            var paged =
                await _paperRepository
                    .GetBySubFieldIdPagedAsync(
                        subFieldId,
                        pageNumber,
                        pageSize);

            var dtos =
                _mapper.Map<List<PaperResponse>>(
                    paged.Items);

            return new PagedResult<PaperResponse>(
                dtos,
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
        }

        public async Task<PagedResult<PaperResponse>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            return await GetPapersAsync(
                new PaginationParams
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
        }

        public async Task<PaperResponse?> GetPaperByIdAsync(
            int id)
        {
            var paper =
                await _paperRepository
                    .GetWithAuthorByIdAsync(id);

            return paper != null
                ? _mapper.Map<PaperResponse>(paper)
                : null;
        }

        public async Task<PaperResponse> CreatePaperAsync(
            PaperCreateRequest request,
            int authorId)
        {
            var paper =
                _mapper.Map<Paper>(request);

            paper.CreatorId =
                authorId;

            paper.Status =
                "Submitted";

            paper.CreatedAt =
                DateTime.UtcNow;

            await _paperRepository
                .AddAsync(paper);

            await _paperRepository
                .SaveChangesAsync();

            var createdPaper =
                await _paperRepository
                    .GetWithAuthorByIdAsync(
                        paper.PaperId);

            return _mapper.Map<PaperResponse>(
                createdPaper);
        }

        public async Task<PaperResponse?> UpdatePaperAsync(
            int id,
            PaperUpdateRequest request,
            bool allowStatusUpdate = false)
        {
            var paper =
                await _paperRepository
                    .GetWithAuthorByIdAsync(id);

            if (paper == null)
                return null;

            /*
                Verification is tied to the Paper metadata
                that was verified.

                If meaningful Paper data changes afterward,
                the old OpenAlex verification becomes stale.
            */
            var verificationRelevantChanged =
                !string.Equals(
                    paper.Title,
                    request.Title,
                    StringComparison.Ordinal) ||

                !string.Equals(
                    paper.Abstract,
                    request.Abstract,
                    StringComparison.Ordinal) ||

                !string.Equals(
                    paper.FileUrl,
                    request.FileUrl,
                    StringComparison.Ordinal) ||

                paper.Issn != request.Issn ||

                paper.IsOpenAccess !=
                    request.IsOpenAccess ||

                !string.Equals(
                    paper.Quartile,
                    request.Quartile,
                    StringComparison.Ordinal) ||

                paper.SubFieldId !=
                    request.SubFieldId;

            /*
                Remember if the current Approved state was
                produced by successful OpenAlex verification.

                Only this automatic approval is reverted
                when the Paper is edited.
            */
            var wasAutoVerified =
                string.Equals(
                    paper.AuthorshipVerificationStatus,
                    VerificationVerified,
                    StringComparison.OrdinalIgnoreCase);

            var wasAutoApproved =
                wasAutoVerified &&
                string.Equals(
                    paper.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase);

            paper.Title =
                request.Title;

            paper.Abstract =
                request.Abstract;

            paper.FileUrl =
                request.FileUrl;

            paper.Issn =
                request.Issn;

            paper.IsOpenAccess =
                request.IsOpenAccess;

            paper.Quartile =
                request.Quartile;

            paper.SubFieldId =
                request.SubFieldId;

            /*
                Clear stale OpenAlex verification if the
                Paper metadata changes.

                This applies to VERIFIED and
                PENDING_ADMIN_REVIEW evidence.
            */
            if (verificationRelevantChanged &&
                !string.Equals(
                    paper.AuthorshipVerificationStatus,
                    VerificationNotChecked,
                    StringComparison.OrdinalIgnoreCase))
            {
                paper.OpenAlexWorkId =
                    null;

                paper.AuthorshipVerificationStatus =
                    VerificationNotChecked;

                paper.AuthorshipVerifiedAt =
                    null;

                paper.AuthorshipVerificationReason =
                    "PAPER_UPDATED_AFTER_VERIFICATION";

                /*
                    Revert only an automatic approval
                    generated from VERIFIED authorship.

                    Do not overwrite unrelated status values.
                */
                if (wasAutoApproved)
                {
                    paper.Status =
                        "Submitted";
                }
            }

            /*
                PaperUpdateRequest already contains Status
                in the old system.

                Preserve Admin's ability to update it,
                but prevent a normal Paper owner from
                submitting Status = Approved manually.
            */
            if (allowStatusUpdate &&
                !string.IsNullOrWhiteSpace(
                    request.Status))
            {
                paper.Status =
                    request.Status.Trim();
            }

            paper.UpdatedAt =
                DateTime.UtcNow;

            _paperRepository
                .Update(paper);

            await _paperRepository
                .SaveChangesAsync();

            return _mapper.Map<PaperResponse>(
                paper);
        }

        public async Task<bool> DeletePaperAsync(
            int id)
        {
            var paper =
                await _paperRepository
                    .GetByIdAsync(id);

            if (paper == null)
                return false;

            _paperRepository
                .Delete(paper);

            await _paperRepository
                .SaveChangesAsync();

            return true;
        }

        public async Task<PaperAuthorshipVerificationResponse?>
            VerifyAuthorshipAsync(
                int paperId,
                PaperAuthorshipVerifyRequest request)
        {
            var paper =
                await _paperRepository
                    .GetWithAuthorByIdAsync(
                        paperId);

            if (paper == null)
            {
                return null;
            }

            /*
                Do not silently replace evidence after
                successful verification.

                If Paper content changes, UpdatePaperAsync
                resets verification first.
            */
            if (string.Equals(
                    paper.AuthorshipVerificationStatus,
                    VerificationVerified,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This paper's authorship has already been verified.");
            }

            var lookup =
                await _openAlexService
                    .LookupWorkByIdAsync(
                        request.OpenAlexWorkId);

            /*
                OpenAlexService returns a canonical W...
                Work ID whenever input has been normalized.

                Invalid raw input is never persisted.
            */
            if (!string.Equals(
                    lookup.LookupStatus,
                    OpenAlexInvalidWorkId,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(
                    lookup.OpenAlexWorkId) &&
                lookup.OpenAlexWorkId.Length <= 50)
            {
                paper.OpenAlexWorkId =
                    lookup.OpenAlexWorkId;
            }
            else
            {
                paper.OpenAlexWorkId =
                    null;
            }

            /*
                Provider/input failures never reject Paper.

                They are routed to manual review.
            */
            if (!string.Equals(
                    lookup.LookupStatus,
                    OpenAlexFound,
                    StringComparison.OrdinalIgnoreCase))
            {
                SetPendingVerification(
                    paper,
                    MapOpenAlexFailureReason(
                        lookup.LookupStatus));

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    verifiedOrcidId: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            /*
                Creator navigation should normally exist
                because repository includes Creator.

                If not, do not auto reject.
            */
            if (paper.Creator == null)
            {
                SetPendingVerification(
                    paper,
                    "CREATOR_NOT_FOUND");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    verifiedOrcidId: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            /*
                ORCID is optional.

                A Paper without verified ORCID remains
                eligible for manual Admin review.
            */
            if (!paper.Creator.IsOrcidVerified ||
                string.IsNullOrWhiteSpace(
                    paper.Creator.OrcidId))
            {
                SetPendingVerification(
                    paper,
                    "NO_VERIFIED_ORCID");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    verifiedOrcidId:
                        paper.Creator.OrcidId,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            if (!OrcidIdUtility
                    .TryNormalizeAndValidate(
                        paper.Creator.OrcidId,
                        out var normalizedCreatorOrcid))
            {
                SetPendingVerification(
                    paper,
                    "INVALID_VERIFIED_ORCID");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    verifiedOrcidId:
                        paper.Creator.OrcidId,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            /*
                PRIORITY 1:
                authorship.raw_orcid

                This is ORCID attached directly to the
                Work authorship metadata.
            */
            var rawMatch =
                lookup.Authorships
                    .FirstOrDefault(
                        authorship =>
                            OrcidMatches(
                                authorship.RawOrcid,
                                normalizedCreatorOrcid));

            if (rawMatch != null)
            {
                SetVerified(
                    paper);

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    normalizedCreatorOrcid,
                    "RAW_ORCID",
                    rawMatch.RawAuthorName
                        ?? rawMatch.AuthorDisplayName);
            }

            /*
                PRIORITY 2:
                resolved author.orcid

                Only used if raw_orcid did not match.
            */
            var resolvedMatch =
                lookup.Authorships
                    .FirstOrDefault(
                        authorship =>
                            OrcidMatches(
                                authorship.AuthorOrcid,
                                normalizedCreatorOrcid));

            if (resolvedMatch != null)
            {
                SetVerified(
                    paper);

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    normalizedCreatorOrcid,
                    "AUTHOR_ORCID",
                    resolvedMatch.AuthorDisplayName
                        ?? resolvedMatch.RawAuthorName);
            }

            /*
                Distinguish missing authorship metadata
                from an explicit ORCID non-match.
            */
            if (lookup.Authorships.Count == 0)
            {
                SetPendingVerification(
                    paper,
                    "AUTHORSHIP_DATA_UNAVAILABLE");
            }
            else
            {
                SetPendingVerification(
                    paper,
                    "ORCID_NOT_IN_AUTHORSHIP");
            }

            await SaveVerificationAsync(
                paper);

            return BuildVerificationResponse(
                paper,
                lookup,
                normalizedCreatorOrcid,
                matchSource: null,
                matchedAuthorName: null);
        }

        private async Task SaveVerificationAsync(
            Paper paper)
        {
            paper.UpdatedAt =
                DateTime.UtcNow;

            _paperRepository
                .Update(paper);

            await _paperRepository
                .SaveChangesAsync();
        }

        private static void SetVerified(
            Paper paper)
        {
            paper.AuthorshipVerificationStatus =
                VerificationVerified;

            paper.AuthorshipVerifiedAt =
                DateTime.UtcNow;

            paper.AuthorshipVerificationReason =
                null;

            /*
                Auto approve only a normally Submitted Paper.

                Do not overwrite unrelated workflow states.
            */
            if (string.Equals(
                    paper.Status,
                    "Submitted",
                    StringComparison.OrdinalIgnoreCase))
            {
                paper.Status =
                    "Approved";
            }
        }

        private static void SetPendingVerification(
            Paper paper,
            string reason)
        {
            paper.AuthorshipVerificationStatus =
                VerificationPendingAdminReview;

            paper.AuthorshipVerifiedAt =
                null;

            paper.AuthorshipVerificationReason =
                reason;

            /*
                Deliberately DO NOT change Paper.Status here.

                No ORCID / mismatch / provider error
                must never automatically reject Paper.
            */
        }

        private static bool OrcidMatches(
            string? candidateOrcid,
            string normalizedCreatorOrcid)
        {
            if (!OrcidIdUtility
                    .TryNormalizeAndValidate(
                        candidateOrcid,
                        out var normalizedCandidate))
            {
                return false;
            }

            return string.Equals(
                normalizedCandidate,
                normalizedCreatorOrcid,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string MapOpenAlexFailureReason(
            string lookupStatus)
        {
            if (string.Equals(
                    lookupStatus,
                    OpenAlexInvalidWorkId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "INVALID_WORK_ID";
            }

            if (string.Equals(
                    lookupStatus,
                    OpenAlexNotFound,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "WORK_NOT_FOUND";
            }

            if (string.Equals(
                    lookupStatus,
                    OpenAlexRateLimited,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "OPENALEX_RATE_LIMITED";
            }

            if (string.Equals(
                    lookupStatus,
                    OpenAlexProviderUnavailable,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "OPENALEX_UNAVAILABLE";
            }

            if (string.Equals(
                    lookupStatus,
                    OpenAlexProviderError,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "OPENALEX_PROVIDER_ERROR";
            }

            return "OPENALEX_LOOKUP_FAILED";
        }

        private static PaperAuthorshipVerificationResponse
            BuildVerificationResponse(
                Paper paper,
                OpenAlexWorkLookupResponse lookup,
                string? verifiedOrcidId,
                string? matchSource,
                string? matchedAuthorName)
        {
            return new PaperAuthorshipVerificationResponse
            {
                PaperId =
                    paper.PaperId,

                PaperStatus =
                    paper.Status,

                OpenAlexWorkId =
                    paper.OpenAlexWorkId,

                AuthorshipVerificationStatus =
                    paper.AuthorshipVerificationStatus,

                AuthorshipVerifiedAt =
                    paper.AuthorshipVerifiedAt,

                AuthorshipVerificationReason =
                    paper.AuthorshipVerificationReason,

                VerifiedOrcidId =
                    verifiedOrcidId,

                MatchSource =
                    matchSource,

                MatchedAuthorName =
                    matchedAuthorName,

                OpenAlexLookupStatus =
                    lookup.LookupStatus,

                OpenAlexMessage =
                    lookup.Message,

                Work =
                    lookup.Work,

                Authorships =
                    lookup.Authorships
            };
        }
    }
}