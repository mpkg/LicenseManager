using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace IOTAP.LicenseManagement
{
    public class LicenseValidator
    {
        private Dictionary<string, string> _licenseNodes;

        string _license;

        string _licenseToken;

        public LicenseValidator(string license)
        {
            _license = license;
        }

        public bool IsValid()
        {
            bool blnReturn = false;

            _loadCode();

            string delimitedString = _delimit();
            try
            {
                if (Hashing.Encrypt(delimitedString).Equals(_licenseToken))
                    blnReturn = true;
            }
            catch (Exception ex)
            {
                blnReturn = false;
            }

            return blnReturn;
        }

        private string _delimit()
        {
            string DelimitFormat = "{0}={1}";
            string DelimitFormatSecond = "|{0}={1}";

            StringBuilder strBdr = new StringBuilder();
            bool isFirst = true;
            foreach (var node in _licenseNodes)
            {
                if (node.Key.Equals(LicenseTag.Token))
                    continue;

                if (isFirst)
                {
                    strBdr.Append(string.Format(DelimitFormat, node.Key, node.Value));
                    isFirst = false;
                }
                else
                    strBdr.Append(string.Format(DelimitFormatSecond, node.Key, node.Value));
            };

            return strBdr.ToString();
        }

        private void _loadCode()
        {
            try
            {
                //_trace("Starting _loadLicenseNodes");
                XDocument licenseDocument = XDocument.Parse(_license);

                var delimitesStrings = (from e in licenseDocument.Descendants(LicenseTag.License).Descendants() select e);

                _licenseNodes = new Dictionary<string, string>();
                foreach (var item in delimitesStrings)
                {
                    _licenseNodes.Add(item.Name.LocalName, item.Value);

                    if (item.Name.LocalName == LicenseTag.Token)
                    {
                        _licenseToken = item.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // ErrorMessage = ex.Message;
                //blnReturn = false;
            }
        }

        internal string Get(string attributename)
        {
            return _licenseNodes[attributename];
        }
    }
}