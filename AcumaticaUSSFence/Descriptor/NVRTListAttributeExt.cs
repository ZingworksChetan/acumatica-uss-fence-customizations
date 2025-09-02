using PX.Data;

namespace Descriptor
{
    public abstract class NVRTListAttributeExt
    {
        public class NVFSWListAttribute : PXStringListAttribute
        {
            public NVFSWListAttribute() : base(
               new[]
               {
                Pair("_", "InitialState"),
                Pair("H", "Hold"),
                Pair("E", "Estimate"),
                Pair("O", "Open"),
                Pair("R", "InRent"),
                Pair("T", "Returned"),
                Pair("S", "Scheduled"),
                Pair("I", "InProcess"),
                Pair("C", "Completed"),
                Pair("L", "Closed"),
                Pair("X", "Canceled"),
                Pair("P", "PendingApproval"),
                Pair("V", "Rejected"),
               })
            { }
        }
    }
}
