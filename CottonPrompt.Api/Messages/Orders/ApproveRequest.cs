namespace CottonPrompt.Api.Messages.Orders
{
    public class ApproveRequest
    {
        public Guid? ApprovedBy { get; set; }

        public bool IsAdminApproval { get; set; }
    }
}
