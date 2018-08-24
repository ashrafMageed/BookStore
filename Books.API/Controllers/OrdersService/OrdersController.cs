using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Books.API.Controllers.Messaging;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Books.API.Controllers
{
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderContext _context;
        private readonly InMemoryMessageBus _bus;

        public OrdersController(OrderContext context, InMemoryMessageBus bus)
        {
            _context = context;
            _bus = bus;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return Ok(_context.PurchaseOrders.ToList());
        }

        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            var book = _context.PurchaseOrders.FirstOrDefault(x => x.Id == id);
            
            if(book == null)
                return NotFound();

            return Ok(book);
        }

        [HttpPost]
        public ActionResult Post([FromBody] PurchaseOrder order)
        {
            if(_context.PurchaseOrders.Any(x => x.Id == order.Id))
                return StatusCode(409);

            _context.PurchaseOrders.Add(order);
            
            _context.SaveChanges();
            
            //this should come from the domain and the event should only contain primitive types in order to simplify versioning
            _bus.Publish(new PurchaseOrderReceived(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.BooksOrdered));

            return Accepted($"{Request.Path.ToUriComponent()}/{order.Id}", order);
        }

        // [HttpPut("{id}")]
        // public ActionResult Put(string id, [FromBody] Book book)
        // {
        //     _context.Books.Add(book);
        //     _context.SaveChanges();

        //     return Ok();
        // }

        
        // [HttpDelete("{id}")]
        // public ActionResult Delete(string id)
        // {
        //     var book = _context.Books.FirstOrDefault(x => x.Id == id);
            
        //     if(book != null)
        //     {
        //         _context.Books.Remove(book);
        //         _context.SaveChanges();
        //     }

        //     return Ok();
        // }

        
    }

    public class PurchaseOrderReceived : Event
    {
        public PurchaseOrderReceived(Guid id, Guid correlationId, string purchaseOrderReference, List<BookOrder> booksOrdered) : base(id, correlationId)
        {
            PurchaseOrderReference = purchaseOrderReference;
            BooksOrdered = booksOrdered;
        }

        public string PurchaseOrderReference { get; }
        public List<BookOrder> BooksOrdered { get; }
    }

    public class OrderContext : DbContext
    {
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {
        }
 
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    }

    public class PurchaseOrder
    {
        public string Id {get; set;}
        public List<BookOrder> BooksOrdered { get; set; }
    }

    public class BookOrder
    {
        public string Id { get; set; }

        public decimal BookPrice { get; set; }
    }


namespace Messaging
{
    public interface IMessage
    {
    }

    public class Command : IMessage{}

    public class Event : IMessage
    {

        public Event(Guid id, Guid correlationId)
        {
            Id = id;
            CorrelationId = correlationId;
        }

        public Guid Id { get; }
        public Guid CorrelationId { get; }
    }

    //this can be moved to Azure service bus implementation
    public class InMemoryMessageBus 
    {
        private readonly Dictionary<Type, List<Action<IMessage>>> _handlers = new Dictionary<Type, List<Action<IMessage>>>();

        public void RegisterHandler<T>(Action<T> handler) where T : IMessage
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Action<IMessage>>();
                _handlers.Add(typeof(T), handlers);
            }

            handlers.Add(x => handler((T) x));
        }


        // public void Send<T>(T command) where T : Command
        // {
        //     if (_handlers.TryGetValue(command.GetType(), out var handlers))
        //     {
        //         if (handlers.Count != 1) throw new InvalidOperationException("cannot send to more than one handler");
        //         handlers[0](command);
        //     }
        //     else
        //         throw new InvalidOperationException("no handler registered");
        // }

        public void Publish<T>(T @event) where T : Event
        {
            if (!_handlers.TryGetValue(@event.GetType(), out var handlers))
            {
                if (!_handlers.TryGetValue(typeof(Event), out handlers))
                    return;
            }

            foreach (var handler in handlers)
                handler(@event);
        }

    }
}
    
}
