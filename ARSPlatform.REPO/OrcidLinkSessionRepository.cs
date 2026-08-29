using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class OrcidLinkSessionRepository
        : GenericRepository<OrcidLinkSession>,
          IOrcidLinkSessionRepository
    {
        public OrcidLinkSessionRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<OrcidLinkSession?> GetByStateHashAsync(
            string stateHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x =>
                    x.StateHash == stateHash);
        }

        public async Task<OrcidLinkSession?> GetByTicketHashAsync(
            string ticketHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x =>
                    x.TicketHash == ticketHash);
        }
    }
}