using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IOrcidLinkSessionRepository
        : IGenericRepository<OrcidLinkSession>
    {
        Task<OrcidLinkSession?> GetByStateHashAsync(string stateHash);

        Task<OrcidLinkSession?> GetByTicketHashAsync(string ticketHash);
    }
}