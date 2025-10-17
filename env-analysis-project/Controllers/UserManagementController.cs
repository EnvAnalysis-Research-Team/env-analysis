using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using env_analysis_project.Models;

namespace env_analysis_project.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Hiển thị danh sách user
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // Form thêm user
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý thêm user
        [HttpPost]
        public async Task<IActionResult> Create(ApplicationUser model, string password)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.Email; // bắt buộc cho Identity
                var result = await _userManager.CreateAsync(model, password);

                if (result.Succeeded)
                {
                    // Nếu có trường Role thì gán
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        if (!await _roleManager.RoleExistsAsync(model.Role))
                            await _roleManager.CreateAsync(new IdentityRole(model.Role));

                        await _userManager.AddToRoleAsync(model, model.Role);
                    }

                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Xóa user
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Index");
        }
    }
}
