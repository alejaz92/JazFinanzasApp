﻿using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class MovementRepository : GenericRepository<Movement>, IMovementRepository
    {
        private readonly ApplicationDbContext _context;

        public MovementRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Movement> Movements, int TotalCount)> GetPaginatedMovements(int userId, int page, int pageSize)
        {
            var totalCount = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.MovementClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .CountAsync();

            var movements = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.MovementClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I") 
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.MovementClass)
                .OrderByDescending(m => m.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (movements, totalCount);
        }


        // get by id including related tables
        public async Task<Movement> GetMovementByIdAsync(int id)
        {
            return await _context.Movements
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.MovementClass)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        

    }
}
