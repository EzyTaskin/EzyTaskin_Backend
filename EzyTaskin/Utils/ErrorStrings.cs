namespace EzyTaskin.Utils;

public static class ErrorStrings
{
    public static string ErrorTryAgain => "An error occurred. Please try again.";

    public static string InvalidCategory => "The specified category is invalid.";

    public static string InvalidOffer => "The specified offer is invalid.";

    public static string InvalidPaymentMethod => "The specified payment method is invalid.";

    public static string InvalidProfileType => "The speicifed profile type is invalid.";

    public static string InvalidRequest => "The specified service request is invalid.";

    public static string NoPaymentMethod => "A party has not registered any payment method.";

    public static string NotAConsumer => "This functionality is only available to consumers.";

    public static string NotAProvider => "This functionality is only available to providers.";

    public static string OfferPriceExceedsBudget => "Offer price exceeds service request's budget";

    public static string PremiumAlreadyActive => "You already have an active Premium subscription.";

    public static string PremiumNotActive => "You do not have an active Premium subscription.";

    public static string PremiumRequired => "This feature requires a Premium account.";

    public static string RequestAlreadyComplete => "This request is already complete.";

    public static string SessionExpired => "Session expired.";
}
