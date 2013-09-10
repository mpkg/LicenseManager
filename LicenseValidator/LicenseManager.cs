using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;

namespace IOTAP.LicenseManagement
{
    public class LicenseManager
    {
        IOrganizationService _crmService = null;
        IPluginExecutionContext _context = null;
        private const int TRIAL_NO_OF_DAYS = 2;

        public LicenseManager(ref IOrganizationService crmService, ref IPluginExecutionContext context)
        {
            _crmService = crmService;
            _context = context;
        }

        public bool _isTrialExpired(string ProductName, int TrialDays)
        {
            QueryExpression qe = new QueryExpression();
            qe.EntityName = EntityName.Solution;
            qe.ColumnSet = new ColumnSet() { AllColumns = true };//"name", "webresourcetype", "content", "displayname");
            ConditionExpression ce = new ConditionExpression();

            ce.AttributeName = EntityAttributes.SolutionName;
            ce.Operator = ConditionOperator.Equal;
            ce.Values.Add(ProductName);
            FilterExpression fe = new FilterExpression();
            fe.Conditions.Add(ce);
            qe.Criteria = fe;
            EntityCollection retrievedRecords = _crmService.RetrieveMultiple(qe);

            if (retrievedRecords.Entities.Count <= 0)
            {
                throw new SystemException("Could not find add-on solution or solution names mis-match.");
            }
            else
            {
                Entity solution = (Entity)retrievedRecords.Entities[0];
                DateTime installDate = solution.GetAttributeValue<DateTime>("installedon");
                if (installDate.ToUniversalTime().AddDays(TRIAL_NO_OF_DAYS) >= DateTime.Today.ToUniversalTime())
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public int _validateLicense(string AppName, string ProductName)
        {
            QueryExpression qe = new QueryExpression();
            qe.EntityName = EntityName.Webresource;
            qe.ColumnSet = new ColumnSet() { AllColumns = true };//"name", "webresourcetype", "content", "displayname");
            ConditionExpression ce = new ConditionExpression();

            ce.AttributeName = EntityAttributes.Name;
            ce.Operator = ConditionOperator.Equal;
            ce.Values.Add(AppName);
            FilterExpression fe = new FilterExpression();
            fe.Conditions.Add(ce);
            qe.Criteria = fe;

            int result = _licenseRetrieve(_crmService.RetrieveMultiple(qe), ProductName);
            switch (result)
            {
                case Result.LicenseValid:
                    return Result.LicenseValid;
                case Result.TrialLicense:
                    return Result.TrialLicense;
                case Result.LicenseExpired:
                    throw new SystemException("License has expired.");
                case Result.LicenseInvalid:
                    throw new SystemException("Invalid license code.");
                case Result.LicenseNotAvailable:
                    throw new SystemException("License is not available.");
                case Result.TrialExpried:
                    throw new SystemException("Trial period has expired. Please contact sales@iotap.com for product license.");
                default:
                    throw new SystemException("Unable to process license.");
            }
        }

        private int _licenseRetrieve(EntityCollection result, string ProductName)
        {
            EntityCollection retrievedRecords = result;

            if (retrievedRecords.Entities.Count <= 0)
            {
                //return Result.LicenseNotAvailable;

                if (_isTrialExpired(ProductName, TRIAL_NO_OF_DAYS))
                {
                    return Result.TrialExpried;
                }
                else
                {
                    //trial version
                    return Result.TrialLicense;
                }
            }

            else
            {
                Entity wb = (Entity)retrievedRecords.Entities[0];

                byte[] b = Convert.FromBase64String(wb.GetAttributeValue<string>(EntityAttributes.Content));

                string lic = System.Text.Encoding.UTF8.GetString(b, 0, b.Length);

                LicenseValidator oValidator = new LicenseValidator(lic);
                if (oValidator.IsValid())
                {
                    string noOfUsers = oValidator.Get(LicenseTag.Users);
                    string expiryDate = oValidator.Get(LicenseTag.ExpireDate);
                    DateTime licenceExpiredDate = Convert.ToDateTime(expiryDate);
                    DateTime CurrenDateTime = DateTime.Now.Date;
                    int IsValidDate = licenceExpiredDate.Subtract(CurrenDateTime).Days;
                    if (!ProductName.Equals(oValidator.Get(LicenseTag.Product), StringComparison.OrdinalIgnoreCase))
                        return Result.LicenseInvalid;
                    if (IsValidDate < 0)
                        return Result.LicenseExpired;
                    else
                    {                                                
                    // return _validateOrg(oValidator.Get(LicenseTag.OrganizationName));                      
                        if (_validateOrg(oValidator.Get(LicenseTag.OrganizationName)) == 0)
                        {
                            if (CheckValidUserCount(oValidator.Get(LicenseTag.Users), _crmService, _context))
                            {
                                return Result.LicenseValid;
                            }
                            else
                            {
                                // tracingService.Trace("Active user count does not match.");
                                throw new InvalidPluginExecutionException("Active user count does not match.");
                            }
                        }
                        else
                        {

                            throw new InvalidPluginExecutionException("Invalid license code.");
                        }
                    }
                }
                else
                {
                    return Result.LicenseInvalid;
                }
            }
        }
        private bool CheckValidUserCount(string userCount, IOrganizationService _crmService, IPluginExecutionContext executionContext)
        {
            if (userCount != "")
            {
                int licenseUser = Convert.ToInt32(userCount);
                QueryExpression qe = new QueryExpression();
                qe.EntityName = "systemuser";
                qe.ColumnSet = new ColumnSet("firstname");//"name", "webresourcetype", "content", "displayname");
                ConditionExpression ce = new ConditionExpression();

                ce.AttributeName = "isdisabled";
                ce.Operator = ConditionOperator.Equal;//, new string[] { AppName }}
                ce.Values.Add("0");
                ConditionExpression ce2 = new ConditionExpression();
                ce2.AttributeName = "accessmode";
                ce2.Operator = ConditionOperator.NotEqual;//, new string[] { AppName }}
                ce2.Values.Add("3");
                FilterExpression fe = new FilterExpression();
                fe.Conditions.Add(ce);
                fe.Conditions.Add(ce2);
                qe.Criteria = fe;

                RetrieveMultipleRequest retrieveMultipleRequest = new RetrieveMultipleRequest();
                RetrieveMultipleResponse retrieveMultipleResponse = new RetrieveMultipleResponse();
                retrieveMultipleRequest.Query = qe;

                retrieveMultipleResponse = (RetrieveMultipleResponse)_crmService.Execute(retrieveMultipleRequest);

                EntityCollection entityCollection = retrieveMultipleResponse.EntityCollection;

                if (entityCollection.Entities.Count <= licenseUser)//License found
                {
                    return true;
                }
                else
                {
                    return false;

                }
            }
            else
            { return true; }

        }
        private int _validateOrg(string orgname)
        {
            string org = _context.OrganizationName;
            if (!string.IsNullOrEmpty(orgname))
            {
                if (orgname.Equals(org, StringComparison.OrdinalIgnoreCase))
                    return Result.LicenseValid;
                else
                    return Result.LicenseInvalid;
            }
            else
            {
                return Result.LicenseValid;
            }
        }
    }
}