using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;
using System.Security.Claims;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class UserController : ControllerBase
    {
        private readonly StockContext context;

        public UserController(StockContext context)
        {
            this.context = context;
        }

        #region User List with Pagination

        [HttpGet]
        public async Task<IActionResult> GetUser(int pageNumber = 1, int pageSize = 5)
        {
            try
            {
                var totalRecords = await context.Users.CountAsync();

                var users = await context.Users
                    .OrderBy(u => u.UserId)   // Order by Id to maintain consistent paging
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new
                {
                    TotalRecords = totalRecords,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    Users = users
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region GetUserById
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        #endregion

        #region InsertUser
        [HttpPost]
        public IActionResult InsertUser([FromBody] User user)
        {
            try
            {
                // Hash the password before saving
                if (!string.IsNullOrEmpty(user.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                }

                context.Users.Add(user);
                context.SaveChanges();
                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }
        #endregion

        #region UpdateUserById
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User user)
        {
            try
            {
                if (id != user.UserId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingUser = context.Users.Find(id);
                if (existingUser == null)
                {
                    return NotFound("User not found");
                }

                // Update fields
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.Address = user.Address;
                existingUser.Phone = user.Phone;
                existingUser.Role = user.Role;

                // Hash password if it's being updated
                if (!string.IsNullOrEmpty(user.Password) && user.Password != existingUser.Password)
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                }

                context.Users.Update(existingUser);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating user: {ex.Message}");
            }
        }
        #endregion

        #region DeleteUserById
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = context.Users.Find(id);
            if (user == null)
                return NotFound();

            context.Users.Remove(user);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region FilterUsers
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<User>>> FilterUsers(
            [FromQuery] string? userName,
            [FromQuery] string? role)
        {
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(u => u.UserName!.Contains(userName));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role!.Contains(role));

            return await query.ToListAsync();
        }
        #endregion

        #region GetTopNUsers
        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<User>>> GetTopNUsers([FromQuery] int n = 2)
        {
            var users = await context.Users.Take(n).ToListAsync();
            return Ok(users);
        }
        #endregion
    }
}