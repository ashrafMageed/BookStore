using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Books.API.Controllers.Messaging;
using Books.API.ShippingService;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Books.API.Controllers
{
    [Route("[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ShippingContext _context;
        private readonly InMemoryMessageBus _bus;

        public ShippingController(ShippingContext context, InMemoryMessageBus bus)
        {
            _context = context;
            _bus = bus;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return Ok(_context.ShippingManifests.ToList());
        }
        
    }


    public class ShippingContext : DbContext
    {
        public ShippingContext(DbContextOptions<ShippingContext> options) : base(options)
        {
        }
 
        public DbSet<ShippingManifest> ShippingManifests { get; set; }
    }

    
}
