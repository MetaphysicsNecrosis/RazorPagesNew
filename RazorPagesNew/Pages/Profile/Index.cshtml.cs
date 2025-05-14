using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesNew.Pages.Profile
{
    [Authorize(Policy = "RequireUserRole")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Пользователь {UserName} просматривает свой профиль", User.Identity?.Name);
        }
    }
}