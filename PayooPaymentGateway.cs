using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using EPiServer.Commerce.Order;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Plugins.Payment;
using RestSharp;

namespace Foundation.Commerce.Payment.Payoo
{
    public class PayooPaymentGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(PayooPaymentGateway));

        private readonly IOrderRepository _orderRepository;
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly ICmsPaymentPropertyService _cmsPaymentPropertyService;
        private PayooConfiguration _paymentMethodConfiguration;

        public PayooPaymentGateway()
            : this(
                ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(),
                ServiceLocator.Current.GetInstance<IOrderRepository>(),
                ServiceLocator.Current.GetInstance<ICmsPaymentPropertyService>())
        { }

        public PayooPaymentGateway(
            IOrderNumberGenerator orderNumberGenerator,
            IOrderRepository orderRepository,
            ICmsPaymentPropertyService cmsPaymentPropertyService)
        {
            _orderNumberGenerator = orderNumberGenerator;
            _orderRepository = orderRepository;
            _cmsPaymentPropertyService = cmsPaymentPropertyService;
            _paymentMethodConfiguration = new PayooConfiguration(Settings);
        }

        /// <summary>
        /// Main entry point of ECF Payment Gateway.
        /// </summary>
        /// <param name="payment">The payment to process</param>
        /// <param name="message">The message.</param>
        /// <returns>return false and set the message will make the WorkFlow activity raise PaymentExcetion(message)</returns>
        public override bool ProcessPayment(Mediachase.Commerce.Orders.Payment payment, ref string message)
        {
            var orderGroup = payment.Parent.Parent;

            var paymentProcessingResult = ProcessPayment(orderGroup, payment);

            if (!string.IsNullOrEmpty(paymentProcessingResult.RedirectUrl))
            {
                HttpContext.Current.Response.Redirect(paymentProcessingResult.RedirectUrl);
            }

            message = paymentProcessingResult.Message;
            return paymentProcessingResult.IsSuccessful;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="orderGroup">The order group.</param>
        /// <param name="payment">The payment.</param>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (HttpContext.Current == null)
            {
                return PaymentProcessingResult.CreateSuccessfulResult("ProcessPaymentNullHttpContext");
            }

            if (payment == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("PaymentNotSpecified");
            }

            var orderForm = orderGroup.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
            if (orderForm == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("PaymentNotAssociatedOrderForm");
            }

            _paymentMethodConfiguration = new PayooConfiguration(Settings);

            var payooOrder = CreatePayooOrder(orderGroup, payment);
            var checksum = CreatePayooChecksum(payooOrder);
            //Call Payoo gateway here
            var message = string.Empty;
            var response = ExecuteCreatePayooPreOrderRequest(payooOrder, checksum, out message);
            if (response == null || !response.IsSuccess)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(message);
            }

            UpdatePaymentPropertiesFromPayooResponse(orderGroup, payment, response);

            var redirectUrl = response.order.payment_url;
            message = $"---Payoo CreatePreOrder is successful. Redirect end user to {redirectUrl}";
            return PaymentProcessingResult.CreateSuccessfulResult(message, redirectUrl);
        }

        private PayooOrder CreatePayooOrder(IOrderGroup orderGroup, IPayment payment)
        {
            var orderNumberID = _orderNumberGenerator.GenerateOrderNumber(orderGroup);

            var order = new PayooOrder();
            order.Session = orderNumberID;
            order.BusinessUsername = _paymentMethodConfiguration.BusinessUsername;
            order.OrderCashAmount = (long)payment.Amount;
            order.OrderNo = orderNumberID;
            order.ShippingDays = 1;
            order.ShopBackUrl = $"{HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)}{_cmsPaymentPropertyService.GetPayooPaymentProcessingPage()}";
            order.ShopDomain = "https://vsm.local"; //HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            order.ShopID = long.Parse(_paymentMethodConfiguration.ShopID);
            order.ShopTitle = _paymentMethodConfiguration.ShopTitle;
            order.StartShippingDate = DateTime.Now.ToString("dd/MM/yyyy");
            order.NotifyUrl = $"{HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)}{_cmsPaymentPropertyService.GetPayooPaymentProcessingPage()}IpnListener";
            order.ValidityTime = DateTime.Now.AddDays(1).ToString("yyyyMMddHHmmss");

            var customer = CustomerContext.Current.GetContactById(orderGroup.CustomerId);
            order.CustomerName = customer.FullName ?? $"{customer.FirstName} {customer.MiddleName} {customer.LastName}";
            order.CustomerPhone = string.Empty;
            order.CustomerEmail = customer.Email;
            order.CustomerAddress = customer.PreferredShippingAddress?.Line1;
            order.CustomerCity = customer.PreferredShippingAddress?.City;

            order.OrderDescription = HttpUtility.UrlEncode(OrderDescriptionHTMLFactory.CreateOrderDescription(orderGroup));
            order.Xml = PaymentXMLFactory.GetPaymentXML(order);
            return order;
        }

        private string CreatePayooChecksum(PayooOrder payooOrder)
        {
            var checksumKey = _paymentMethodConfiguration.ChecksumKey;
            return Utilities.EncryptSHA512($"{checksumKey}{payooOrder.Xml}");
        }

        private CreateOrderResponse ExecuteCreatePayooPreOrderRequest(PayooOrder payooOrder, string checksum, out string message)
        {
            try
            {
                var client = new RestClient(_paymentMethodConfiguration.ApiPayooCheckout);
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "multipart/form-data");
                request.AddParameter("data", payooOrder.Xml);
                request.AddParameter("checksum", checksum);
                request.AddParameter("refer", payooOrder.ShopDomain);
                IRestResponse response = client.Execute(request);
                message = $"Excute request to create Payoo Preorder with status {response.StatusCode}";

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JavaScriptSerializer objJson = new JavaScriptSerializer();
                    return objJson.Deserialize<CreateOrderResponse>(response.Content);
                }

                return null;
            }
            catch (Exception e)
            {
                message = e.Message;
                _logger.Error(e);
                return null;
            }
        }

        private void UpdatePaymentPropertiesFromPayooResponse(IOrderGroup orderGroup, IPayment payment, CreateOrderResponse response)
        {
            payment.Properties[Constant.PayooOrderIdPropertyName] = response.order.order_id;
            payment.Properties[Constant.PayooOrderNumberPropertyName] = response.order.order_no;
            payment.Properties[Constant.PayooAmountPropertyName] = response.order.amount;
            payment.Properties[Constant.PayooExpiryDatePropertyName] = response.order.expiry_date;
            payment.Properties[Constant.PayooPaymentCodePropertyName] = response.order.payment_code;
            _orderRepository.Save(orderGroup);
        }
    }
}
