﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Vipr.Core.CodeModel
{
    public class OdcmModel
    {
        private List<OdcmType> _Types = new List<OdcmType>();

        public List<OdcmNamespace> Namespaces { get; private set; }

        public IReadOnlyDictionary<string, string> ServiceMetadata { get; private set; } 

        public OdcmClass EntityContainer
        {
            get
            {
                return Namespaces
                    .SelectMany(n => n.Classes)
                    .FirstOrDefault(c => c.Kind == OdcmClassKind.Service);
            }
        }

        public IEnumerable<OdcmVocabularyAnnotation> VocabularyAnnotations { get; private set; }
        public ServiceType ServiceType { get; private set; }

        public OdcmModel(IReadOnlyDictionary<string, string> serviceMetadata, ServiceType serviceType = ServiceType.ODataV4)
        {
            Namespaces = new List<OdcmNamespace>();
            ServiceMetadata = serviceMetadata;
            ServiceType = serviceType;
        }

        public void AddType(OdcmType type)
        {
            string @namespace = type.Namespace;
            OdcmNamespace odcmNamespace = null;
            foreach (OdcmNamespace candidate in Namespaces)
            {
                if (string.Equals(candidate.Name, @namespace))
                {
                    odcmNamespace = candidate;
                    break;
                }
            }
            if (odcmNamespace == null)
            {
                odcmNamespace = new OdcmNamespace(@namespace);
                Namespaces.Add(odcmNamespace);
            }

            _Types.Add(type);
            odcmNamespace.Types.Add(type);
        }

        public bool TryResolveType<T>(string name, string @namespace, out T type) where T : OdcmType
        {
            foreach (OdcmType candidate in _Types)
            {
                if (string.Equals(candidate.Name, name) && string.Equals(candidate.Namespace, @namespace))
                {
                    type = candidate as T;
                    return true;
                }
            }

            type = null;

            return false;
        }
    }

    public enum ServiceType
    {
        ODataV3,
        ODataV4
    }
}
