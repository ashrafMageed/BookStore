using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Books.API.Controllers
{
    [Route("[controller]")]
    public class BooksController : ControllerBase
    {
        private static ConcurrentDictionary<string, Book> _books = new ConcurrentDictionary<string, Book>();
        private readonly ApiContext _context;
 
        public BooksController(ApiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return Ok(_context.Books.ToList());
        }

        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            var book = _context.Books.FirstOrDefault(x => x.Id == id);
            
            if(book == null)
                return NotFound();

            return Ok(book);
        }

        [HttpPost]
        public ActionResult Post([FromBody] Book book)
        {
            if(_context.Books.Any(x => x.Id == book.Id))
                return StatusCode(409);

            _context.Books.Add(book);
            _context.SaveChanges();

            return Created($"{Request.Path.ToUriComponent()}/{book.Id}", book);
        }

        [HttpPut("{id}")]
        public ActionResult Put(string id, [FromBody] Book book)
        {
            _context.Books.Add(book);
            _context.SaveChanges();

            return Ok();
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var book = _context.Books.FirstOrDefault(x => x.Id == id);
            
            if(book != null)
            {
                _context.Books.Remove(book);
                _context.SaveChanges();
            }

            return Ok();
        }

        [HttpPatch("{id}")]
        public ActionResult Patch(string id, [FromBody]JsonPatchDocument<BookForUpdate> jsonPatchDocument)
        {
            if(jsonPatchDocument == null)
                return BadRequest();

            var bookToUpdate = _context.Books.FirstOrDefault(x => x.Id == id);
            
            if(bookToUpdate == null)
                return NotFound();

            var updatableBook = bookToUpdate.GetUpdatableSection;    

            jsonPatchDocument.ApplyTo(updatableBook);

            bookToUpdate.MapFrom(updatableBook);

            _context.SaveChanges();
            return Ok();
        }
    }
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }
 
        public DbSet<Book> Books { get; set; }
 
        public DbSet<Author> Authors { get; set; }
    }

    public class BookForUpdate
    {
        public string Title { get; set; }
        public decimal Price { get; set; }
    }

    public class Book
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Publisher { get; set; }

        public DateTimeOffset Year { get; set; }

        public decimal Price { get; set; }

        public List<Author> Authors { get; set; }

        public BookForUpdate GetUpdatableSection => new BookForUpdate{ Price = this.Price, Title = this.Title};
        public void MapFrom(BookForUpdate bookForUpdate) 
        {
            Price = bookForUpdate.Price;
            Title = bookForUpdate.Title;
        }
        //testing Azure deployment
    }

    public class Author
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string PlaceOfBirth { get; set; }

        public DateTime DateOfDeath { get; set; }

        public string PlaceOfDeath { get; set; }
    }
}
