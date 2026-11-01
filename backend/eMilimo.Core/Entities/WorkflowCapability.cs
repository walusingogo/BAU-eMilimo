namespace eMilimo.Core.Entities;

public static class WorkflowCapabilities
{
    public const string RequisitionEntityReview = "REQUISITION_ENTITY_REVIEW";
    public const string RequisitionChurchAuthority = "REQUISITION_CHURCH_AUTHORITY";
    public const string RequisitionTreasurerApproval = "REQUISITION_TREASURER_APPROVAL";
    public const string RequisitionAccountantApproval = "REQUISITION_ACCOUNTANT_APPROVAL";
    public const string RequisitionMinisterApproval = "REQUISITION_MINISTER_APPROVAL";
    public const string ContributionReview = "CONTRIBUTION_REVIEW";
    public const string FrbManage = "FRB_MANAGE";
    public const string FrbVerifyStewardship = "FRB_VERIFY_STEWARDSHIP";
    public const string FrbVerifyTreasurer = "FRB_VERIFY_TREASURER";
    public const string FrbVerifyMinister = "FRB_VERIFY_MINISTER";
    public const string FrbFinalize = "FRB_FINALIZE";
    public const string FrbOrganizationScope = "FRB_ORGANIZATION_SCOPE";
    public const string ServiceCollectionReview = "SERVICE_COLLECTION_REVIEW";
    public const string ServiceCollectionManage = "SERVICE_COLLECTION_MANAGE";
    public const string ServiceCollectionVarianceApprove = "SERVICE_COLLECTION_VARIANCE_APPROVE";

    public static readonly string[] All =
    [
        RequisitionEntityReview, RequisitionChurchAuthority, RequisitionTreasurerApproval,
        RequisitionAccountantApproval, RequisitionMinisterApproval, ContributionReview,
        FrbManage, FrbVerifyStewardship, FrbVerifyTreasurer, FrbVerifyMinister,
        FrbFinalize, FrbOrganizationScope, ServiceCollectionManage, ServiceCollectionReview, ServiceCollectionVarianceApprove
    ];
}
