using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Data;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core.Features;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Security;

namespace Foundation.Commerce.Payment.Payoo
{
    public interface IPayooCartService
    {
        bool DoCompletingCart(ICart cart, IList<string> errorMessages);
        IPurchaseOrder MakePurchaseOrder(ICart cart, IPayment payment);
    }

    [ServiceConfiguration(typeof(IPayooCartService), Lifecycle = ServiceInstanceScope.Transient)]
    public class PayooCartService : IPayooCartService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IFeatureSwitch _featureSwitch;
        private readonly IInventoryProcessor _inventoryProcessor;
        private static Lazy<DatabaseMode> _databaseMode = new Lazy<DatabaseMode>(() => GetDefaultDatabaseMode());

        public PayooCartService()
            : this(
                ServiceLocator.Current.GetInstance<IFeatureSwitch>(),
                ServiceLocator.Current.GetInstance<IInventoryProcessor>(),
                ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public PayooCartService(
            IFeatureSwitch featureSwitch,
            IInventoryProcessor inventoryProcessor,
            IOrderRepository orderRepository)
        {
            _featureSwitch = featureSwitch;
            _inventoryProcessor = inventoryProcessor;
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Validates and completes a cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="errorMessages">The error messages.</param>
        public virtual bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            // Change status of payments to processed. 
            // It must be done before execute workflow to ensure payments which should mark as processed.
            // To avoid get errors when executed workflow.
            foreach (IPayment p in cart.Forms.SelectMany(f => f.Payments).Where(p => p != null))
            {
                PaymentStatusManager.ProcessPayment(p);
            }

            var isSuccess = true;

            if (_databaseMode.Value != DatabaseMode.ReadOnly)
            {
                if (_featureSwitch.IsSerializedCartsEnabled())
                {
                    var validationIssues = new Dictionary<ILineItem, IList<ValidationIssue>>();
                    cart.AdjustInventoryOrRemoveLineItems(
                        (item, issue) => AddValidationIssues(validationIssues, item, issue), _inventoryProcessor);

                    isSuccess = !validationIssues.Any();

                    foreach (var issue in validationIssues.Values.SelectMany(x => x).Distinct())
                    {
                        if (issue == ValidationIssue.RejectedInventoryRequestDueToInsufficientQuantity)
                        {
                            errorMessages.Add("NotEnoughStockWarning");
                        }
                        else
                        {
                            errorMessages.Add("CartValidationWarning");
                        }
                    }

                    return isSuccess;
                }

                // Execute CheckOutWorkflow with parameter to ignore running process payment activity again.
                var isIgnoreProcessPayment = new Dictionary<string, object> { { "PreventProcessPayment", true } };
                var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup)cart,
                    OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

                var warnings = workflowResults.OutputParameters["Warnings"] as StringDictionary;
                isSuccess = warnings.Count == 0;

                foreach (string message in warnings.Values)
                {
                    errorMessages.Add(message);
                }
            }

            return isSuccess;
        }

        public virtual IPurchaseOrder MakePurchaseOrder(ICart cart, IPayment payment)
        {
            var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
            purchaseOrder.OrderNumber = payment.Properties[Constant.PayooOrderNumberPropertyName] as string;

            if (_databaseMode.Value != DatabaseMode.ReadOnly)
            {
                // Update last order date time for CurrentContact
                UpdateLastOrderTimestampOfCurrentContact(CustomerContext.Current.CurrentContact, purchaseOrder.Created);
            }

            AddNoteToPurchaseOrder(string.Empty, $"New order placed by {PrincipalInfo.CurrentPrincipal.Identity.Name} in Public site", Guid.Empty, purchaseOrder);

            // Remove old cart
            _orderRepository.Delete(cart.OrderLink);
            purchaseOrder.OrderStatus = OrderStatus.InProgress;

            _orderRepository.Save(purchaseOrder);

            return purchaseOrder;
        }

        /// <summary>
        /// Update last order time stamp which current user completed.
        /// </summary>
        /// <param name="contact">The customer contact.</param>
        /// <param name="datetime">The order time.</param>
        protected void UpdateLastOrderTimestampOfCurrentContact(CustomerContact contact, DateTime datetime)
        {
            if (contact != null)
            {
                contact.LastOrder = datetime;
                contact.SaveChanges();
            }
        }

        /// <summary>
        /// Adds the note to purchase order.
        /// </summary>
        /// <param name="title">The note title.</param>
        /// <param name="detail">The note detail.</param>
        /// <param name="customerId">The customer Id.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        protected void AddNoteToPurchaseOrder(string title, string detail, Guid customerId, IPurchaseOrder purchaseOrder)
        {
            var orderNote = purchaseOrder.CreateOrderNote();
            orderNote.Type = OrderNoteTypes.System.ToString();
            orderNote.CustomerId = customerId != Guid.Empty ? customerId : PrincipalInfo.CurrentPrincipal.GetContactId();
            orderNote.Title = !string.IsNullOrEmpty(title) ? title : detail.Substring(0, Math.Min(detail.Length, 24)) + "...";
            orderNote.Detail = detail;
            orderNote.Created = DateTime.UtcNow;
            purchaseOrder.Notes.Add(orderNote);
        }

        protected void AddValidationIssues(IDictionary<ILineItem, IList<ValidationIssue>> issues, ILineItem lineItem, ValidationIssue issue)
        {
            if (!issues.ContainsKey(lineItem))
            {
                issues.Add(lineItem, new List<ValidationIssue>());
            }

            if (!issues[lineItem].Contains(issue))
            {
                issues[lineItem].Add(issue);
            }
        }

        private static DatabaseMode GetDefaultDatabaseMode()
        {
            if (!_databaseMode.IsValueCreated)
            {
                return ServiceLocator.Current.GetInstance<IDatabaseMode>().DatabaseMode;
            }
            return _databaseMode.Value;
        }
    }
}
