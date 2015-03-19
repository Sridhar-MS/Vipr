using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vipr.Core.CodeModel.Vocabularies.Capabilities
{
    /// <summary>
    /// Restrictions on update operations
    /// </summary>
    public class OdcmUpdateCapability : OdcmCapability
    {
        /// <summary>
        /// Entities can be updated
        /// </summary>
        public bool Updatable { get; set; }

        public OdcmUpdateCapability()
        {
            Updatable = true;
        }

        public override bool Equals(OdcmCapability otherCapability)
        {
            var other = otherCapability as OdcmUpdateCapability;
            if (other == null)
            {
                return false;
            }

            return Updatable == other.Updatable;
        }
    }
}
