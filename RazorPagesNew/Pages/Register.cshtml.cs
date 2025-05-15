using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.Data;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUserService _userService;

        public RegisterModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IUserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                var userLocal = new User
                {
                    Username = user.UserName,
                    IdentityUserId = user.Id,
                    RoleId = 1, // например, админ
                    PasswordHash = "не используетс€, если вход через Identity"
                };
                await _userService.CreateUserAsync(userLocal, "NoPassword");
                // ѕосле успешной регистрации автоматически авторизуем пользовател€
                await _signInManager.SignInAsync(user, isPersistent: false);


                // ѕеренаправл€ем на главную страницу или другую страницу после регистрации
                return RedirectToPage("/Index");
            }

            // ≈сли не удалось зарегистрировать пользовател€, показываем ошибки
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
