using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Books.API.Controllers;
using Books.API.Controllers.Messaging;

namespace Books.API.ShippingService
{
    public class PurchaseOrderReceivedHandler
    {
        private readonly InMemoryMessageBus _bus;
        private readonly ShippingContext _context;

        public PurchaseOrderReceivedHandler(InMemoryMessageBus bus, ShippingContext context)
        {
            _bus = bus;
            _context = context;
        }
        public void Handle(PurchaseOrderReceived @event)
        {
            // Invoke the shipping domain to check stock for every book using this service's own db
            // calculate shipping cost based on destination
            // if all is OK, store the shipping manifest and publish an event
            var totalCostWithoutShipping = @event.BooksOrdered.Sum(x => x.BookPrice);
            var shippingCost =  totalCostWithoutShipping * 0.2m;
            var shippingManifest = new ShippingManifest
            { 
                BookIds = String.Join(", ", @event.BooksOrdered.Select(x => x.Id).ToList()), 
                ShippingCost = shippingCost, 
                ShippingReference = $"SP{@event.Id}"
            };
            _context.ShippingManifests.Add(shippingManifest);
            _context.SaveChanges();
            _bus.Publish(new OrderShipped(Guid.NewGuid(), @event.CorrelationId, shippingManifest.ShippingReference, 
                @event.BooksOrdered.Select(x => x.Id).ToList(), shippingCost, totalCostWithoutShipping + shippingCost));
        }
    }

    public class OrderShipped : Event
    {
        public OrderShipped(Guid id, Guid correlationId, string shippingReference, 
            List<string> itemIds, decimal shippingCost, decimal orderTotal) : base(id, correlationId)
        {
            ShippingReference = shippingReference;
            ItemIds = itemIds;
            ShippingCost = shippingCost;
            OrderTotal = orderTotal;
        }

        public string ShippingReference { get; }
        public List<string> ItemIds { get; }
        public decimal ShippingCost { get; }
        public decimal OrderTotal { get; }
    }

    public class ShippingManifest
    {
        [Key]
        public string ShippingReference { get; set; }
        public string BookIds { get; set; }
        public decimal ShippingCost { get; set; }

    }
}