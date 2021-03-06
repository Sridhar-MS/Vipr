﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Its.Recipes;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Vipr.Core.CodeModel;
using Xunit;

namespace CSharpWriterUnitTests
{
    public class Given_an_OdcmClass_Service_Bound_Function : EntityTestBase
    {
        private OdcmMethod _method;
        private OdcmClass _expectedReturnClass;
        private Type _expectedReturnType;
        private string _expectedMethodName;

        
        public Given_an_OdcmClass_Service_Bound_Function()
        {
            Init(
                model =>
                {
                    model.AddType(_expectedReturnClass = Any.OdcmClass());
                    model.EntityContainer.Methods.Add(
                        _method = Any.OdcmMethod(m => m.ReturnType = _expectedReturnClass));
                });

            _expectedReturnType =
                typeof(Task<>).MakeGenericType(Proxy.GetClass(_expectedReturnClass.Namespace, _expectedReturnClass.Name));

            _expectedMethodName = _method.Name + "Async";
        }

        [Fact]
        public void The_EntityContainer_interface_exposes_the_method()
        {
            EntityContainerInterface.Should().HaveMethod(
                CSharpAccessModifiers.Public,
                _expectedReturnType,
                _expectedMethodName,
                GetMethodParameterTypes());
        }

        [Fact]
        public void The_EntityContainer_class_exposes_the_async_method()
        {
            EntityContainerType.Should().HaveMethod(
                CSharpAccessModifiers.Public,
                true,
                _expectedReturnType,
                _expectedMethodName,
                GetMethodParameterTypes());
        }

        private IEnumerable<Type> GetMethodParameterTypes()
        {
            return _method.Parameters.Select(p => Proxy.GetClass(p.Type.Namespace, p.Type.Name));
        }
    }
}
