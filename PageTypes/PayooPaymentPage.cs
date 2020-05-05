using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace Foundation.Commerce.Payment.Payoo
{
    [ContentType(GUID = "2FF561E5-0526-4904-B035-0DB56DF5C597",
        DisplayName = "Payoo Payment Page",
        Description = "Payoo Payment Process Page.",
        GroupName = "Payment",
        Order = 100)]
    public class PayooPaymentPage : PageData
    {
    }
}
