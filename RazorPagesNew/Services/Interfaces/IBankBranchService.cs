using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IBankBranchService
    {
        Task<IEnumerable<BankBranch>> GetAllBranchesAsync();
        Task<BankBranch> GetBranchByIdAsync(int id);
        Task<BankBranch> CreateBranchAsync(BankBranch branch);
        Task<BankBranch> UpdateBranchAsync(BankBranch branch);
        Task<bool> DeleteBranchAsync(int id);
        Task<int> GetEmployeeCountInBranchAsync(int branchId);
    }
}