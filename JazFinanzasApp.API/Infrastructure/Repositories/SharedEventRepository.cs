using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class SharedEventRepository : GenericRepository<SharedEvent>, ISharedEventRepository
    {
        private readonly ApplicationDbContext _context;

        public SharedEventRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SharedEvent>> GetByUserIdAsync(int userId, bool includeClosed)
        {
            var query = _context.SharedEvents
                .Include(e => e.Participants)
                .Include(e => e.Movements)
                .Where(e => e.UserId == userId);

            if (!includeClosed)
                query = query.Where(e => !e.IsClosed);

            return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }

        public async Task<SharedEvent?> GetDetailByIdAsync(int id)
        {
            return await _context.SharedEvents
                .Include(e => e.Participants)
                    .ThenInclude(p => p.Person)
                .Include(e => e.Movements)
                    .ThenInclude(m => m.Shares)
                        .ThenInclude(s => s.Person)
                .Include(e => e.Movements)
                    .ThenInclude(m => m.TransactionClass)
                .Include(e => e.Movements)
                    .ThenInclude(m => m.Asset)
                .Include(e => e.Movements)
                    .ThenInclude(m => m.PayerPerson)
                .Include(e => e.Payments)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<SharedEventParticipant?> GetParticipantAsync(int sharedEventId, int personId)
        {
            return await _context.SharedEventParticipants
                .FirstOrDefaultAsync(p => p.SharedEventId == sharedEventId && p.PersonId == personId);
        }

        public async Task AddParticipantAsync(SharedEventParticipant participant)
        {
            await _context.SharedEventParticipants.AddAsync(participant);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveParticipantAsync(SharedEventParticipant participant)
        {
            _context.SharedEventParticipants.Remove(participant);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasMovementsOrPaymentsAsync(int sharedEventId)
        {
            if (await _context.SharedEventMovements.AnyAsync(m => m.SharedEventId == sharedEventId))
                return true;

            return await _context.SharedEventPayments.AnyAsync(p => p.SharedEventId == sharedEventId);
        }

        public async Task<bool> ParticipantHasActivityAsync(int sharedEventId, int personId)
        {
            if (await _context.SharedEventMovements
                .AnyAsync(m => m.SharedEventId == sharedEventId && m.PayerPersonId == personId))
                return true;

            if (await _context.SharedEventMovementShares
                .AnyAsync(s => s.SharedEventMovement.SharedEventId == sharedEventId && s.PersonId == personId))
                return true;

            return await _context.SharedEventPayments
                .AnyAsync(p => p.SharedEventId == sharedEventId && (p.FromPersonId == personId || p.ToPersonId == personId));
        }

        public async Task DeleteEventWithParticipantsAsync(int sharedEventId)
        {
            var participants = await _context.SharedEventParticipants
                .Where(p => p.SharedEventId == sharedEventId)
                .ToListAsync();
            _context.SharedEventParticipants.RemoveRange(participants);

            var sharedEvent = await _context.SharedEvents.FindAsync(sharedEventId);
            if (sharedEvent != null)
                _context.SharedEvents.Remove(sharedEvent);

            await _context.SaveChangesAsync();
        }
    }
}
