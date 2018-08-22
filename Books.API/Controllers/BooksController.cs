using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Books.API.Controllers
{
    [Route("[controller]")]
    public class BooksController : ControllerBase
    {
        private static ConcurrentDictionary<string, Book> _books = new ConcurrentDictionary<string, Book>();

        [HttpGet]
        public ActionResult Get()
        {
            return Ok(_books.Select(x => x.Value));
        }

        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            return Ok(_books.Where(x => x.Key == id).Select(x => x.Value));
        }

        [HttpPost]
        public ActionResult Post([FromBody] Book book)
        {
            if(_books.Keys.Any(x => x == book.Id))
                return StatusCode(409);

            _books.TryAdd(book.Id, book);
            return Created($"{Request.Path.ToUriComponent()}/{book.Id}", book);
        }

        [HttpPut("{id}")]
        public ActionResult Put(string id, [FromBody] Book book)
        {
            return Ok(_books.AddOrUpdate(id, book, (k, v) => book));
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            return Ok(_books.TryRemove(id, out var book));
        }

        [HttpPatch("{id}")]
        public ActionResult Patch(string id, [FromBody]JsonPatchDocument<BookForUpdate> jsonPatchDocument)
        {
            if(jsonPatchDocument == null)
                return BadRequest();

            if(!_books.TryGetValue(id, out var bookToUpdate))
                return NotFound();

            var updatableBook = bookToUpdate.GetUpdatableSection;    

            jsonPatchDocument.ApplyTo(updatableBook);

            bookToUpdate.MapFrom(updatableBook);

            _books[id] = bookToUpdate;
            return Ok();
        }
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
