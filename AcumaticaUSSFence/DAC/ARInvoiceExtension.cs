using PX.Data;
using System;


namespace DAC
{
    public class ARInvoiceExtension:PXCacheExtension<PX.Objects.AR.ARInvoice>
    {
        public static bool IsActive() => true;
        #region Story: FOX-867 | Engineer: [Divya Kurumkar] | Date: [2025-06-02] | Add Fence – Invoice Review and Action field to SOInvoiceEntry
        #region UsrFenceInvoiceReview
        public class FenceInvoiceReviewAction
        {
            public const int NotSelected = 0;
            public const int Post = 1;
            public const int Reject = 2;
        }

        [PXDBInt]
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Invoice Review", Visibility = PXUIVisibility.SelectorVisible)]
        [PXIntList(
            new[] { FenceInvoiceReviewAction.NotSelected, FenceInvoiceReviewAction.Post, FenceInvoiceReviewAction.Reject },
            new[] { "Not Selected", "Post Invoice", "Reject Invoice" }
        )]
        public virtual int? UsrFenceInvoiceReview { get; set; }
        public abstract class usrFenceInvoiceReview : PX.Data.BQL.BqlInt.Field<usrFenceInvoiceReview> { }
        #endregion
        #endregion

        #region Story: FOX-868 | Engineer: [Divya Kurumkar] | Date: [2025-06-02] | Add Reject Reason Code Field for Invoice Review

        #region UsrFenceRejectReasonCode
        public class RejectReasonCode
        {
            public const int PriceMismatch = 11;
            public const int QuantityDiscrepancy = 12;
            public const int IncorrectServiceDates = 13;
            public const int MissingOneTimeCharges = 14;
        }
        [PXDBInt]
        [PXUIField(DisplayName = "Reject Reason Code")]
        [PXIntList(
            new[] {
                    RejectReasonCode.PriceMismatch,
                    RejectReasonCode.QuantityDiscrepancy,
                    RejectReasonCode.IncorrectServiceDates,
                    RejectReasonCode.MissingOneTimeCharges
            },
            new[] {
                    "Price Mismatch",
                    "Quantity Discrepancy",
                    "Incorrect Service Dates",
                    "Missing One-Time Charges"
            }
        )]
        public virtual int? UsrFenceRejectReasonCode { get; set; }
        public abstract class usrFenceRejectReasonCode : PX.Data.BQL.BqlInt.Field<usrFenceRejectReasonCode> { }
        #endregion
        #endregion

        #region Story: FOX-901 | Engineer: [Satej Ambekar] | Date: [2025-07-03] | Add Release Date to Billing Document screen
        #region Release Date
        [PXDBDateAndTime]
        [PXUIField(DisplayName = "Release Date")]
        public virtual DateTime? UsrReleaseDate { get; set; }
        public abstract class usrReleaseDate : PX.Data.BQL.BqlDateTime.Field<usrReleaseDate> { }
        #endregion
        #endregion

    }
}
