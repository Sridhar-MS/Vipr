using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vipr.Core.CodeModel.Vocabularies.Capabilities
{
    public class OdcmExpandCapability : OdcmCapability
    {
        /// <summary>
        /// $expand is supported
        /// </summary>
        public bool Expandable { get; set; }

        public OdcmExpandCapability()
        {
            Expandable = true;
        }

        public override bool Equals(OdcmCapability otherCapability)
        {
            var other = otherCapability as OdcmExpandCapability;
            if (other == null)
            {
                return false;
            }

            return Expandable == other.Expandable;
        }
    }
}
