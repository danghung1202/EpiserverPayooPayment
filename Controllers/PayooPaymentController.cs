using System;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Security;

namespace Foundation.Commerce.Payment.Payoo.Controllers
{
    public class PayooPaymentController : PageController<PayooPaymentPage>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICmsPaymentPropertyService _cmsPaymentPropertyService;

        public PayooPaymentController() : this(
            ServiceLocator.Current.GetInstance<IOrderRepository>(),
            ServiceLocator.Current.GetInstance<ICmsPaymentPropertyService>())
        { }

        public PayooPaymentController(IOrderRepository orderRepository, ICmsPaymentPropertyService cmsPaymentPropertyService)
        {
            _orderRepository = orderRepository;
            _cmsPaymentPropertyService = cmsPaymentPropertyService;
        }

        public ActionResult Index()
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            var currentCart = _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), Cart.DefaultName + SiteDefinition.Current.StartPage.ID);
            if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Generic Error");
            }

            var payooConfiguration = new PayooConfiguration();
            var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(payooConfiguration.PaymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment Not Specified");
            }

            var orderNumber = payment.Properties[Constant.PayooOrderNumberPropertyName] as string;
            if (string.IsNullOrEmpty(orderNumber))
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment Not Specified");
            }

            // Redirect customer to receipt page
            var paymentResult = ExtractPaymentResultFromPayoo();
            var cancelUrl = _cmsPaymentPropertyService.GetCancelledPaymentUrl(); // get link to Checkout page
            cancelUrl = UriUtil.AddQueryString(cancelUrl, "success", "false");
            cancelUrl = UriUtil.AddQueryString(cancelUrl, "paymentmethod", "payoo");

            var redirectUrl = cancelUrl;
            if (VerifyChecksumIsValid(payooConfiguration.ChecksumKey, orderNumber, paymentResult))
            {
                var gateway = new PayooPaymentGateway();
                if (paymentResult.Status.Equals("1"))
                {
                    var acceptUrl = _cmsPaymentPropertyService.GetAcceptedPaymentUrl();
                    redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, acceptUrl, cancelUrl);
                }
                else
                {
                    var message = paymentResult.Status.Equals("0") ? "Payment failed via Payoo gateway" : "Payment cancelled via Payoo gateway";
                    TempData["ErrorMessages"] = message;
                    redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, message);
                }
            }

            return Redirect(redirectUrl);
        }

        private PayooPaymentResult ExtractPaymentResultFromPayoo()
        {
            return new PayooPaymentResult()
            {
                Session = Request.QueryString["session"],
                OrderNo = Request.QueryString["order_no"],
                Status = Request.QueryString["status"],
                ErrorCode = Request.QueryString["errorcode"],
                ErrorMsg = Request.QueryString["errormsg"],
                PaymentMethod = Request.QueryString["paymethod"],
                Bank = Request.QueryString["bank"],
                Checksum = Request.QueryString["checksum"]
            };
        }

        private bool VerifyChecksumIsValid(string checksumKey, string orderNumber, PayooPaymentResult payment)
        {
            var localChecksum = Utilities.EncryptSHA512($"{checksumKey}{payment.Session}.{orderNumber}.{payment.Status}");
            return localChecksum.Equals(payment.Checksum, StringComparison.OrdinalIgnoreCase);
        }
    }
}
