using System.Text;
using StockConsume.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StockConsume.Helper;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using StockConsume.Services;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<UserController> _logger;

        public UserController(IApiService apiService, ILogger<UserController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        #region User List With Pagination

        public async Task<IActionResult> UserList(int page = 1)
        {
            try
            {
                var paginatedUsers = await _apiService.GetPaginatedUsersAsync(page, 5);
                return View(paginatedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                TempData["Error"] = "Failed to load users. Please try again.";
                return View(new PaginatedUserResponse());
            }
        }

        #endregion

        // Delete user by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["Success"] = "User deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting user with ID {id}");
                TempData["Error"] = "An error occurred while deleting the user.";
            }
            
            return RedirectToAction("UserList");
        }

        // GET: Add/Edit User
        public async Task<IActionResult> AddEdit(int? id)
        {
            UserModel user;

            if (id == null)
            {
                user = new UserModel();
            }
            else
            {
                user = await _apiService.GetUserByIdAsync(id.Value);
                if (user == null)
                {
                    return NotFound();
                }
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(UserModel user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                bool success;
                if (user.UserId > 0)
                {
                    // Update existing user
                    success = await _apiService.UpdateUserAsync(user);
                    if (success)
                    {
                        TempData["Success"] = "User updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update user.";
                    }
                }
                else
                {
                    // Create new user
                    success = await _apiService.CreateUserAsync(user);
                    if (success)
                    {
                        TempData["Success"] = "User created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create user.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving user");
                TempData["Error"] = "An error occurred while saving the user.";
            }

            return RedirectToAction("UserList");
        }
    }
}
