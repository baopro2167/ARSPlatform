using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ARSPlatform.SERVICES
{
    public class OrcidLinkService : IOrcidLinkService
    {
        private const string RegistrationContext =
            "REGISTRATION";

        private const string AccountLinkContext =
            "ACCOUNT_LINK";

        private const string PendingStatus =
            "PENDING";

        private const string AuthenticatedStatus =
            "AUTHENTICATED";

        private const string CompletedStatus =
            "COMPLETED";

        private const string FailedStatus =
            "FAILED";

        private static readonly TimeSpan OAuthSessionLifetime =
            TimeSpan.FromMinutes(10);

        private static readonly TimeSpan RegistrationTicketLifetime =
            TimeSpan.FromMinutes(15);

        private readonly IOrcidLinkSessionRepository
            _orcidLinkSessionRepository;

        private readonly IUserRepository
            _userRepository;

        private readonly IOrcidOAuthService
            _orcidOAuthService;

        public OrcidLinkService(
            IOrcidLinkSessionRepository orcidLinkSessionRepository,
            IUserRepository userRepository,
            IOrcidOAuthService orcidOAuthService)
        {
            _orcidLinkSessionRepository =
                orcidLinkSessionRepository;

            _userRepository =
                userRepository;

            _orcidOAuthService =
                orcidOAuthService;
        }

        public async Task<OrcidLinkStartResponse>
            StartRegistrationAsync(
                CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var rawState =
                GenerateSecureToken();

            var stateHash =
                ComputeSha256(rawState);

            /*
                Build URL before writing the session.

                If ORCID credentials are not configured yet,
                BuildAuthorizationUrl will fail and we do not
                leave an unusable PENDING session in the DB.
            */
            var authorizationUrl =
                _orcidOAuthService
                    .BuildAuthorizationUrl(rawState);

            var session =
                new OrcidLinkSession
                {
                    StateHash = stateHash,
                    Context = RegistrationContext,
                    UserId = null,
                    Status = PendingStatus,
                    CreatedAt = now,
                    ExpiresAt =
                        now.Add(OAuthSessionLifetime)
                };

            await _orcidLinkSessionRepository
                .AddAsync(session);

            await _orcidLinkSessionRepository
                .SaveChangesAsync();

            return new OrcidLinkStartResponse
            {
                AuthorizationUrl =
                    authorizationUrl,

                Context =
                    RegistrationContext,

                ExpiresAt =
                    session.ExpiresAt
            };
        }

        public async Task<OrcidLinkStartResponse>
            StartAccountLinkAsync(
                int userId,
                CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "A valid UserId is required.",
                    nameof(userId));
            }

            var user =
                await _userRepository
                    .GetByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException(
                    $"User {userId} was not found.");
            }

            /*
                ORCID is system-owned.

                If an account already has an ORCID,
                do not silently replace it with another one.
            */
            if (!string.IsNullOrWhiteSpace(user.OrcidId) ||
                user.IsOrcidVerified)
            {
                throw new InvalidOperationException(
                    "This ARS account already has an ORCID iD connected.");
            }

            var now = DateTime.UtcNow;

            var rawState =
                GenerateSecureToken();

            var stateHash =
                ComputeSha256(rawState);

            var authorizationUrl =
                _orcidOAuthService
                    .BuildAuthorizationUrl(rawState);

            var session =
                new OrcidLinkSession
                {
                    StateHash = stateHash,
                    Context = AccountLinkContext,
                    UserId = user.UserId,
                    Status = PendingStatus,
                    CreatedAt = now,
                    ExpiresAt =
                        now.Add(OAuthSessionLifetime)
                };

            await _orcidLinkSessionRepository
                .AddAsync(session);

            await _orcidLinkSessionRepository
                .SaveChangesAsync();

            return new OrcidLinkStartResponse
            {
                AuthorizationUrl =
                    authorizationUrl,

                Context =
                    AccountLinkContext,

                ExpiresAt =
                    session.ExpiresAt
            };
        }

        public async Task<OrcidLinkCallbackResponse>
            HandleCallbackAsync(
                string? code,
                string? state,
                string? error,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return Failure(
                    context: null,
                    status: FailedStatus,
                    errorCode: "INVALID_STATE",
                    errorMessage:
                        "ORCID OAuth state is missing.");
            }

            var stateHash =
                ComputeSha256(state.Trim());

            var session =
                await _orcidLinkSessionRepository
                    .GetByStateHashAsync(stateHash);

            if (session == null)
            {
                return Failure(
                    context: null,
                    status: FailedStatus,
                    errorCode: "INVALID_STATE",
                    errorMessage:
                        "ORCID OAuth state is invalid.");
            }

            /*
                A state can only be processed once.

                This prevents the same callback from being
                replayed after it was authenticated/completed.
            */
            if (!string.Equals(
                    session.Status,
                    PendingStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    context: session.Context,
                    status: session.Status,
                    errorCode:
                        "SESSION_ALREADY_PROCESSED",
                    errorMessage:
                        "This ORCID linking session has already been processed.");
            }

            var now = DateTime.UtcNow;

            if (session.ExpiresAt <= now)
            {
                return await FailSessionAsync(
                    session,
                    "SESSION_EXPIRED",
                    "The ORCID linking session has expired.");
            }

            /*
                ORCID can redirect back with an OAuth error,
                for example when the user cancels authorization.
            */
            if (!string.IsNullOrWhiteSpace(error))
            {
                var failureCode =
                    string.Equals(
                        error,
                        "access_denied",
                        StringComparison.OrdinalIgnoreCase)
                        ? "AUTHORIZATION_DENIED"
                        : "ORCID_AUTHORIZATION_ERROR";

                return await FailSessionAsync(
                    session,
                    failureCode,
                    "ORCID authorization was not completed.");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return await FailSessionAsync(
                    session,
                    "INVALID_CODE",
                    "ORCID authorization code is missing.");
            }

            var oauthResult =
                await _orcidOAuthService
                    .ExchangeCodeAsync(
                        code.Trim(),
                        cancellationToken);

            if (!oauthResult.Success)
            {
                return await FailSessionAsync(
                    session,
                    oauthResult.ErrorCode
                        ?? "ORCID_OAUTH_FAILED",
                    oauthResult.ErrorMessage
                        ?? "ORCID authentication failed.");
            }

            if (string.IsNullOrWhiteSpace(
                    oauthResult.OrcidId))
            {
                return await FailSessionAsync(
                    session,
                    "INVALID_AUTHENTICATED_ORCID",
                    "ORCID did not return a valid authenticated ORCID iD.");
            }

            /*
                ExchangeCodeAsync already validates and
                normalizes the ORCID iD.

                We validate again at the business boundary so
                User.OrcidId can never receive unvalidated data.
            */
            if (!OrcidIdUtility.TryNormalizeAndValidate(
                    oauthResult.OrcidId,
                    out var normalizedOrcidId))
            {
                return await FailSessionAsync(
                    session,
                    "INVALID_AUTHENTICATED_ORCID",
                    "ORCID returned an invalid authenticated ORCID iD.");
            }

            session.AuthenticatedOrcidId =
                normalizedOrcidId;

            session.DisplayName =
                string.IsNullOrWhiteSpace(
                    oauthResult.DisplayName)
                    ? null
                    : oauthResult.DisplayName.Trim();

            session.AuthenticatedAt =
                now;

            var existingOrcidUser =
                await _userRepository
                    .GetByOrcidAsync(
                        normalizedOrcidId);

            if (string.Equals(
                    session.Context,
                    RegistrationContext,
                    StringComparison.OrdinalIgnoreCase))
            {
                return await CompleteRegistrationOAuthAsync(
                    session,
                    existingOrcidUser,
                    normalizedOrcidId,
                    now);
            }

            if (string.Equals(
                    session.Context,
                    AccountLinkContext,
                    StringComparison.OrdinalIgnoreCase))
            {
                return await CompleteAccountLinkAsync(
                    session,
                    existingOrcidUser,
                    normalizedOrcidId,
                    now);
            }

            return await FailSessionAsync(
                session,
                "INVALID_CONTEXT",
                "The ORCID linking context is invalid.");
        }

        public async Task<OrcidStatusResponse?> GetStatusAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return null;
            }

            var user =
                await _userRepository
                    .GetByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            var hasOrcid =
                !string.IsNullOrWhiteSpace(
                    user.OrcidId);

            var isVerified =
                hasOrcid &&
                user.IsOrcidVerified;

            /*
                Must remain consistent with StartAccountLinkAsync.

                We do not allow a new OAuth link when either:
                - an ORCID already exists, or
                - the account is already marked ORCID verified.

                An inconsistent legacy state therefore cannot silently
                overwrite the academic identity.
            */
            var canConnect =
                !hasOrcid &&
                !user.IsOrcidVerified;

            return new OrcidStatusResponse
            {
                UserId =
                    user.UserId,

                IsConnected =
                    hasOrcid,

                IsVerified =
                    isVerified,

                OrcidId =
                    hasOrcid
                        ? user.OrcidId
                        : null,

                VerifiedAt =
                    isVerified
                        ? user.OrcidVerifiedAt
                        : null,

                CanConnect =
                    canConnect
            };
        }

        private async Task<OrcidLinkCallbackResponse>
            CompleteRegistrationOAuthAsync(
                OrcidLinkSession session,
                User? existingOrcidUser,
                string normalizedOrcidId,
                DateTime now)
        {
            /*
                Registration has no UserId yet.

                If this ORCID already belongs to an ARS User,
                a new account must not be created with it.
            */
            if (existingOrcidUser != null)
            {
                return await FailSessionAsync(
                    session,
                    "ORCID_ALREADY_LINKED",
                    "This ORCID iD is already connected to another ARS account.");
            }

            var rawTicket =
                GenerateSecureToken();

            session.TicketHash =
                ComputeSha256(rawTicket);

            session.Status =
                AuthenticatedStatus;

            /*
                The original OAuth state has now been used.

                ExpiresAt is repurposed as the expiry time
                of the registration one-time ticket.
            */
            session.ExpiresAt =
                now.Add(
                    RegistrationTicketLifetime);

            _orcidLinkSessionRepository
                .Update(session);

            await _orcidLinkSessionRepository
                .SaveChangesAsync();

            return new OrcidLinkCallbackResponse
            {
                Success = true,
                Context = RegistrationContext,
                Status = AuthenticatedStatus,
                OrcidId = normalizedOrcidId,
                DisplayName = session.DisplayName,
                RegistrationTicket = rawTicket
            };
        }

        private async Task<OrcidLinkCallbackResponse>
            CompleteAccountLinkAsync(
                OrcidLinkSession session,
                User? existingOrcidUser,
                string normalizedOrcidId,
                DateTime now)
        {
            if (!session.UserId.HasValue)
            {
                return await FailSessionAsync(
                    session,
                    "INVALID_ACCOUNT_LINK_SESSION",
                    "The ORCID account-link session does not contain a valid ARS user.");
            }

            var user =
                await _userRepository
                    .GetByIdAsync(
                        session.UserId.Value);

            if (user == null)
            {
                return await FailSessionAsync(
                    session,
                    "USER_NOT_FOUND",
                    "The ARS account for this ORCID linking session was not found.");
            }

            /*
                Same authenticated ORCID cannot belong
                to two different ARS accounts.
            */
            if (existingOrcidUser != null &&
                existingOrcidUser.UserId !=
                    user.UserId)
            {
                return await FailSessionAsync(
                    session,
                    "ORCID_ALREADY_LINKED",
                    "This ORCID iD is already connected to another ARS account.");
            }

            /*
                Do not silently replace an existing different
                ORCID on the same ARS account.
            */
            if (!string.IsNullOrWhiteSpace(
                    user.OrcidId) &&
                !string.Equals(
                    user.OrcidId,
                    normalizedOrcidId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return await FailSessionAsync(
                    session,
                    "ACCOUNT_ALREADY_HAS_ORCID",
                    "This ARS account already has another ORCID iD connected.");
            }

            user.OrcidId =
                normalizedOrcidId;

            user.IsOrcidVerified =
                true;

            user.OrcidVerifiedAt =
                now;

            user.UpdatedAt =
                now;

            session.Status =
                CompletedStatus;

            session.CompletedAt =
                now;

            _userRepository
                .Update(user);

            _orcidLinkSessionRepository
                .Update(session);

            /*
                Both repositories use the same scoped
                AppDbContext.

                One SaveChanges persists:
                - User ORCID
                - session completion
            */
            await _orcidLinkSessionRepository
                .SaveChangesAsync();

            return new OrcidLinkCallbackResponse
            {
                Success = true,
                Context = AccountLinkContext,
                Status = CompletedStatus,
                OrcidId = normalizedOrcidId,
                DisplayName = session.DisplayName,
                RegistrationTicket = null
            };
        }

        private async Task<OrcidLinkCallbackResponse>
            FailSessionAsync(
                OrcidLinkSession session,
                string errorCode,
                string errorMessage)
        {
            session.Status =
                FailedStatus;

            session.FailureCode =
                NormalizeFailureCode(
                    errorCode);

            _orcidLinkSessionRepository
                .Update(session);

            await _orcidLinkSessionRepository
                .SaveChangesAsync();

            return Failure(
                context: session.Context,
                status: FailedStatus,
                errorCode: session.FailureCode,
                errorMessage: errorMessage);
        }

        private static OrcidLinkCallbackResponse Failure(
            string? context,
            string? status,
            string errorCode,
            string errorMessage)
        {
            return new OrcidLinkCallbackResponse
            {
                Success = false,
                Context = context,
                Status = status,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        private static string GenerateSecureToken()
        {
            var bytes =
                RandomNumberGenerator
                    .GetBytes(32);

            return Convert
                .ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string ComputeSha256(
            string value)
        {
            var bytes =
                Encoding.UTF8.GetBytes(value);

            var hash =
                SHA256.HashData(bytes);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private static string NormalizeFailureCode(
            string? failureCode)
        {
            if (string.IsNullOrWhiteSpace(
                    failureCode))
            {
                return "ORCID_LINK_FAILED";
            }

            var normalized =
                failureCode
                    .Trim()
                    .ToUpperInvariant();

            return normalized.Length <= 100
                ? normalized
                : normalized[..100];
        }
    }
}