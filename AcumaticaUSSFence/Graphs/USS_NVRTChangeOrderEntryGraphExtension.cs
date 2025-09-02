using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Descriptor;
using NAWUnitedSiteServices.Prototype.Extensions.DACExtensions;
using NAWUnitedSiteServices.Prototype.Extensions.GraphExtensions;
using NV.Rental360.ChangeOrders;
using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.SM;
using PX.TM;
using PX.Objects.AR;

namespace Graphs
{
    public class USS_NVRTChangeOrderEntryGraphExtension : PXGraphExtension<NAWNVRTChangeOrderEntryGraphExtensionUSSExt, NVRTChangeOrderEntry>
    {
        public static bool IsActive() => true;

        public override void Initialize()
        {
            base.Initialize();
            ChangeOrderApprovalHistory.View = new PXView(Base, false, ChangeOrderApprovalHistory.View.BqlSelect, new PXSelectDelegate(changeOrderApprovalHistory));
        }

        #region Views

        public PXSelectJoin<
                 USSChangeOrderApproval,
                 LeftJoin<Contact, On<Contact.contactID, Equal<USSChangeOrderApproval.approverID>>>,
                 Where<USSChangeOrderApproval.refNoteID, Equal<Current<NVRTChangeOrder.noteID>>>>
                 ChangeOrderApprovalHistory;

        #endregion

        #region Actions

        #region Story: FOX-759 | Engineer: [Satej Ambekar] | Date: [2025-06-03] | Approvals should be applied CH quotes (to inc change order & extension quotes).
        public PXAction<NVRTChangeOrder> ApproveDis;
        [PXUIField(DisplayName = "Approve Discount", MapEnableRights = PXCacheRights.Select)]
        [PXButton(CommitChanges = true)]
        protected virtual IEnumerable approveDis(PXAdapter adapter)
        {
            var changeOrder = Base.Document.Current;
            var approverInfos = GetQuoteApproverInfos(Base);
            var allowedUsernames = approverInfos.Select(x => x.Username).ToList();
            bool isApprover = allowedUsernames.Contains(Base.Accessinfo.UserName);
            if (!isApprover)
            {
                throw new PXException(Descriptor.Messages.ApprovalToChangeOrder);
            }
            UpdateApprovalRecordOnApprove(changeOrder);
            return adapter.Get<NVRTChangeOrder>();
        }

        public PXAction<NVRTChangeOrder> Reject;
        [PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select)]
        [PXButton(CommitChanges = true)]
        protected virtual IEnumerable reject(PXAdapter adapter)
        {
            var changeOrder = Base.Document.Current;
            var approverInfos = GetQuoteApproverInfos(Base);
            var allowedUsernames = approverInfos.Select(x => x.Username).ToList();
            bool isApprover = allowedUsernames.Contains(Base.Accessinfo.UserName);
            if (!isApprover)
            {
                throw new PXException(Descriptor.Messages.ApprovalToChangeOrder);
            }
            return adapter.Get<NVRTChangeOrder>();
        }
        #endregion

        #endregion

        #region Event Handlers

        #region Story: FOX-759 | Engineer: [Satej Ambekar] | Date: [2025-06-03] | Approvals should be applied CH quotes (to inc change order & extension quotes).
        [PXUIField(DisplayName = "Status")]
        [PXMergeAttributes(Method = MergeMethod.Replace)]
        [PXDBString(1, IsFixed = true)]
        [NVRTListAttributeExt.NVFSWList]
        [PXDefault()]
        protected virtual void _(Events.CacheAttached<NVRTChangeOrder.status> e) { }
        #endregion

        protected virtual void _(Events.RowSelected<NVRTChangeOrder> e)
        {
            if (e.Row == null) return;
            var ext = PXCache<NVRTChangeOrder>.GetExtension<NVRTChangeOrderExt>(e.Row);
            #region Story: FOX-759 | Engineer: [Satej Ambekar] | Date: [2025-06-03] | Approvals should be applied CH quotes (to inc change order & extension quotes).
            var lines = PXSelect<NVRTChangeOrderLine,
                Where<NVRTChangeOrderLine.orderType, Equal<Required<NVRTChangeOrder.orderType>>,
                    And<NVRTChangeOrderLine.orderNbr, Equal<Required<NVRTChangeOrder.orderNbr>>>>>
                .Select(Base, e.Row.OrderType, e.Row.OrderNbr)
                .RowCast<NVRTChangeOrderLine>();
            ext.MaxDiscPct = lines.Any() ? lines.Max(l => l.DiscPct ?? 0m) : 0m;
            bool isPendingApproval = e.Row.Status == "P";
            PXAction approveDisAction = Base.Actions["ApproveDis"];
            if (approveDisAction != null)
                approveDisAction.SetVisible(isPendingApproval);

            PXAction rejectAction = Base.Actions["Reject"];
            if (rejectAction != null)
                rejectAction.SetVisible(isPendingApproval);
            #endregion
           
            NVRTChangeOrder NVRTChangeOrderDetails = (NVRTChangeOrder)e.Row;
            HandlePendingApproval(e.Cache, NVRTChangeOrderDetails); //FOX-953 
        }
        protected virtual void _(Events.RowUpdated<NVRTChangeOrder> e)
        {
            if (e.Row == null || e.OldRow == null)
                return;
            if (e.Row.Status == "P" && e.OldRow.Status != "P")
            {
                InsertApprovalHistoryForPendingStatus(e.Row);// Story: FOX-759
            }
        }

        #region Story: FOX-947 | Engineer: [Abhishek Sonone] | Date: [2025-07-23] | Change Order line discount percent should be limited from -999% to 100% to support Approval workflow.

        protected void _(Events.FieldVerifying<NVRTChangeOrderLine, NVRTChangeOrderLine.discPct> e)
        {
            if (e.NewValue == null) return;
            decimal value = (decimal)e.NewValue;
            if (value < -999.00m || value > 100.00m)
            {
                throw new PXSetPropertyException(e.Row,Descriptor.Messages.DiscountPercentOutOfRange);
            }
        }
        #endregion

        #endregion

        #region Helper Methods

        #region Story: FOX-770 | Engineer: [Abhishek Sonone] | Date: [2025-04-28] | Default the Fence Quote type on the PDF based on the customer's setting when creating EX or CH quotes.
        [PXUIField(DisplayName = "Print Change Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(CommitChanges = true, DisplayOnMainToolbar = false, Connotation = ActionConnotation.None, Category = "Reports")]
        public virtual IEnumerable changeOrderRpt(PXAdapter adapter)
        {
            NVRTChangeOrder current = base.Base.Document.Current;
            if (current != null)
            {
                base.Base.Save.Press();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty("NVRT3410"))
                {
                    List<NVRTChangeOrder> list = adapter.Get<NVRTChangeOrder>().ToList();
                    string text = null;
                    PXReportRequiredException ex = null;
                    Dictionary<PrintSettings, PXReportRequiredException> reportsToPrint = new Dictionary<PrintSettings, PXReportRequiredException>();

                    //Fetch the quoteType based on OrderType and OrderNbr from NVRTChangeOrder
                    Customer result = PXSelect<Customer,
                                       Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                       .Select(Base, Base.Document.Current?.CustomerID);

                    string quoteType = null;
                    foreach (NVRTChangeOrder item in list)
                    {
                        dictionary = new Dictionary<string, string>();
                        dictionary["NVRTChangeOrder.OrderType"] = item.OrderType;
                        dictionary["NVRTChangeOrder.OrderNbr"] = item.OrderNbr;

                        if (result != null)
                        {
                            //Customer customer = result;
                            var customerExt = result.GetExtension<NAWxCustomerUSSExt>();
                            quoteType = customerExt?.UsrNAWDefaultFenceQuoteType;
                        }
                        if (!string.IsNullOrEmpty(quoteType))
                        {
                            string format = quoteType == "S" ? "S" : "D";
                            dictionary["Format"] = format;
                        }
                        text = new NotificationUtility(Base).SearchCustomerReport("NVRT3410", item.CustomerID, item.BranchID);
                        ex = PXReportRequiredException.CombineReport(ex, text, dictionary, OrganizationLocalizationHelper.GetCurrentLocalization(Base));
                        ex.Mode = PXBaseRedirectException.WindowMode.New;
                        reportsToPrint = SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, dictionary, adapter, new NotificationUtility(Base).SearchPrinter, "Customer", "NVRT3410", text, item.BranchID, OrganizationLocalizationHelper.GetCurrentLocalization(Base));
                    }
                    if (ex != null)
                    {
                        Base.LongOperationManager.StartOperation(async delegate (CancellationToken ct)
                        {
                            await SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint, ct);
                            throw ex;
                        });
                    }
                    throw new PXReportRequiredException(dictionary, "NVRT3410", PXBaseRedirectException.WindowMode.New, "Change Order Report");
                }
            }

            return adapter.Get();
        }
        #endregion

        #endregion

        #region Story: FOX-759 | Engineer: [Satej Ambekar] | Date: [2025-06-03] | Approvals should be applied CH quotes (to inc change order & extension quotes).

        #region Approval Logic
        private void InsertApprovalHistoryForPendingStatus(NVRTChangeOrder changeOrder)
        {
            if (changeOrder == null)
                return;

            var approvers = GetQuoteApproverInfos(Base);
            foreach (var approver in approvers)
            {
                var existing = PXSelect<USSChangeOrderApproval,
                    Where<USSChangeOrderApproval.refNoteID, Equal<Required<USSChangeOrderApproval.refNoteID>>,
                        And<USSChangeOrderApproval.approverID, Equal<Required<USSChangeOrderApproval.approverID>>>>>
                    .Select(Base, changeOrder.NoteID, approver.ContactID)
                    .FirstOrDefault();
                var preferencesResult = PXSelect<PreferencesEmail>.Select(Base).FirstOrDefault();
                string notificationSiteUrl = (preferencesResult != null)
                    ? ((PreferencesEmail)preferencesResult).NotificationSiteUrl
                    : null;


                if (existing == null)
                {
                    var history = new USSChangeOrderApproval
                    {
                        RefNoteID = changeOrder.NoteID,
                        OrderType = changeOrder.OrderType,
                        OrderNbr = changeOrder.OrderNbr,
                        ApproverID = approver.ContactID,
                        ApproverDisplayName = approver.DisplayName,
                        ApproverEmail = approver.Email,
                        Status = "P", // Pending
                        Action = "Pending",
                        NotificationSiteUrl = notificationSiteUrl
                    };
                    Base.Caches<USSChangeOrderApproval>().Insert(history);
                }
            }
        }
        public static List<ApproverInfo> GetQuoteApproverInfos(PXGraph graph)
        {
            var workgroupResult = PXSelect<EPCompanyTree,
                Where<EPCompanyTree.description, Equal<Required<EPCompanyTree.description>>>>
                .Select(graph, "Change Order Approvers")
                .FirstOrDefault();
            EPCompanyTree workgroup = workgroupResult?.GetItem<EPCompanyTree>();
            if (workgroup == null)
                return new List<ApproverInfo>();

            var results = PXSelectJoin<EPCompanyTreeMember,
                InnerJoin<Contact, On<Contact.contactID, Equal<EPCompanyTreeMember.contactID>>,
                LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>>>,
                Where<EPCompanyTreeMember.workGroupID, Equal<Required<EPCompanyTreeMember.workGroupID>>>>
                .Select(graph, workgroup.WorkGroupID);

            var members = new List<ApproverInfo>();
            foreach (PXResult<EPCompanyTreeMember, Contact, Users> r in results)
            {
                var member = (EPCompanyTreeMember)r;
                var contact = (Contact)r;
                var user = (Users)r;

                if (member.ContactID.HasValue && user?.Username != null)
                {
                    members.Add(new ApproverInfo
                    {
                        ContactID = member.ContactID,
                        Username = user.Username,
                        Email = contact.EMail,
                        DisplayName = contact.DisplayName
                    });
                }
            }
            return members;
        }        
        private void UpdateApprovalRecordOnApprove(NVRTChangeOrder changeOrder)
        {
            if (changeOrder?.NoteID == null)
                return;

            var existingResult = PXSelect<USSChangeOrderApproval,
                Where<USSChangeOrderApproval.refNoteID, Equal<Required<USSChangeOrderApproval.refNoteID>>,
                    And<USSChangeOrderApproval.approverID, Equal<Required<USSChangeOrderApproval.approverID>>>>>
                .Select(Base, changeOrder.NoteID, Base.Accessinfo.ContactID)
                .FirstOrDefault();
            var existing = existingResult != null ? (USSChangeOrderApproval)existingResult : null;

            var approverInfos = GetQuoteApproverInfos(Base);
            var approver = approverInfos.FirstOrDefault(a => a.ContactID == Base.Accessinfo.ContactID);

            if (existing != null && approver != null)
            {
                existing.Status = "A";
                existing.ActionDate = PXTimeZoneInfo.Now;
                existing.ActionByID = Base.Accessinfo.ContactID;
                existing.Action = "Approved";
                existing.ApproverDisplayName = approver.DisplayName;
                existing.ApproverEmail = approver.Email;
                Base.Caches<USSChangeOrderApproval>().Update(existing);
                Base.Actions.PressSave();
            }

        }
        public IEnumerable changeOrderApprovalHistory()
        {
            var changeOrder = Base.Document.Current;
            if (changeOrder == null || changeOrder.NoteID == null)
                yield break;

            var approvers = GetQuoteApproverInfos(Base);
            var history = PXSelectJoin<
                USSChangeOrderApproval,
                LeftJoin<Contact, On<Contact.contactID, Equal<USSChangeOrderApproval.approverID>>>,
                Where<USSChangeOrderApproval.refNoteID, Equal<Required<USSChangeOrderApproval.refNoteID>>>>
                .Select(Base, changeOrder.NoteID)
                .Cast<PXResult<USSChangeOrderApproval, Contact>>()
                .ToList();

            foreach (var rec in history)
            {
                var hist = (USSChangeOrderApproval)rec;
                var contact = (Contact)rec;
                hist.ApproverDisplayName = contact?.DisplayName;
                hist.ApproverEmail = contact?.EMail;
                yield return hist;
            }

            var approvedIds = history.Select(h => ((USSChangeOrderApproval)h).ApproverID).ToHashSet();
            foreach (var approver in approvers.Where(a => !approvedIds.Contains(a.ContactID)))
            {
                yield return new USSChangeOrderApproval
                {
                    ApproverID = approver.ContactID,
                    ApproverDisplayName = approver.DisplayName,
                    ApproverEmail = approver.Email,
                    Status = "P",
                    Action = "Pending"
                };
            }
        }

        #endregion

        #region Helper Classes       

        public class ApproverInfo
        {
            public int? ContactID { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string DisplayName { get; set; }
        }

        #endregion

        #endregion

        #region Private Methods

        #region Story: FOX-953 | Engineer: [Abhishek Sonone] | Date: [2025-07-28] | When CH or EX quotes are Pending Approval - restrict editable fields in header to align with CH/EX quotes.

        private void HandlePendingApproval(PXCache cache, NVRTChangeOrder changeOrders)
        {
            if (changeOrders == null) return;

            if (changeOrders.Status == "P")
            {
                cache = Base.Caches[typeof(NVRTChangeOrder)];

                // Loop through all fields in the DAC
                foreach (var field in cache.Fields)
                {
                    // Skip this fields — keep it editable
                    if (field == nameof(NVRTChangeOrder.description) || field == nameof(NVRTChangeOrder.CustomerRefNbr) || field == nameof(NVRTChangeOrder.orderDate) || field == nameof(NVRTChangeOrder.ScheduleStartDate)) continue;
                    PXUIFieldAttribute.SetEnabled(cache, changeOrders, field, false);
                }

                PXUIFieldAttribute.SetEnabled<NVRTChangeOrder.description>(cache, changeOrders, true);
                PXUIFieldAttribute.SetEnabled<NVRTChangeOrder.orderDate>(cache, changeOrders, true);
            }

        }

        #endregion
        
        #endregion

    }
}