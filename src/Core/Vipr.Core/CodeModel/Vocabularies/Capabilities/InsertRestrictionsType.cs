using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vipr.Core.CodeModel.Vocabularies.Capabilities
{
    public class OdcmInsertCapability : OdcmCapability
    {
        /// <summary>
        /// Entities can be inserted
        /// </summary>
        public bool Insertable { get; set; }

        public OdcmInsertCapability()
        {
            Insertable = true;
        }

        public override bool Equals(OdcmCapability otherCapability)
        {
            var other = otherCapability as OdcmInsertCapability;
            if (other == null)
            {
                return false;
            }

            return Insertable == other.Insertable;
        }

    }
}
