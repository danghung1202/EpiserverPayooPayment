using System;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc;

namespace Foundation.Commerce.Payment.Payoo.Controllers
{
    public class PayooPaymentController : PageController<PayooPaymentPage>
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(PayooPaymentController));

        private readonly IPayooCartService _cartService;
        private readonly ICmsPaymentPropertyService _cmsPaymentPropertyService;

        public PayooPaymentController() : this(
            ServiceLocator.Current.GetInstance<IPayooCartService>(),
            ServiceLocator.Current.GetInstance<ICmsPaymentPropertyService>())
        { }

        public PayooPaymentController(IPayooCartService cartService, ICmsPaymentPropertyService cmsPaymentPropertyService)
        {
            _cartService = cartService;
            _cmsPaymentPropertyService = cmsPaymentPropertyService;
        }

        public ActionResult Index()
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            var payooConfiguration = new PayooConfiguration();
            var paymentResult = ExtractPaymentResultFromPayoo();
            //get purchase order which was created in IpnListener
            var purchaseOrder = Utilities.GetPurchaseOrderByOrderNumber(paymentResult.OrderNo);
            if (paymentResult.IsSuccess && purchaseOrder != null)
            {
                var payment = purchaseOrder.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(payooConfiguration.PaymentMethodId));
                var orderNumber = payment?.Properties[Constant.PayooOrderNumberPropertyName] as string;
                if (VerifyChecksumIsValid(payooConfiguration.ChecksumKey, orderNumber, paymentResult))
                {
                    var confirmationOrderUrl = CreateAcceptRedirectionUrl(purchaseOrder);
                    return Redirect(confirmationOrderUrl);
                }
            }


            //In case purchase order can not created in IpnListener
            //Try create purchase order if payment successfully

            //var currentCart = _cartService.LoadDefaultCart();
            //if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
            //{
            //    throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Generic Error");
            //}

            //var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(payooConfiguration.PaymentMethodId));
            //if (payment == null)
            //{
            //    throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment Not Specified");
            //}

            //var orderNumber = payment.Properties[Constant.PayooOrderNumberPropertyName] as string;
            //if (string.IsNullOrEmpty(orderNumber))
            //{
            //    throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment Not Specified");
            //}

            // Redirect customer to receipt page

            var redirectUrl = CreateCancelRedirectionUrl();
            var message = paymentResult.Status.Equals("0") ? "Payment failed" : "Payment cancelled";
            TempData[Constant.ErrorMessages] = $"{message}. Payoo Message: {paymentResult.ErrorCode}-{paymentResult.ErrorMsg}";
            redirectUrl = _cartService.ProcessUnsuccessfulTransaction(redirectUrl, message);

            return Redirect(redirectUrl);
        }

        //[HttpPost]
        public ActionResult IpnListener()
        {
            var invoice = ExtractPaymentNotification();
            if (invoice == null || !invoice.State.Equals("PAYMENT_RECEIVED", StringComparison.OrdinalIgnoreCase)) return Content("Verified signature is faillure!");

            try
            {
                var payooConfiguration = new PayooConfiguration();
                var currentCart = _cartService.LoadDefaultCart();
                var payment = currentCart?.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(payooConfiguration.PaymentMethodId));
                if (payment != null)
                {
                    payment.Properties[Constant.PaymentMethod] = invoice.PaymentMethod;
                    _cartService.ProcessSuccessfulTransaction(currentCart, payment);
                }
                return Content("NOTIFY_RECEIVED");
            }
            catch (Exception ex)
            {
                _logger.Error("IpnListener:: Create purchase order failed", ex);
                return Content("NOTIFY_RECEIVED");
            }
        }

        private PaymentNotification ExtractPaymentNotification()
        {
            string notifyMessage = Request.Form.Get("NotifyData");
            if (string.IsNullOrEmpty(notifyMessage)) return null;

            var notifyPackage = Utilities.GetPayooConnectionPackage(notifyMessage);
            var objCms = new CmsCryptography();
            if (objCms.Verify(notifyPackage.Data, notifyPackage.Signature))
            {
                return Utilities.GetPaymentNotify(notifyPackage.Data);
            }
            return null;
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
                Checksum = Request.QueryString["checksum"],
                TotalAmount = Request.QueryString["totalAmount"],
                PaymentFree = Request.QueryString["paymentFee"]
            };
        }

        private bool VerifyChecksumIsValid(string checksumKey, string orderNumber, PayooPaymentResult payooResult)
        {
            var localChecksum = Utilities.EncryptSHA512($"{checksumKey}{payooResult.Session}.{orderNumber}.{payooResult.Status}");
            return localChecksum.Equals(payooResult.Checksum, StringComparison.OrdinalIgnoreCase);
        }

        private string CreateAcceptRedirectionUrl(IPurchaseOrder purchaseOrder)
        {
            var acceptUrl = _cmsPaymentPropertyService.GetAcceptedPaymentUrl();
            var redirectionUrl = UriUtil.AddQueryString(acceptUrl, "success", "true");
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "contactId", purchaseOrder.CustomerId.ToString());
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString());

            return redirectionUrl;
        }

        private string CreateCancelRedirectionUrl()
        {
            var cancelUrl = _cmsPaymentPropertyService.GetCancelledPaymentUrl();
            cancelUrl = UriUtil.AddQueryString(cancelUrl, "success", "false");
            cancelUrl = UriUtil.AddQueryString(cancelUrl, "paymentmethod", "payoo");
            return cancelUrl;
        }
    }
}
