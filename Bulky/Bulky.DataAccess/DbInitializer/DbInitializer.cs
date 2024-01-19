using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db, ILogger<DbInitializer> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _logger = logger;
    }

    public void Initialize()
    {

        //push migration if not applied
        try
        {
            if(_db.Database.GetPendingMigrations().Count() > 0)
            {
                _db.Database.Migrate();
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        //create roles if not created
        if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

            //create admin user as well if roles are not created
            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin@mail.com",
                Email = "admin@mail.com",
                Name = "Admin Guy",
                PhoneNumber = "1234567890",
                StreetAddress = "Admin St",
                State = "AD",
                PostalCode = "12345",
                City = "Admin City"
            }, "1q2w3E*").GetAwaiter().GetResult();

            ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@mail.com");
            _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
        };

        return;
        
    }
}
