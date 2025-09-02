using System;
using System.Collections;
using System.Collections.Generic;
using DAC;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;

namespace Graphs
{
    public class SOInvoiceEntry_Extension : PXGraphExtension<SOInvoiceEntry>
    {
        public static bool IsActive() => true;

        #region Events
        protected void _(Events.RowSelected<ARInvoice> e)
        {
            PXCache cache = e.Cache;
            SetDocumentDiscountVisibility(cache, e.Row);//Story: FOX-900
            SetRejectReasonCodeEditable(cache, e.Row);// Story: FOX-868
            DisableFenceFieldsIfReleased(cache, e.Row); // Story: FOX-870
            HandleReleasebutton(cache, e.Row);// story: FOX-869

        }

        protected void _(Events.RowPersisting<ARInvoice> e)
        {
            if (e.Row == null) return;
            var ext = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(e.Row);
            ValidateInvoiceReviewRequired(e, ext); // Story: FOX-867
            ValidateRejectReasonCode(e.Cache, e.Row, ext); // Story: FOX-868
        }
        #endregion
        #region Story: FOX-901 | Engineer: [Satej Ambekar] | Date: [2025-07-03] | Add Release Date to Billing Document 
        #region Action
        [PXOverride]
        public IEnumerable Release(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseRelease)
        {
             foreach (ARInvoice invoice in adapter.Get<ARInvoice>())
            {
                SetReleaseDateOnInvoiceRelease(invoice);
            }
            return baseRelease(adapter);
        }
        #endregion
        #endregion
        #region Private Methods

        #region Story: FOX-900  | Engineer: [Satej Ambekar] | Date: [2025-05-30] | Hide Order Level Discount field from SOOrder & ARInvoice
        private void SetDocumentDiscountVisibility(PXCache cache, ARInvoice row)
        {
            List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
            bool isSystemAdmin = userRolesList.Contains("Administrator");
            PXUIFieldAttribute.SetVisible<ARInvoice.curyDiscTot>(cache, row, isSystemAdmin);
        }
        #endregion

        #region Story: FOX-867 | Engineer: [Divya Kurumkar] | Date: [2025-06-02] | Add Fence – Invoice Review and Action field to SOInvoiceEntry
        private void ValidateInvoiceReviewRequired(Events.RowPersisting<ARInvoice> e, ARInvoiceExtension ext)
        {
            string screenID = PXContext.GetScreenID();
            if (screenID == "SO.30.30.00")
            {
                if (Base is SOInvoiceEntry
    && (ext.UsrFenceInvoiceReview == null || ext.UsrFenceInvoiceReview == ARInvoiceExtension.FenceInvoiceReviewAction.NotSelected)
    && e.Operation.Command() != PXDBOperation.Insert
    && e.Row.Status == ARDocStatus.Balanced)
                {
                    throw new PXRowPersistingException(
                        nameof(ARInvoiceExtension.UsrFenceInvoiceReview),
                        ext.UsrFenceInvoiceReview,
                        Descriptor.Messages.InvoiceReviewRequired);
                }
            }
        }
        #endregion

        #region Story: FOX-868 | Engineer: [Divya Kurumkar] | Date: [2025-06-02] | Add Reject Reason Code Field for Invoice Review
        private void ValidateRejectReasonCode(PXCache cache, ARInvoice row, ARInvoiceExtension ext)
        {
            if (ext.UsrFenceInvoiceReview == ARInvoiceExtension.FenceInvoiceReviewAction.Reject)
            {
                if (!ext.UsrFenceRejectReasonCode.HasValue)
                {
                    throw new PXRowPersistingException(
                        nameof(ARInvoiceExtension.UsrFenceRejectReasonCode),
                        ext.UsrFenceRejectReasonCode,
                        Descriptor.Messages.RejectReasonCodeRequired
                    );
                }
            }
            else if (ext.UsrFenceRejectReasonCode.HasValue)
            {
                ext.UsrFenceRejectReasonCode = null;
                cache.SetValue<ARInvoiceExtension.usrFenceRejectReasonCode>(row, null);
            }
        }
        private void SetRejectReasonCodeEditable(PXCache cache, ARInvoice row)
        {
            var ext = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(row);
            bool isReject = ext.UsrFenceInvoiceReview == ARInvoiceExtension.FenceInvoiceReviewAction.Reject;
            PXUIFieldAttribute.SetEnabled<ARInvoiceExtension.usrFenceRejectReasonCode>(cache, row, isReject);
        }
        #endregion

        #region Story: FOX-870 | Engineer: [Divya Kurumkar] | Date: [2025-06-03] | Make Invoice Review and Reject Reason Code fields non-editable if the invoice is released
        private void DisableFenceFieldsIfReleased(PXCache cache, ARInvoice row)
        {
            if (row == null) return;
            bool isReleased = row.Released == true;
            var ext = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(row);
            bool isReject = ext.UsrFenceInvoiceReview == ARInvoiceExtension.FenceInvoiceReviewAction.Reject;
            PXUIFieldAttribute.SetEnabled<ARInvoiceExtension.usrFenceInvoiceReview>(cache, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<ARInvoiceExtension.usrFenceRejectReasonCode>(cache, row, !isReleased && isReject);
        }
        #endregion

        #region Story: FOX-869 | Engineer: [Abhishek Sonone] | Date: [2025-06-12] | Release button is disabled for Billing Specialist on "Reject Invoice"; enabled for Billing Manager
        private void HandleReleasebutton(PXCache cache, ARInvoice row)
        {
            List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
            bool isBillingManger = userRolesList.Contains("Billing Manager");

            var ext = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(row);
            bool isRejectInvoice = ext?.UsrFenceInvoiceReview == 2;

            if (isRejectInvoice)
            {
                Base.release.SetEnabled(isBillingManger);
            }
            else
            {
                // In other cases, allow release for everyone
                Base.release.SetEnabled(true);
            }
        }
        #endregion

        #region Story: FOX-901 | Engineer: [Satej Ambekar] | Date: [2025-07-03] | Add Release Date to Billing Document 
        private void SetReleaseDateOnInvoiceRelease(ARInvoice invoice)
        {
            if (invoice == null) return;
            var ext = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(invoice);
            if (ext.UsrReleaseDate == null)
            {
                ext.UsrReleaseDate = Base.Accessinfo.BusinessDate;
                Base.Caches[typeof(ARInvoice)].SetValue<ARInvoiceExtension.usrReleaseDate>(invoice, ext.UsrReleaseDate);
            }
        }
        #endregion

        #endregion
    }
}