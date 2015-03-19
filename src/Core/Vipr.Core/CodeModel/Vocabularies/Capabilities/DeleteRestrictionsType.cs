using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vipr.Core.CodeModel.Vocabularies.Capabilities
{
    public abstract class OdcmCapability : IEquatable<OdcmCapability>
    {
        public abstract bool Equals(OdcmCapability otherCapability);
    }

    /// <summary>
    /// Restrictions on delete operations
    /// </summary>
    public class OdcmDeleteCapability : OdcmCapability
    {
        /// <summary>
        /// Entities can be deleted
        /// </summary>
        public bool Deletable { get; set; }

        public OdcmDeleteCapability()
        {
            Deletable = true;
        }

        public override bool Equals(OdcmCapability otherCapability)
        {
            var other = otherCapability as OdcmDeleteCapability;
            if(other == null)
            {
                return false;
            }

            return Deletable == other.Deletable;
        }
    }
}
