using System.ComponentModel.DataAnnotations;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string StreetAddress { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string? Role { get; set; }

        public IEnumerable<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();

        public int? CompanyId { get; set; }

        public IEnumerable<SelectListItem> CompanyList { get; set; } = new List<SelectListItem>();
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await SeedRolesAsync();

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        PopulateRoleList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await SeedRolesAsync();
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        PopulateRoleList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            Name = Input.Name,
            PhoneNumber = Input.PhoneNumber,
            StreetAddress = Input.StreetAddress,
            City = Input.City,
            State = Input.State,
            PostalCode = Input.PostalCode,
            CompanyId = Input.Role == SD.Role_Company ? Input.CompanyId : null
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            var role = string.IsNullOrEmpty(Input.Role) ? SD.Role_Customer : Input.Role;
            await _userManager.AddToRoleAsync(user, role);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailSender.SendEmailAsync(
                Input.Email,
                "Confirm your email",
                $"Please confirm your account with token: {code}");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private async Task SeedRolesAsync()
    {
        if (await _roleManager.RoleExistsAsync(SD.Role_Customer))
        {
            return;
        }

        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
        await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
    }

    private void PopulateRoleList()
    {
        Input.RoleList = _roleManager.Roles
            .Select(role => role.Name)
            .Where(roleName => roleName != null)
            .Select(roleName => new SelectListItem
            {
                Text = roleName,
                Value = roleName
            });

        Input.CompanyList = _unitOfWork.Company.GetAll()
            .Select(company => new SelectListItem
            {
                Text = company.Name,
                Value = company.Id.ToString()
            });
    }
}
