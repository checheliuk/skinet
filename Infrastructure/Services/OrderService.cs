using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository _basketRepo;
        private readonly IUnItOfWork _unItOfWork;
        private readonly IPaymentService _paymentService;
        public OrderService(IBasketRepository basketRepo, IUnItOfWork unItOfWork, IPaymentService paymentService)
        {
            _paymentService = paymentService;
            _unItOfWork = unItOfWork;
            _basketRepo = basketRepo;
        }

        public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, Address shippingAddress)
        {
            var basket = await _basketRepo.GetBasketAsync(basketId);
            var items = new List<OrderItem>();

            foreach (var item in basket.Items)
            {
                var productItem = await _unItOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);
            }

            var deliveryMethod = await _unItOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);
            var subtotal = items.Sum(item => item.Price * item.Quantity);
            var spec = new OrderByPaymentIntentIdSpecification(basket.PaymentIntentId);
            var existingOrder = await _unItOfWork.Repository<Order>().GetEnityWithSpec(spec);
            if (existingOrder != null) 
            {
                _unItOfWork.Repository<Order>().Delete(existingOrder);
                await _paymentService.CreateOrUpdatePaymentIntent(basket.PaymentIntentId);
            }
            var order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal, basket.PaymentIntentId);
            _unItOfWork.Repository<Order>().Add(order);
            var result = await _unItOfWork.Complete();

            if (result <= 0)
            {
                return null;
            }

            return order;
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await _unItOfWork.Repository<DeliveryMethod>().ListAllAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);
            return await _unItOfWork.Repository<Order>().GetEnityWithSpec(spec);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(buyerEmail);
            return await _unItOfWork.Repository<Order>().ListAsync(spec);
        }
    }
}