using PX.Common;

namespace Descriptor
{
    [PXLocalizable]
    public static class Messages
    {
        public const string QuantityMustBeGreaterThanZero = "Quantity must be greater than 0";
        public const string InvoiceReviewRequired = "Please select an action in the ‘Fence – Invoice Review and Action’ field.";
        public const string RejectReasonCodeRequired = "Please select a Reject Reason Code.";
        public const string ApprovalToChangeOrder = "You are not authorized to approve this Change Order.";
        public const string DiscountPercentOutOfRange = "Discount percent must be between -999.00 and 100.00.";

    }

}
