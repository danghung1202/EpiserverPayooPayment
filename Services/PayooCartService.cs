using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Order;
using EPiServer.Data;
using EPiServer.Logging.Compatibility;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
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
        string GetDefaultCartName();
        ICart LoadDefaultCart();
        IPurchaseOrder ProcessSuccessfulTransaction(ICart cart, IPayment payment);
        string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage);
        bool DoCompletingCart(ICart cart, IList<string> errorMessages);
        IPurchaseOrder MakePurchaseOrder(ICart cart, IPayment payment);
    }

    [ServiceConfiguration(typeof(IPayooCartService), Lifecycle = ServiceInstanceScope.Transient)]
    public class PayooCartService : IPayooCartService
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(PayooCartService));

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

        public virtual string GetDefaultCartName() => Cart.DefaultName;

        public virtual ICart LoadDefaultCart() => _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), GetDefaultCartName());

        /// <summary>
        /// Processes the successful transaction, was called when Payoo Gateway redirect back.
        /// </summary>
        /// <param name="cart"></param>
        /// <param name="payment">The order payment.</param>
        /// <returns>The url redirection after process.</returns>
        public virtual IPurchaseOrder ProcessSuccessfulTransaction(ICart cart, IPayment payment)
        {
            if (HttpContext.Current == null || cart == null)
            {
                return null;
            }
            // everything is fine
            var errorMessages = new List<string>();
            var cartCompleted = DoCompletingCart(cart, errorMessages);

            if (!cartCompleted)
            {
                _logger.Error(string.Join(";", errorMessages.Distinct().ToArray()));
                return null;
            }

            // Place order
            return MakePurchaseOrder(cart, payment);
        }

        /// <summary>
        /// Processes the unsuccessful transaction
        /// </summary>
        /// <param name="cancelUrl">The cancel url.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The url redirection after process.</returns>
        public virtual string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage)
        {
            if (HttpContext.Current == null)
            {
                return cancelUrl;
            }

            _logger.Error($"Payoo transaction failed [{errorMessage}].");
            return UriUtil.AddQueryString(cancelUrl, "message", HttpUtility.UrlEncode(errorMessage));
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
            //process payment
            var processed = ProcessPayment(cart);
            if (!processed) return false;

            var isSuccess = true;

            if (_databaseMode.Value != DatabaseMode.ReadOnly)
            {
                isSuccess = UpdateInventory(cart, errorMessages);
                if (!isSuccess) return false;

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

        protected bool ProcessPayment(ICart cart)
        {
            try
            {
                foreach (IPayment p in cart.Forms.SelectMany(f => f.Payments).Where(p => p != null))
                {
                    PaymentStatusManager.ProcessPayment(p);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Process Payment Failed", ex);
                return false;
            }
        }

        protected bool UpdateInventory(ICart cart, IList<string> errorMessages)
        {
            if (!_featureSwitch.IsSerializedCartsEnabled()) return true;

            var isSuccess = true;
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
