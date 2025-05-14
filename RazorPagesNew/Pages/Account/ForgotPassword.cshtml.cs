using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email обязателен")]
            [EmailAddress(ErrorMessage = "Некорректный формат Email")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Для безопасности не раскрываем, существует ли пользователь
                    _logger.LogWarning("Запрос на сброс пароля для несуществующего или неподтвержденного email: {Email}", Input.Email);
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // В реальном приложении здесь должна быть отправка электронного письма для сброса пароля
                // Например:
                // var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                // var callbackUrl = Url.Page(
                //     "/Account/ResetPassword",
                //     pageHandler: null,
                //     values: new { code },
                //     protocol: Request.Scheme);
                // await _emailSender.SendEmailAsync(Input.Email, "Сброс пароля",
                //     $"Для сброса пароля перейдите по <a href='{callbackUrl}'>ссылке</a>.");

                _logger.LogInformation("Сгенерирована ссылка для сброса пароля для пользователя с Email {Email}", Input.Email);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
