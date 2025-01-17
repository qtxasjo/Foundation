using EPiServer.DataAnnotations;
using Foundation.Cms.Pages;

namespace Foundation.Commerce.Models.Pages
{
    [ContentType(DisplayName = "Subscription Details",
        GUID = "8eaf6fe8-3bf3-4f54-9b4a-06a1569087e1",
        Description = "Page for customer to see their subscription details.",
        GroupName = CommerceGroupNames.Commerce,
        AvailableInEditMode = false)]
    [ImageUrl("~/assets/icons/cms/pages/CMS-icon-page-14.png")]
    public class SubscriptionDetailPage : FoundationPageData
    {

    }
}