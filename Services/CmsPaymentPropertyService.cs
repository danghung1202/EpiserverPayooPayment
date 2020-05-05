using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Foundation.Commerce.Payment.Payoo
{
    public interface ICmsPaymentPropertyService
    {
        ContentReference PaymentSettingPage { get; }
        string CheckoutPagePropertyName { get; }
        string PayooPaymentPagePropertyName { get; }
        string OrderConfirmationPagePropertyName { get; }
        string GetCancelledPaymentUrl();
        string GetAcceptedPaymentUrl();
        string GetPayooPaymentProcessingPage();
    }

    [ServiceConfiguration(typeof(ICmsPaymentPropertyService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CmsPaymentPropertyService : ICmsPaymentPropertyService
    {
        public virtual ContentReference PaymentSettingPage => ContentReference.StartPage;
        public virtual string CheckoutPagePropertyName => "CheckoutPage";
        public virtual string PayooPaymentPagePropertyName => "PayooPaymentPage";
        public virtual string OrderConfirmationPagePropertyName => "OrderConfirmationPage";

        private readonly IContentLoader _contentLoader;
        private readonly UrlResolver _urlResolver;
        public CmsPaymentPropertyService(IContentLoader contentLoader, UrlResolver urlResolver)
        {
            _contentLoader = contentLoader;
            _urlResolver = urlResolver;
        }

        public virtual string GetCancelledPaymentUrl()
        {
            return this.GetUrlFromPaymentSettingPageProperty(CheckoutPagePropertyName);
        }

        public virtual string GetAcceptedPaymentUrl()
        {
            return this.GetUrlFromPaymentSettingPageProperty(OrderConfirmationPagePropertyName);
        }

        public virtual string GetPayooPaymentProcessingPage()
        {
            return this.GetUrlFromPaymentSettingPageProperty(PayooPaymentPagePropertyName);
        }

        /// <summary>
        /// Gets url from start page's page reference property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The friendly url.</returns>
        private string GetUrlFromPaymentSettingPageProperty(string propertyName)
        {
            var startPageData = _contentLoader.Get<PageData>(PaymentSettingPage);
            if (startPageData == null)
            {
                return _urlResolver.GetUrl(ContentReference.StartPage);
            }

            var contentLink = startPageData.Property[propertyName]?.Value as ContentReference;
            if (!ContentReference.IsNullOrEmpty(contentLink))
            {
                return _urlResolver.GetUrl(contentLink);
            }
            return _urlResolver.GetUrl(ContentReference.StartPage);
        }

    }
}
