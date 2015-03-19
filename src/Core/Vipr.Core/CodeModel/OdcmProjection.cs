using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vipr.Core.CodeModel.Vocabularies;
using Vipr.Core.CodeModel.Vocabularies.Capabilities;

namespace Vipr.Core.CodeModel
{
    public class OdcmProjection
    {
        public OdcmType Type { get; set; }

        public List<OdcmCapability> Capabilities { get; set; }
    }
}
