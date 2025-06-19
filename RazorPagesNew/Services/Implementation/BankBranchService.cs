/*using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;

namespace RazorPagesNew.Services.Implementation
{
    public class BankBranchService : IBankBranchService
    {
        private readonly MyApplicationDbContext _context;

        public BankBranchService(MyApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BankBranch>> GetAllBranchesAsync()
        {
            return await _context.BankBranches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<BankBranch> GetBranchByIdAsync(int id)
        {
            return await _context.BankBranches
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BankBranch> CreateBranchAsync(BankBranch branch)
        {
            if (branch == null)
                throw new ArgumentNullException(nameof(branch));

            branch.CreatedAt = DateTime.UtcNow;

            await _context.BankBranches.AddAsync(branch);
            await _context.SaveChangesAsync();

            return branch;
        }

        public async Task<BankBranch> UpdateBranchAsync(BankBranch branch)
        {
            if (branch == null)
                throw new ArgumentNullException(nameof(branch));

            var existingBranch = await _context.BankBranches
                .FirstOrDefaultAsync(b => b.Id == branch.Id);

            if (existingBranch == null)
                throw new KeyNotFoundException($"Отделение с ID {branch.Id} не найдено.");

            existingBranch.Name = branch.Name;
            existingBranch.Address = branch.Address;
            existingBranch.Phone = branch.Phone;
            existingBranch.Manager = branch.Manager;
            existingBranch.IsActive = branch.IsActive;
            existingBranch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingBranch;
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _context.BankBranches.FindAsync(id);
            if (branch == null)
                return false;

            // Проверяем, есть ли сотрудники в этом отделении
            bool hasEmployees = await _context.Employees.AnyAsync(e => e.BankBranchId == id);

            if (hasEmployees)
            {
                // Можно не удалять физически, а просто пометить как неактивное
                branch.IsActive = false;
                branch.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.BankBranches.Remove(branch);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetEmployeeCountInBranchAsync(int branchId)
        {
            return await _context.Employees
                .CountAsync(e => e.BankBranchId == branchId);
        }
    }
}*/