using PX.Data;
using System;

[Serializable]
[PXCacheName("Change Order Approval History")]
public class USSChangeOrderApproval : PXBqlTable ,IBqlTable
{
    #region HistoryID
    [PXDBIdentity(IsKey = true)]
    public virtual int? HistoryID { get; set; }
    public abstract class historyID : PX.Data.BQL.BqlInt.Field<historyID> { }
    #endregion

    #region RefNoteID
    [PXDBGuid]
    [PXUIField(DisplayName = "Change Order NoteID")]
    public virtual Guid? RefNoteID { get; set; }
    public abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }
    #endregion

    #region OrderType
    [PXDBString(2, IsFixed = true)]
    [PXUIField(DisplayName = "Change Order Type", Enabled = false)]
    public virtual string OrderType { get; set; }
    public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
    #endregion

    #region OrderNbr
    [PXDBString(15, IsUnicode = true)]
    [PXUIField(DisplayName = "Change Order Nbr", Enabled = false)]
    public virtual string OrderNbr { get; set; }
    public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
    #endregion

    #region ApproverDisplayName
    [PXDBString(255, IsUnicode = true)]
    [PXUIField(DisplayName = "Approver Name", Enabled = false)]
    public virtual string ApproverDisplayName { get; set; }
    public abstract class approverDisplayName : PX.Data.BQL.BqlString.Field<approverDisplayName> { }
    #endregion

    #region ApproverEmail
    [PXDBString(255, IsUnicode = true)]
    [PXUIField(DisplayName = "Approver Email", Enabled = false)]
    public virtual string ApproverEmail { get; set; }
    public abstract class approverEmail : PX.Data.BQL.BqlString.Field<approverEmail> { }
    #endregion

    #region ApproverID
    [PXDBInt]
    [PXUIField(DisplayName = "Approver ID")]
    public virtual int? ApproverID { get; set; }
    public abstract class approverID : PX.Data.BQL.BqlInt.Field<approverID> { }
    #endregion

    #region Status
    [PXDBString(1, IsFixed = true)]
    [PXUIField(DisplayName = "Status")]
    public virtual string Status { get; set; }
    public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
    #endregion

    #region ActionDate
    [PXDBDateAndTime]
    [PXUIField(DisplayName = "Approve Date")]
    public virtual DateTime? ActionDate { get; set; }
    public abstract class actionDate : PX.Data.BQL.BqlDateTime.Field<actionDate> { }
    #endregion

    #region ActionByID
    [PXDBInt]
    [PXUIField(DisplayName = "ApprovedByID")]
    public virtual int? ActionByID { get; set; }
    public abstract class actionByID : PX.Data.BQL.BqlInt.Field<actionByID> { }
    #endregion

    #region Action
    [PXDBString(50)]
    [PXUIField(DisplayName = "Approval Status")]
    public virtual string Action { get; set; }
    public abstract class action : PX.Data.BQL.BqlString.Field<action> { }
    #endregion

    #region NotificationSiteUrl
    [PXDBString(255, IsUnicode = true)]
    [PXUIField(DisplayName = "Notification Site URL", Enabled = false)]
    public virtual string NotificationSiteUrl { get; set; }
    public abstract class notificationSiteUrl : PX.Data.BQL.BqlString.Field<notificationSiteUrl> { }
    #endregion

    #region NoteID
    [PXNote]
    public virtual Guid? NoteID { get; set; }
    public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
    #endregion

    #region Default Fields
    [PXDBCreatedByID]
    public virtual Guid? CreatedByID { get; set; }
    public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

    [PXDBCreatedByScreenID]
    public virtual string CreatedByScreenID { get; set; }
    public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

    [PXDBCreatedDateTime]
    public virtual DateTime? CreatedDateTime { get; set; }
    public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

    [PXDBLastModifiedByID]
    public virtual Guid? LastModifiedByID { get; set; }
    public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

    [PXDBLastModifiedByScreenID]
    public virtual string LastModifiedByScreenID { get; set; }
    public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

    [PXDBLastModifiedDateTime]
    public virtual DateTime? LastModifiedDateTime { get; set; }
    public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

    #region tstamp
    [PXDBTimestamp]
    public virtual byte[] tstamp { get; set; }
    public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
    #endregion

    #endregion
}