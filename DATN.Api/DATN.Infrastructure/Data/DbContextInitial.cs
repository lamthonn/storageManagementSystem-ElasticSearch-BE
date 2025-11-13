using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Data
{
    public class DbContextInitial
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public DbContextInitial(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                // Run migration
                await _context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ex.StackTrace);
            }
        }
    }
}
