﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Vipr.Core.CodeModel;

namespace CSharpWriter
{
    public class Indexers
    {
        public static IEnumerable<Indexer> ForCollection(OdcmClass odcmClass)
        {
            return new Indexer[]
            {
                new CollectionGetByIdIndexer(odcmClass)
            };
        }
    }
}