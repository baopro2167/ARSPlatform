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
using System.Globalization;
using System.Linq;
using System.Text;
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
            var normalizedWorkId =
                NormalizeCanonicalWorkIdOrNull(
                    request.OpenAlexWorkId);

            var paper =
                _mapper.Map<Paper>(request);

            paper.CreatorId =
                authorId;

            paper.OpenAlexWorkId =
                normalizedWorkId;

            paper.Status =
                "Submitted";

            paper.CreatedAt =
                DateTime.UtcNow;

            paper.UpdatedAt =
                DateTime.UtcNow;

            if (normalizedWorkId != null)
            {
                paper.AuthorshipVerificationStatus =
                    VerificationPendingAdminReview;

                paper.AuthorshipVerifiedAt =
                    null;

                paper.AuthorshipVerificationReason =
                    "AWAITING_ADMIN_VERIFICATION";
            }
            else
            {
                paper.AuthorshipVerificationStatus =
                    VerificationNotChecked;

                paper.AuthorshipVerifiedAt =
                    null;

                paper.AuthorshipVerificationReason =
                    null;
            }

            /*
                Keep Authors optional for backward compatibility
                with the existing Paper create flow.

                New OpenAlex/manual forms may submit Authors,
                but older clients are not broken if they omit it.
            */
            if (request.Authors != null &&
                request.Authors.Count > 0)
            {
                ReplaceAuthors(
                    paper,
                    request.Authors,
                    normalizedWorkId != null
                        ? "OPENALEX"
                        : "MANUAL");
            }

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

            var normalizedWorkId =
                request.OpenAlexWorkId == null
                    ? paper.OpenAlexWorkId
                    : NormalizeCanonicalWorkIdOrNull(
                        request.OpenAlexWorkId);

            var updatedDoi =
                request.Doi == null
                    ? paper.Doi
                    : NormalizeOptionalText(request.Doi);

            var updatedPublicationDate =
                request.PublicationDate
                ?? paper.PublicationDate;

            var updatedSourceName =
                request.SourceName == null
                    ? paper.SourceName
                    : NormalizeOptionalText(request.SourceName);

            var updatedIssnValue =
                request.IssnValue == null
                    ? paper.IssnValue
                    : NormalizeOptionalText(request.IssnValue);

            var authorsChanged =
                request.Authors != null &&
                AuthorsChanged(
                    paper.PaperAuthors,
                    request.Authors);

            /*
                Verification is tied to the Paper metadata
                and author list that were verified.
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
                    request.SubFieldId ||

                !string.Equals(
                    paper.OpenAlexWorkId,
                    normalizedWorkId,
                    StringComparison.Ordinal) ||

                !string.Equals(
                    paper.Doi,
                    updatedDoi,
                    StringComparison.Ordinal) ||

                paper.PublicationDate !=
                    updatedPublicationDate ||

                !string.Equals(
                    paper.SourceName,
                    updatedSourceName,
                    StringComparison.Ordinal) ||

                !string.Equals(
                    paper.IssnValue,
                    updatedIssnValue,
                    StringComparison.Ordinal) ||

                authorsChanged;

            /*
                Preserve the existing safety behavior:
                if an already verified/approved OpenAlex Paper
                is edited, it must return to Submitted.
            */
            var wasVerifiedAndApproved =
                string.Equals(
                    paper.AuthorshipVerificationStatus,
                    VerificationVerified,
                    StringComparison.OrdinalIgnoreCase) &&
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

            paper.OpenAlexWorkId =
                normalizedWorkId;

            paper.Doi =
                updatedDoi;

            paper.PublicationDate =
                updatedPublicationDate;

            paper.SourceName =
                updatedSourceName;

            paper.IssnValue =
                updatedIssnValue;

            if (request.Authors != null)
            {
                ReplaceAuthors(
                    paper,
                    request.Authors,
                    normalizedWorkId != null
                        ? "OPENALEX"
                        : "MANUAL");
            }

            if (verificationRelevantChanged)
            {
                paper.AuthorshipVerifiedAt =
                    null;

                if (normalizedWorkId != null)
                {
                    paper.AuthorshipVerificationStatus =
                        VerificationPendingAdminReview;

                    paper.AuthorshipVerificationReason =
                        "PAPER_UPDATED_REQUIRES_REVIEW";
                }
                else
                {
                    paper.AuthorshipVerificationStatus =
                        VerificationNotChecked;

                    paper.AuthorshipVerificationReason =
                        null;
                }

                if (wasVerifiedAndApproved)
                {
                    paper.Status =
                        "Submitted";
                }
            }

            /*
                Preserve Admin status management, but an
                OpenAlex-linked Paper cannot be Approved until
                ORCID ID and verified ORCID name both match.
            */
            if (allowStatusUpdate &&
                !string.IsNullOrWhiteSpace(
                    request.Status))
            {
                var requestedStatus =
                    request.Status.Trim();

                if (string.Equals(
                        requestedStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(
                        paper.OpenAlexWorkId) &&
                    !string.Equals(
                        paper.AuthorshipVerificationStatus,
                        VerificationVerified,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "An OpenAlex-linked paper can only be approved after ORCID and author-name verification succeeds.");
                }

                paper.Status =
                    requestedStatus;
            }

            paper.UpdatedAt =
                DateTime.UtcNow;

            _paperRepository
                .Update(paper);

            await _paperRepository
                .SaveChangesAsync();

            var updatedPaper =
                await _paperRepository
                    .GetWithAuthorByIdAsync(id);

            return updatedPaper == null
                ? null
                : _mapper.Map<PaperResponse>(updatedPaper);
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

            string? requestedWorkId;

            if (!string.IsNullOrWhiteSpace(
                    request.OpenAlexWorkId))
            {
                requestedWorkId =
                    NormalizeCanonicalWorkIdOrNull(
                        request.OpenAlexWorkId);
            }
            else
            {
                requestedWorkId =
                    NormalizeCanonicalWorkIdOrNull(
                        paper.OpenAlexWorkId);
            }

            if (string.IsNullOrWhiteSpace(
                    requestedWorkId))
            {
                throw new InvalidOperationException(
                    "This paper does not have an OpenAlex Work ID. Manual papers must be reviewed manually by Admin.");
            }

            var lookup =
                await _openAlexService
                    .LookupWorkByIdAsync(
                        requestedWorkId);

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

            /*
                Provider/input failures never auto reject
                or approve the Paper. They remain for Admin review.
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
                    verifiedOrcidId: paper.Creator?.OrcidId,
                    orcidDisplayName: paper.Creator?.OrcidDisplayName,
                    isOrcidMatch: false,
                    isNameMatch: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            /*
                Refresh PaperAuthors from the authoritative
                OpenAlex authorship payload during Admin check.
            */
            SyncAuthorsFromOpenAlex(
                paper,
                lookup.Authorships);

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
                    orcidDisplayName: null,
                    isOrcidMatch: false,
                    isNameMatch: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

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
                    verifiedOrcidId: paper.Creator.OrcidId,
                    orcidDisplayName: paper.Creator.OrcidDisplayName,
                    isOrcidMatch: false,
                    isNameMatch: null,
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
                    verifiedOrcidId: paper.Creator.OrcidId,
                    orcidDisplayName: paper.Creator.OrcidDisplayName,
                    isOrcidMatch: false,
                    isNameMatch: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            var rawMatch =
                lookup.Authorships
                    .FirstOrDefault(
                        authorship =>
                            OrcidMatches(
                                authorship.RawOrcid,
                                normalizedCreatorOrcid));

            var resolvedMatch =
                rawMatch == null
                    ? lookup.Authorships
                        .FirstOrDefault(
                            authorship =>
                                OrcidMatches(
                                    authorship.AuthorOrcid,
                                    normalizedCreatorOrcid))
                    : null;

            var matchedAuthorship =
                rawMatch ?? resolvedMatch;

            if (matchedAuthorship == null)
            {
                SetPendingVerification(
                    paper,
                    lookup.Authorships.Count == 0
                        ? "AUTHORSHIP_DATA_UNAVAILABLE"
                        : "ORCID_NOT_IN_AUTHORSHIP");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    normalizedCreatorOrcid,
                    paper.Creator.OrcidDisplayName,
                    isOrcidMatch: false,
                    isNameMatch: null,
                    matchSource: null,
                    matchedAuthorName: null);
            }

            var matchedAuthorName =
                matchedAuthorship.AuthorDisplayName
                ?? matchedAuthorship.RawAuthorName;

            var matchSource =
                rawMatch != null
                    ? "RAW_ORCID"
                    : "AUTHOR_ORCID";

            if (string.IsNullOrWhiteSpace(
                    paper.Creator.OrcidDisplayName))
            {
                SetPendingVerification(
                    paper,
                    "ORCID_DISPLAY_NAME_UNAVAILABLE");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    normalizedCreatorOrcid,
                    paper.Creator.OrcidDisplayName,
                    isOrcidMatch: true,
                    isNameMatch: null,
                    matchSource,
                    matchedAuthorName);
            }

            var isNameMatch =
                NamesMatch(
                    paper.Creator.OrcidDisplayName,
                    matchedAuthorName);

            if (!isNameMatch)
            {
                SetPendingVerification(
                    paper,
                    "ORCID_NAME_MISMATCH");

                await SaveVerificationAsync(
                    paper);

                return BuildVerificationResponse(
                    paper,
                    lookup,
                    normalizedCreatorOrcid,
                    paper.Creator.OrcidDisplayName,
                    isOrcidMatch: true,
                    isNameMatch: false,
                    matchSource,
                    matchedAuthorName);
            }

            SetVerified(
                paper);

            await SaveVerificationAsync(
                paper);

            return BuildVerificationResponse(
                paper,
                lookup,
                normalizedCreatorOrcid,
                paper.Creator.OrcidDisplayName,
                isOrcidMatch: true,
                isNameMatch: true,
                matchSource,
                matchedAuthorName);
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
                "ORCID_AND_NAME_MATCH";

            /*
                Verification never changes Paper.Status.
                Admin remains the final approver/rejector.
            */
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

        private static bool NamesMatch(
            string? orcidDisplayName,
            string? paperAuthorName)
        {
            var left = NormalizePersonName(
                orcidDisplayName);

            var right = NormalizePersonName(
                paperAuthorName);

            return left.Length > 0 &&
                   right.Length > 0 &&
                   string.Equals(
                       left,
                       right,
                       StringComparison.Ordinal);
        }

        private static string NormalizePersonName(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized =
                value.Trim()
                    .Normalize(
                        NormalizationForm.FormD);

            var builder =
                new StringBuilder();

            foreach (var character in normalized)
            {
                var category =
                    CharUnicodeInfo.GetUnicodeCategory(
                        character);

                if (category ==
                    UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(
                        char.ToLowerInvariant(
                            character));
                }
                else
                {
                    builder.Append(' ');
                }
            }

            return string.Join(
                " ",
                builder
                    .ToString()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));
        }

        private static string? NormalizeOptionalText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeCanonicalWorkIdOrNull(
            string? workId)
        {
            if (string.IsNullOrWhiteSpace(workId))
            {
                return null;
            }

            var value = workId.Trim();

            if (value.Length < 2 ||
                value[0] != 'W')
            {
                throw new ArgumentException(
                    "OpenAlexWorkId must be a canonical W-prefixed OpenAlex Work ID.");
            }

            for (var index = 1;
                 index < value.Length;
                 index++)
            {
                if (!char.IsDigit(value[index]))
                {
                    throw new ArgumentException(
                        "OpenAlexWorkId must be a canonical W-prefixed OpenAlex Work ID.");
                }
            }

            return value;
        }

        private static void ReplaceAuthors(
            Paper paper,
            IReadOnlyList<PaperAuthorRequest> authors,
            string source)
        {
            paper.PaperAuthors.Clear();

            var now =
                DateTime.UtcNow;

            for (var index = 0;
                 index < authors.Count;
                 index++)
            {
                var author =
                    authors[index];

                if (string.IsNullOrWhiteSpace(
                        author.AuthorName))
                {
                    throw new ArgumentException(
                        "AuthorName is required for every paper author.");
                }

                string? normalizedOrcid = null;

                if (!string.IsNullOrWhiteSpace(
                        author.OrcidId))
                {
                    if (!OrcidIdUtility
                            .TryNormalizeAndValidate(
                                author.OrcidId,
                                out normalizedOrcid))
                    {
                        throw new ArgumentException(
                            $"Author ORCID '{author.OrcidId}' is invalid.");
                    }
                }

                paper.PaperAuthors.Add(
                    new PaperAuthor
                    {
                        AuthorOrder = index + 1,
                        AuthorName = author.AuthorName.Trim(),
                        RawAuthorName =
                            string.IsNullOrWhiteSpace(author.RawAuthorName)
                                ? null
                                : author.RawAuthorName.Trim(),
                        OrcidId = normalizedOrcid,
                        OpenAlexAuthorId =
                            string.IsNullOrWhiteSpace(author.OpenAlexAuthorId)
                                ? null
                                : author.OpenAlexAuthorId.Trim(),
                        IsCorresponding = author.IsCorresponding,
                        Source = source,
                        CreatedAt = now
                    });
            }
        }

        private static bool AuthorsChanged(
            ICollection<PaperAuthor> existingAuthors,
            IReadOnlyList<PaperAuthorRequest> requestedAuthors)
        {
            var existing =
                existingAuthors
                    .OrderBy(author => author.AuthorOrder)
                    .ToList();

            if (existing.Count !=
                requestedAuthors.Count)
            {
                return true;
            }

            for (var index = 0;
                 index < existing.Count;
                 index++)
            {
                var current =
                    existing[index];

                var requested =
                    requestedAuthors[index];

                if (!string.Equals(
                        current.AuthorName?.Trim(),
                        requested.AuthorName?.Trim(),
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        current.RawAuthorName?.Trim(),
                        requested.RawAuthorName?.Trim(),
                        StringComparison.Ordinal) ||
                    !OrcidEquivalent(
                        current.OrcidId,
                        requested.OrcidId) ||
                    !string.Equals(
                        current.OpenAlexAuthorId?.Trim(),
                        requested.OpenAlexAuthorId?.Trim(),
                        StringComparison.OrdinalIgnoreCase) ||
                    current.IsCorresponding !=
                        requested.IsCorresponding)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool OrcidEquivalent(
            string? left,
            string? right)
        {
            if (string.IsNullOrWhiteSpace(left) &&
                string.IsNullOrWhiteSpace(right))
            {
                return true;
            }

            if (!OrcidIdUtility.TryNormalizeAndValidate(
                    left,
                    out var normalizedLeft) ||
                !OrcidIdUtility.TryNormalizeAndValidate(
                    right,
                    out var normalizedRight))
            {
                return string.Equals(
                    left?.Trim(),
                    right?.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(
                normalizedLeft,
                normalizedRight,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SyncAuthorsFromOpenAlex(
            Paper paper,
            IReadOnlyList<OpenAlexWorkAuthorshipResponse> authorships)
        {
            if (authorships.Count == 0)
            {
                return;
            }

            paper.PaperAuthors.Clear();

            var now =
                DateTime.UtcNow;

            for (var index = 0;
                 index < authorships.Count;
                 index++)
            {
                var authorship =
                    authorships[index];

                var authorName =
                    authorship.AuthorDisplayName
                    ?? authorship.RawAuthorName;

                if (string.IsNullOrWhiteSpace(authorName))
                {
                    continue;
                }

                var candidateOrcid =
                    !string.IsNullOrWhiteSpace(authorship.RawOrcid)
                        ? authorship.RawOrcid
                        : authorship.AuthorOrcid;

                string? normalizedOrcid = null;

                if (!string.IsNullOrWhiteSpace(candidateOrcid))
                {
                    OrcidIdUtility.TryNormalizeAndValidate(
                        candidateOrcid,
                        out normalizedOrcid);
                }

                paper.PaperAuthors.Add(
                    new PaperAuthor
                    {
                        AuthorOrder = index + 1,
                        AuthorName = authorName.Trim(),
                        RawAuthorName = authorship.RawAuthorName,
                        OrcidId = normalizedOrcid,
                        OpenAlexAuthorId = authorship.AuthorOpenAlexId,
                        IsCorresponding = authorship.IsCorresponding,
                        Source = "OPENALEX",
                        CreatedAt = now
                    });
            }
        }

        private static PaperAuthorshipVerificationResponse
            BuildVerificationResponse(
                Paper paper,
                OpenAlexWorkLookupResponse lookup,
                string? verifiedOrcidId,
                string? orcidDisplayName,
                bool isOrcidMatch,
                bool? isNameMatch,
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

                OrcidDisplayName =
                    orcidDisplayName,

                IsOrcidMatch =
                    isOrcidMatch,

                IsNameMatch =
                    isNameMatch,

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