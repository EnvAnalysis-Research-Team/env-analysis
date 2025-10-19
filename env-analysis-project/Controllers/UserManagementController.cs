using System;
using System.Linq;
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
            return View("Manage", users); 
        }

        // Form thêm user
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý thêm user
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return Json(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role,
                createdAt = user.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ApplicationUser model)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
            {
                if (IsAjaxRequest()) return Json(new { success = false, error = "Invalid request." });
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                if (IsAjaxRequest()) return Json(new { success = false, error = "User not found." });
                return NotFound();
            }

            // Cập nhật các trường có thể chỉnh sửa
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email; // Identity cần đồng bộ Email và Username
            user.Role = model.Role;

            // Nếu có cập nhật vai trò
            if (!string.IsNullOrEmpty(model.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!await _roleManager.RoleExistsAsync(model.Role))
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));

                await _userManager.AddToRoleAsync(user, model.Role);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (IsAjaxRequest()) return Json(new { success = true });
                return RedirectToAction(nameof(Index));
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            if (IsAjaxRequest()) return Json(new { success = false, error = errors });

            TempData["Error"] = errors;
            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
